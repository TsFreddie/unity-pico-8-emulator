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
        PicoEmulator processor = new PicoEmulator();
        // Read from cart
        cart.LoadFromP8PNG(filename);
        // Load script
        processor.LoadCartridge(cart);
        // Coroutine Test
        processor.Run(@"while true do dprint(4) end");



    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
