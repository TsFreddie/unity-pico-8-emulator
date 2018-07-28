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
        mesh.material.SetTexture("_MainTex", processor.Texture);
    }
	
	// Update is called once per frame
	void Update () {
        processor.Call("_update");
        //processor.Call("_draw");
        processor.Run("flip()");
        //processor.Run("poke(rnd(0x2000) + 0x6000, rnd(0xff)) flip()");
	}
}
