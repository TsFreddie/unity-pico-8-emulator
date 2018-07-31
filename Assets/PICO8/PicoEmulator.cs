using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TsFreddie.Pico8
{
	public abstract class PicoEmulator {

		#region PICO8SYNTAX
        private static string IfShorthandMatch(Match match)
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

        private static string UnaryAssignmentMatch(Match match)
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
        
        private static string ProcessUnaryAssignment(string str)
        {
            return Regex.Replace(str, @"([a-zA-Z_](?:[a-zA-Z0-9_]|(?:\.\s*))*(?:\[.*\])?)\s*([+\-*\/%])=\s*(.*)$", UnaryAssignmentMatch, RegexOptions.Multiline);
        }

        private static string ProcessIfShorthand(string str)
        {
            //[iI][fF]\s*(\(.*\))\s*(.*)$
            return Regex.Replace(str, @"[iI][fF]\s*(\(.*)$", IfShorthandMatch, RegexOptions.Multiline);
        }

        static void Preprocess(ref string script)
        {
            script = script.Replace("!=", "~=");
            script = ProcessIfShorthand(ProcessUnaryAssignment(script));
        }
        #endregion
	}

}