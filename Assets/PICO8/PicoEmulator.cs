using MoonSharp.Interpreter;
using MoonSharp.VsCodeDebugger;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.IO;
using FixedPointy;
using UnityEngine;
using UnityEngine.Profiling;

namespace TsFreddie.Pico8
{
    public class PicoEmulator
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

        Script engine;
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

        void Del(Table t, DynValue dv) {
            int len = t.Length;
            for (int i = 1; i <= len; i++) {
                if (t.Get(i).Equals(dv)) {
                    for (int j = i + 1; j <= len; j++) {
                        t.Set(j-1, t.Get(j));
                    }
                    t.Remove(len);
                    return;
                }
            }
        }

        void Foreach(ScriptExecutionContext context, Table t, DynValue f) {
            sampler.Begin();
            if (t == null) return;
            
            if (f.Type == DataType.Function) {
                for (int i = 1; i <= t.Length; i++) {
                    f.Function.Call(t.Get(i));
                }
            }
            else if (f.Type == DataType.ClrFunction) {
                for (int i = 1; i <= t.Length; i++) {
                    f.Callback.ClrCallback(context, new CallbackArguments(new DynValue[1]{t.Get(i)}, false));
                }
            }
            sampler.End();
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
                { "fget", (Func<int,byte?,DynValue>)ppu.Fget },
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

                #region TABLE
                { "add", (Action<Table, DynValue>)((t, v) => t.Append(v)) },
                { "del", (Action<Table, DynValue>)Del },
                { "foreach", (Action<ScriptExecutionContext, Table, DynValue>)Foreach },
                { "count", (Func<Table, int>)(t => t.Length) },
                #endregion
            };

            foreach (var api in apiTable)
            {
                engine.Globals[api.Key] = api.Value;
            }

            // Lua Implemented API
            Run(@"
            function tostr(x)
                if (type(x) == ""number"") return tostring(math.floor(x*10000)/10000)
                return tostring(x)
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

            cocreate = coroutine.create
            coresume = coroutine.resume
            costatus = coroutine.status
            yield = coroutine.yield

            ");
        }

        #endregion
        #region PICO8SYNTAX
        private string IfShorthandMatch(Match match)
        {
            string exp = match.Groups[1].ToString();
            if (match.Groups[1].ToString().Contains("then"))
            {
                return match.ToString();
            }
            int nextChar = 0;

            // Skip to the closing right parethese.
            LinkedList<char> stack = new LinkedList<char>();
            stack.AddLast('(');
            nextChar++;
            while (stack.Count != 0)
            {
                if (nextChar >= exp.Length)
                    return match.ToString();
                if (exp[nextChar] == '(')
                {
                    stack.AddLast('(');
                }
                if (exp[nextChar] == ')')
                {
                    stack.RemoveLast();
                }
                nextChar++;
            }

            string rest = exp.Substring(nextChar).Trim();
            if (exp.Length <= 0) {
                return match.ToString();
            }
            exp = exp.Substring(0, nextChar);

            return String.Format("if {0} then {1} end", exp, rest);
        }

        private string UnaryAssignmentMatch(Match match)
        {
            //Debug.Log(match);
            string potentialExp = Regex.Replace(match.Groups[3].ToString(), @"\.\s+", _ => ".");
            string realExp = "";
            //Debug.Log("potential: " + potentialExp);

            MatchCollection terms = Regex.Matches(potentialExp, @"(?:\-?(?:0x)?[0-9.]+)|(?:\-?[a-zA-Z_](?:[a-zA-Z0-9_]|(?:\.\s*))*(?:\[[^\]]\])*)");

            if (terms.Count < 0) return match.ToString();
            int i = 0;
            int nextChar = 0;
            Match nextTerm = terms[0];
            bool expectTerm = true;

            while (nextChar < potentialExp.Length)
            {
                if (nextChar >= potentialExp.Length)
                    return match.ToString();

                if (nextTerm == null || nextChar < nextTerm.Index)
                {
                    if (potentialExp[nextChar] == '(')
                    {
                        // Skip to the closing right parethese.
                        LinkedList<char> stack = new LinkedList<char>();
                        stack.AddLast('(');
                        nextChar++;
                        while (stack.Count != 0)
                        {
                            if (nextChar >= potentialExp.Length)
                                return match.ToString();
                            if (potentialExp[nextChar] == '(')
                            {
                                stack.AddLast('(');
                            }
                            if (potentialExp[nextChar] == ')')
                            {
                                stack.RemoveLast();
                            }
                            nextChar++;
                        }
                        expectTerm = false;
                    }
                    else if (Regex.IsMatch(potentialExp[nextChar].ToString(), @"\s"))
                    {
                        nextChar++;
                    }
                    else if (Regex.IsMatch(potentialExp[nextChar].ToString(), @"[+\-*\/%^]"))
                    {
                        nextChar++;
                        expectTerm = true;
                    }
                }
                else if (nextChar == nextTerm.Index)
                {
                    //Debug.Log(string.Format("{0}: expecting: {1}, term: {2}", nextChar, expectTerm, nextTerm.ToString()));
                    if (!expectTerm)
                    {
                        break;
                    }
                    nextChar += nextTerm.Length;
                    if (++i >= terms.Count)
                        break;
                    nextTerm = terms[i];
                    expectTerm = false;
                }
                else
                {
                    if (++i >= terms.Count)
                        nextTerm = null;
                    else
                        nextTerm = terms[i];
                }
            }
            if (nextChar > potentialExp.Length) nextChar = potentialExp.Length;

            realExp = potentialExp.Substring(0, nextChar).Trim();
            potentialExp = potentialExp.Substring(nextChar);
            string assignee = match.Groups[1].ToString();
            string operation = match.Groups[2].ToString();

            string result = string.Format("{0} = {0} {1} ({2}) {3}", assignee, operation, realExp, ProcessUnaryAssignment(potentialExp));
            //Debug.Log("result: " + result);
            return result;

        }
        
        public string ProcessUnaryAssignment(string str)
        {
            return Regex.Replace(str, @"([a-zA-Z_](?:[a-zA-Z0-9_]|(?:\.\s*))*(?:\[.*\])?)\s*([+\-*\/%])=\s*(.*)$", UnaryAssignmentMatch, RegexOptions.Multiline);
        }

        public string ProcessIfShorthand(string str)
        {
            //[iI][fF]\s*(\(.*\))\s*(.*)$
            return Regex.Replace(str, @"[iI][fF]\s*(\(.*)$", IfShorthandMatch, RegexOptions.Multiline);
        }

        public void Preprocess(ref string script)
        {
            script = ProcessIfShorthand(ProcessUnaryAssignment(script));
        }
        #endregion
        
        public PicoEmulator() {
            engine = new Script();
            memory = new MemoryModule();
            ppu = new PictureProcessingUnit(memory);
            apu = new AudioProcessingUnit();
            buttons = new bool[(int)Buttons.UNDEFINED];
            lastButtons = new bool[(int)Buttons.UNDEFINED];
            // Random seed
            random = new System.Random();
            // Register APIs
            RegisterAPIs();
        }

        public void LoadCartridge(Cartridge cart) {
            cartridge = cart;
            // Copy to memory
            memory.CopyFromROM(cart.ROM, 0, 0x4300);
            #if UNITY_EDITOR
                // lua debugger
                var server = new MoonSharpVsCodeDebugServer();
                server.Start();
                server.AttachToScript(engine, "code");
            #endif
            string script = cart.ExtractScript();
            Run(script); 
            if (engine.Globals["_init"] != null)
                Call("_init");
        }
        public DynValue Call(string func, params DynValue[] args)
        {
            return engine.Call(engine.Globals[func], args);
        }

        /* 
        public DynValue Run(string script)
        {
            Preprocess(ref script);
            DynValue luaScript = engine.LoadString(script);
            DynValue coroutine = engine.CreateCoroutine(luaScript);
            DynValue result = null;
            int cycle = 0;
            coroutine.Coroutine.AutoYieldCounter = 1000;
            for (result = coroutine.Coroutine.Resume(); result.Type == DataType.YieldRequest; result = coroutine.Coroutine.Resume()) 
            {
                Debug.Log("cycle");
                cycle += 4;
                if (cycle > 1)
                    return new DynValue();
            }
            return result;
        }
        */
        public DynValue Run(string script)
        {
            Preprocess(ref script);
            //return engine.DoFile("fact.lua");
            return engine.DoString(script);
        }

        public void SendInput(Buttons button) {
            buttons[(int)button] = true;
        }

        public void Update() {
            if (engine.Globals["_update"] != null)
                Call("_update");
            if (engine.Globals["_draw"] != null)
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

