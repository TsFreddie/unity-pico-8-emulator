using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using NLua;

namespace TsFreddie.Pico8
{
    public class NLuaPicoEmulator : PicoEmulator
    {
        Lua engine;

        protected override void InitializeLuaEngine() {
            engine = new Lua();
        }

        public override void Call(string func, params object[] args)
        {
            if (engine[func] != null) 
                (engine[func] as LuaFunction).Call(args);
        }

        public override void Run(string script)
        {
            Preprocess(ref script);
            engine.DoString(script);
        }

        protected override void RegisterAPI(string apiName, object func)
        {
            engine[apiName] = func;
        }
    }
}

