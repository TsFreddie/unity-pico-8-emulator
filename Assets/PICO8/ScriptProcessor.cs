using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using FixedPointy;
using UnityEngine;

namespace TsFreddie.Pico8
{
    public class ScriptProcessor
    {
        Script engine;

        // Northbridge
        MemoryModule memory;
        PictureProcessingUnit ppu;
        AudioProcessingUnit apu;

        // Southbridge
        Cartridge cartridge;
        CartridgeData cartdata;

        #region PICO8API
        
        void RegisterMathAPIs()
        {
            //engine.Globals["abs"] = (Func<double, double>)(x => Math.Abs(x));
            engine.Globals["atan2"] = (Func<double, double, double>)((dx, dy) => 1 - Math.Atan2(dy, dx) / (2 * Math.PI));
            engine.Globals["band"] = (Func<double, double, double>)((dx, dy) => ((int)(dx * 65536) & (int)(dy * 65536)) / 65536d);
        }

        #endregion

        public ScriptProcessor()
        {
            engine = new Script();
            // Register APIs
            object k = (Func<double, double>)(x => Math.Abs(x));
            engine.Globals["abs"] = k;
            RegisterMathAPIs();

        }

        private string IfShorthandMatch(Match match)
        {
            string result = string.Format("if {0} then {1} end", match.Groups[1], match.Groups[2]);
            if (match.Groups[2].ToString().Contains("then"))
            {
                result = match.Groups[0].ToString();

            }
            return result;
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
            return Regex.Replace(str, @"[iI][fF]\s*(\(.*\))\s*(.*)$", IfShorthandMatch, RegexOptions.Multiline);
        }

        public void Preprocess(ref string script)
        {
            script = ProcessIfShorthand(ProcessUnaryAssignment(script));
        }

        public void Load(string script)
        {
            Preprocess(ref script);
            engine.LoadString(script);
        }

        public DynValue Call(string func, params DynValue[] args)
        {
            return engine.Call(engine.Globals[func], args);
        }

        public DynValue Run(string script)
        {
            Preprocess(ref script);
            return engine.DoString(script);
        }
    }
}

