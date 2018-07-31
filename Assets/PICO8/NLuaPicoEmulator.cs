using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using NLua;

namespace TsFreddie.Pico8
{
    public class NLuaPicoEmulator
    {
        public enum Buttons : int {
            LEFT = 0,
            RIGHT,
            UP,
            DOWN,
            CIRCLE,
            CROSS,
            PAUSE,
            UNDEFINED,
        }

        Lua engine;
        System.Random random;
        const double FRAC = 65536;

        // Northbridge
        MemoryModule memory;
        PictureProcessingUnit ppu;
        AudioProcessingUnit apu;

        // Southbridge
        Cartridge cartridge;
        CartridgeData cartdata;
        bool[] lastButtons;
        bool[] buttons;
        CustomSampler sampler = CustomSampler.Create("Foreach");
        // Exposed data
        public Texture2D Texture { get { return ppu.Texture; } }
        public byte[] SCREEN { get { return memory.VideoBuffer; } }

        #region PICO8API

        bool Btn(int b, int p = -1) {
            return buttons[b];
        }

        bool Btnp(int b, int p = -1) {
            return buttons[b] && !lastButtons[b];
        }

        void RegisterAPIs()
        {
            // C# & Lambda Implemented API
            Dictionary<String, object> apiTable = new Dictionary<String, object>()
            {
                #region MATH
                { "abs", (Func<double, double>)Math.Abs },
                { "atan2", (Func<double, double, double>)((dx, dy) => 1 - Math.Atan2(dy, dx) / (2 * Math.PI)) },
                { "band", (Func<double, double, double>)((x, y) => ((int)(x * FRAC) & (int)(y * FRAC)) / FRAC) },
                { "bnot", (Func<double, double>)(x => (~(int)(x * FRAC)) / FRAC) },
                { "bor", (Func<double, double, double>)((x, y) => ((int)(x * FRAC) | (int)(y * FRAC)) / FRAC) },
                { "bxor", (Func<double, double, double>)((x, y) => ((int)(x * FRAC) ^ (int)(y * FRAC)) / FRAC) },
                { "cos", (Func<double, double>)(x => Math.Cos(x * 2 * Math.PI)) },
                { "flr", (Func<double, double>)Math.Floor },
                { "max", (Func<double, double, double>)Math.Max },
                { "mid", (Func<double, double, double, double>)((x, y, z) =>  Math.Max(Math.Min(x,y), Math.Min(Math.Max(x,y),z))) },
                { "min", (Func<double, double, double>)Math.Min },
                { "rnd", (Func<double, double>)(x => random.NextDouble() * x) },
                { "shl", (Func<double, double, double>)((x, y) => ((int)(x * FRAC) << (int)y) / FRAC) },
                { "shr", (Func<double, double, double>)((x, y) => ((int)(x * FRAC) >> (int)y) / FRAC) },
                { "sin", (Func<double, double>)(x => -Math.Sin(x * 2 * Math.PI)) },
                { "sqrt", (Func<double, double>)Math.Sqrt },
                { "srand", (Action<double>)(x => random = new System.Random((int)(x * FRAC))) },
                #endregion
                
                #region MEMORY
                { "peek", (Func<ushort, byte>)memory.Peek },
                { "poke", (Action<ushort, byte>)memory.Poke },
                { "memcpy", (Action<ushort, ushort, ushort>)memory.MemCpy },
                { "memset", (Action<ushort, byte, ushort>)memory.MemSet },
                #endregion

                #region GRAPHICS
                { "camera",  (Action<int,int>)ppu.Camera },
                { "circ", (Action<int,int,int,int>)ppu.Circ },
                { "circfill", (Action<int,int,int,int>)ppu.Circfill },
                { "clip", (Action<int,int,int,int>)ppu.Clip },
                { "cls", (Action)ppu.Cls },
                { "color", (Action<Nullable<int>>)ppu.Color },
                { "cursor", (Action<int,int>)ppu.Cursor },
                { "fget", (Func<int,byte?,object>)ppu.Fget },
                { "fillp", (Action<int>)ppu.Fillp },
                { "fset", (Action<int,int,bool>)ppu.Fset },
                { "line", (PictureProcessingUnit.APILine)ppu.Line },
                { "pal",  (Action<byte,byte,byte>)ppu.Pal },
                { "palt", (Action<byte, bool>)ppu.Palt },
                { "pget", (Func<int,int,byte>)ppu.Pget },
                { "print", (Action<string,int,int,int>)ppu.Print },
                { "pset", (Action<int,int,int>)ppu.Pset },
                { "rect", (PictureProcessingUnit.APIRect)ppu.Rect },
                { "rectfill", (PictureProcessingUnit.APIRect)ppu.Rectfill },
                { "sget", (Func<int,int,byte>)ppu.Sget },
                { "spr",  (PictureProcessingUnit.APISpr)ppu.Spr },
                { "sset", (Action<int,int,byte>)ppu.Sset },
                { "sspr", (PictureProcessingUnit.APISspr)ppu.Sspr },

                // MAP
                { "map", (PictureProcessingUnit.APIMap)ppu.Map },
                { "mget", (Func<int,int,byte>)ppu.Mget },
                { "mset", (Action<int,int,byte>)ppu.Mset },
                #endregion

                #region INTERNAL
                { "flip", (Action)ppu.Flip },
                { "dprint", (Action<object>)(x => Debug.Log(x)) },
                #endregion

                #region MUSIC
                { "music", (Action<int, int, int>)apu.Music },
                { "sfx", (Action<int, int, int, int>)apu.Sfx },
                #endregion

                #region INPUT
                { "btn", (Func<int, int, bool>)Btn },
                { "btnp", (Func<int, int, bool>)Btnp },
                #endregion
            };

            foreach (var api in apiTable)
            {
                engine[api.Key] = api.Value;
            }

            // Lua Implemented API
            Run(@"
            function tostr(x)
                if (type(x) == ""number"") return tostring(math.floor(x*10000)/10000)
                return tostring(x)
            end

            function del(a,dv)
                if a == nil then
                    return
                end
                for i,v in ipairs(a) do
                    if v==dv then
                        table.remove(a,i)
                    end
                end
            end

            function add(a,v)
                if a == nil then
                    return
                end
                table.insert(a,v)
            end

            function foreach(a,f)
                if not a then
                    return
                end
                for i,v in ipairs(a) do
                    f(v)
                end
            end

            function all(t)
                local i = 0
                local n = #t
                return
                    function ()
                        i = i + 1
                        if i <= n then return t[i] end
                    end
            end

            function count(a)
	            return #a
            end

            cocreate = coroutine.create
            coresume = coroutine.resume
            costatus = coroutine.status
            yield = coroutine.yield

            ");
        }

        #endregion
        
        public NLuaPicoEmulator() {
            engine = new Lua();
            memory = new MemoryModule();
            ppu = new PictureProcessingUnit(memory);
            apu = new AudioProcessingUnit();
            buttons = new bool[(int)Buttons.UNDEFINED];
            lastButtons = new bool[(int)Buttons.UNDEFINED];

            engine["_mem"] = memory;
            engine["_ppu"] = ppu;
            engine["_apu"] = apu;

            // Random seed
            random = new System.Random();
            // Register APIs
            RegisterAPIs();
        }

        public void LoadCartridge(Cartridge cart) {
            cartridge = cart;
            // Copy to memory
            memory.CopyFromROM(cart.ROM, 0, 0x4300);
            string script = cart.ExtractScript();
            StreamWriter writer = new StreamWriter(new FileStream("script.lua", FileMode.Create));
            writer.Write(script);
            writer.Close();
            Run(script); 
            if (engine["_init"] != null)
                Call("_init");
        }
        public object[] Call(string func, params object[] args)
        {
            return (engine[func] as LuaFunction).Call(args);
        }

        public void Run(string script)
        {
            Preprocess(ref script);
            engine.DoString(script);
        }

        public void SendInput(Buttons button) {
            buttons[(int)button] = true;
        }

        public void Update() {
            if (engine["_update"] != null)
                Call("_update");
            if (engine["_draw"] != null)
                Call("_draw");
            ppu.Flip();

            // TODO: in memory?
            bool[] tmp = lastButtons;
            lastButtons = buttons;
            buttons = tmp;

            for (int i = 0; i < (int)Buttons.UNDEFINED; i++) {
                buttons[i] = false;
            }
        }
    }
}

