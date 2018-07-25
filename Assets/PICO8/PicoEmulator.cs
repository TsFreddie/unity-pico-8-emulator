using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using FixedPointy;
using UnityEngine;

namespace TsFreddie.Pico8
{
    public class PicoEmulator
    {
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

        // Exposed data
        public Texture2D Texture { get { return ppu.Texture; } }
        public byte[] SCREEN { get { return memory.screen; } }

        #region PICO8API
        double Rnd(double x) {
            return random.NextDouble() * x;
        }

        void RegisterAPIs()
        {
            /*
            engine.Globals["abs"] = (Func<double, double>)(x => Math.Abs(x));
            engine.Globals["atan2"] = (Func<double, double, double>)((dx, dy) => 1 - Math.Atan2(dy, dx) / (2 * Math.PI));
            engine.Globals["band"] = (Func<double, double, double>)((x, y) => ((int)(x * 65536) & (int)(y * 65536)) / 65536d);
            engine.Globals["bnot"] = (Func<double, double>)(x => (~(int)(x * 65536)) / 65536d);
            engine.Globals["bor"] = (Func<double, double, double>)((x, y) => ((int)(x * 65536) | (int)(y * 65536)) / 65536d);
            engine.Globals["bxor"] = (Func<double, double, double>)((x, y) => ((int)(x * 65536) ^ (int)(y * 65536)) / 65536d);
            engine.Globals["cos"] = (Func<double, double>)(x => (~(int)(x * 65536)) / 65536d);
            */

            // C# & Lambda Implemented API
            Dictionary<String, object> apiTable = new Dictionary<String, object>()
            {
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
                { "dprint", (Action<object>)(x => Debug.Log(x)) },
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
            
            function add(t, v)
                table.insert(t, v)
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

            function del(t, dv)
                local pos = -1
                for i, v in ipairs(t) do
                    if (v == dv) pos = i break
                end
                if (pos >= 0) table.remove(t, pos)
            end
            
            function foreach(t, f)
                for k, v in pairs(t) do
                    f(v)
                end
            end

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
            // Random seed
            random = new System.Random();
            // Register APIs
            RegisterAPIs();
        }

        public void LoadCartridge(Cartridge cart) {
            cartridge = cart;
            // Copy to memory
            memory.CopyFromROM(cart.ROM, 0, 0x4300);
            Run(cart.ExtractScript()); 
        }
        public DynValue Call(string func, params DynValue[] args)
        {
            return engine.Call(engine.Globals[func], args);
        }

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
    }
}

