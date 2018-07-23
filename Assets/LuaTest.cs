using System.Collections;
using System.Collections.Generic;
using System.IO;
using TsFreddie.Pico8;
using UnityEngine;

public class LuaTest : MonoBehaviour {

    public string filename;

    // Use this for initialization
    void Start () {
        Cartridge cart = new Cartridge();
        MemoryModule memory = new MemoryModule();
        ScriptProcessor processor = new ScriptProcessor();
        // Read from cart
        cart.LoadFromP8PNG(filename);
        // Copy to memory
        memory.CopyFromROM(cart.ROM, 0, 0x4300);
        // Load script
        string code = cart.ExtractScript();
        processor.Load(code);
        // Load script


    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
