using System.Collections;
using System.Collections.Generic;
using System.IO;
using TsFreddie.Pico8;
using UnityEngine;

public class LuaTest : MonoBehaviour {

    public string filename;
    public MeshRenderer mesh;
    PicoEmulator processor;
    // Use this for initialization
    void Start () {
        Cartridge cart = new Cartridge();
        processor = new PicoEmulator();
        // Read from cart
        cart.LoadFromP8PNG(filename);
        // Load script
        processor.LoadCartridge(cart);
        // Coroutine Test
        processor.Run(@"while true do dprint(4) end");
        mesh.material.SetTexture("_MainTex", processor.Texture);
    }
	
	// Update is called once per frame
	void Update () {
        processor.SCREEN[0] = 0xBC;
	    //processor.SCREEN[Random.Range(0, 0x2000)] = (byte)Random.Range(0, 256);
        processor.Texture.LoadRawTextureData(processor.SCREEN);
        processor.Texture.Apply();
	}
}
