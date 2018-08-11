using MoonSharp.Interpreter;
using MoonSharp.VsCodeDebugger;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace TsFreddie.Pico8
{
    public class MoonSharpPicoEmulator : PicoEmulator
    {
        Script engine;

        protected override void InitializeLuaEngine() {
            engine = new Script();
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
        protected override void RegisterAPI(string apiName, object func)
        {
            engine.Globals[apiName] = func;
        }

        public override void Run(string script)
        {
            Preprocess(ref script);
            engine.DoString(script);
        }

        public override void Call(string func, params object[] args)
        {
            if (engine.Globals[func] != null) {
                engine.Call(engine.Globals[func], args);
            }
        }
    }
}

