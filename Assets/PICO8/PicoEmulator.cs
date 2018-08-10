using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TsFreddie.Pico8
{
	public abstract class PicoEmulator {
		public const double FRAC = 65536;
	
		protected System.Random random;

		// Northbridge
        protected MemoryModule memory;
        public PictureProcessingUnit ppu;
        protected AudioProcessingUnit apu;

		// Southbridge
        protected Cartridge cartridge;
        protected CartridgeData cartdata;
        bool[] lastButtons;
        bool[] buttons;

		// Exposed data
        public Texture2D Texture { get { return ppu.Texture; } }
        public byte[] SCREEN { get { return memory.VideoBuffer; } }

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

		protected PicoEmulator() {
			InitializeLuaEngine();
			memory = new MemoryModule();
            memory.InitializeStates();

            ppu = new PictureProcessingUnit(memory);
            apu = new AudioProcessingUnit();
			buttons = new bool[(int)Buttons.UNDEFINED];
            lastButtons = new bool[(int)Buttons.UNDEFINED];
			
            // Random seed
            random = new System.Random();

            // Register APIs
			RegisterAPIs();
		}
		
		protected abstract void InitializeLuaEngine();
		protected abstract void RegisterAPI(String apiName, object func);
		
        private void RegisterAPIs()
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
                { "peek", (Func<int, byte>)memory.Peek },
                { "poke", (Action<int, byte>)memory.Poke },
                { "memcpy", (Action<int, int, int>)memory.MemCpy },
                { "memset", (Action<int, byte, int>)memory.MemSet },
                #endregion

                #region GRAPHICS
                { "camera",  (Action<int,int>)ppu.Camera },
                { "circ", (Action<int?,int?,int,int?>)ppu.Circ },
                { "circfill", (Action<int?,int?,int,int?>)ppu.Circfill },
                { "clip", (Action<int,int,int,int>)ppu.Clip },
                { "cls", (Action)ppu.Cls },
                { "color", (Action<int>)ppu.Color },
                { "cursor", (Action<int,int>)ppu.Cursor },
                { "fget", (Func<int,byte?,object>)ppu.Fget },
                { "fillp", (Action<int>)ppu.Fillp },
                { "fset", (Action<int,byte?,bool?>)ppu.Fset },
                { "line", (PictureProcessingUnit.APILine)ppu.Line },
                { "pal",  (Action<int,byte?,byte>)ppu.Pal },
                { "palt", (Action<byte,bool?>)ppu.Palt },
                { "pget", (Func<int?,int?,byte>)ppu.Pget },
                { "print", (Action<string,int?,int?,int?>)ppu.Print },
                { "pset", (Action<int?,int?,int?>)ppu.Pset },
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
                RegisterAPI(api.Key, api.Value);
            }

            // Lua Implemented API
            Run(@"
            function tostr(x)
                if (type(x) == ""number"") return tostring(math.floor(x*10000)/10000)
                return tostring(x)
            end

            function del(t, dv)
                if (t == nil) return
                for i=1, #t do
                    if t[i] == dv then
                        table.remove(t, i)
                        return
                    end
                end
            end

            function add(t,v)
                if (t == nil) return
                table.insert(t,v)
            end

            function foreach(t,f)
                for v in all(t) do
                    f(v)
                end
            end

            function all(t)
                if (t == nil or #t == 0) return function() end
                local i, li=1
                return function()
                    if (t[i]==li) then i=i+1 end
                    while(t[i]==nil and i<=#t) do i=i+1 end
                    li=t[i]
                    return t[i]
                end
            end

            function count(t)
	            return #t
            end

            sub = string.sub
            cocreate = coroutine.create
            coresume = coroutine.resume
            costatus = coroutine.status
            yield = coroutine.yield

            ");
        }

		#region PICO8SYNTAX
        private static string IfShorthandMatch(Match match)
        {
            string exp = match.Groups[1].ToString();
            if (match.Groups[1].ToString().Contains("then"))
            {
                return match.ToString();
            }
            int nextChar = 0;

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

        private static string UnaryAssignmentMatch(Match match)
        {
            string potentialExp = Regex.Replace(match.Groups[3].ToString(), @"\.\s+", _ => ".");
            string realExp = "";

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
            return result;

        }
        
        private static string ProcessUnaryAssignment(string str)
        {
            return Regex.Replace(str, @"([a-zA-Z_](?:[a-zA-Z0-9_]|(?:\.\s*))*(?:\[.*\])?)\s*([+\-*\/%])=\s*(.*)$", UnaryAssignmentMatch, RegexOptions.Multiline);
        }

        private static string ProcessIfShorthand(string str)
        {
            return Regex.Replace(str, @"[iI][fF]\s*(\(.*)$", IfShorthandMatch, RegexOptions.Multiline);
        }

        protected static void Preprocess(ref string script)
        {
            script = script.Replace("!=", "~=");
            script = ProcessIfShorthand(ProcessUnaryAssignment(script));
        }
        #endregion

		#region PICO8API
        protected bool Btn(int b, int p = -1) {
            return buttons[b];
        }

        protected bool Btnp(int b, int p = -1) {
            return buttons[b] && !lastButtons[b];
        }
		#endregion

		
        public void LoadCartridge(Cartridge cart) {
            cartridge = cart;
            // Copy to memory
            memory.CopyFromROM(cart.ROM, 0, 0x4300);
            string script = cart.ExtractScript();
            
            Run(script); 
            Call("_init");
        }
        public void SendInput(Buttons button) {
            buttons[(int)button] = true;
        }
		public abstract void Run(string script);
		public abstract void Call(string func, params object[] args);

		public void Update() {
            Call("_update");
            Call("_draw");
            ppu.Flip();

            bool[] tmp = lastButtons;
            lastButtons = buttons;
            buttons = tmp;

            for (int i = 0; i < (int)Buttons.UNDEFINED; i++) {
                buttons[i] = false;
            }
		}
	}

}