using System.Collections;
using System.Collections.Generic;
using System.IO;
using TsFreddie.Pico8;
using UnityEngine;
using System;

public class LuaTest : MonoBehaviour {

    public string filename;
    public MeshRenderer mesh;
    NLuaPicoEmulator processor;
    // Use this for initialization
    void Start () {
        //int dis = Screen.currentResolution.refreshRate;
        QualitySettings.vSyncCount = 2;

        Cartridge cart = new Cartridge();
        
        processor = new NLuaPicoEmulator();
        WWW www = new WWW(filename);

        while(!www.isDone);

        // Read from cart
        cart.LoadFromP8PNG(new MemoryStream(www.bytes));
        // Load script
        processor.LoadCartridge(cart);

        mesh.material.SetTexture("_MainTex", processor.Texture);
        /* 
        processor.Run(@"a = {5,6} b = {""haha""}");
        processor.Run(@"add(a, b)");
        processor.Run(@"foreach(a, dprint)");
        processor.Run(@"del(a, b)");
        processor.Run(@"foreach(a, dprint)");
        
        processor.Run(@"function next_room()
        dprint(""next_room"")
 if room.x==2 and room.y==1 then
  music(30,500,7)
 elseif room.x==3 and room.y==1 then
  music(20,500,7)
 elseif room.x==4 and room.y==2 then
  music(30,500,7)
 elseif room.x==5 and room.y==3 then
  music(30,500,7)
 end

	if room.x==7 then
		load_room(0,room.y+1)
	else
		load_room(room.x+1,room.y)
	end
end");*/
        
        #if UNITY_EDITOR
            //QualitySettings.vSyncCount = 0;  // VSync must be disabled
        #endif
        //Application.targetFrameRate = 15;
    }

	void Update () {
        // Input
        foreach (Touch t in Input.touches) {
			Vector2 position = Camera.main.ScreenToWorldPoint(t.position);
			Collider2D[] colliders = Physics2D.OverlapPointAll(position);
			foreach (Collider2D collider in colliders) {
                TouchButton button = collider.GetComponent<TouchButton>();
				processor.SendInput(button.button);
                //button.SetState(true);
			}
		}

        if (Input.GetKey(KeyCode.N)) processor.SendInput(NLuaPicoEmulator.Buttons.CIRCLE);
        if (Input.GetKey(KeyCode.E)) processor.SendInput(NLuaPicoEmulator.Buttons.CROSS);
        if (Input.GetKey(KeyCode.W)) processor.SendInput(NLuaPicoEmulator.Buttons.UP);
        if (Input.GetKey(KeyCode.A)) processor.SendInput(NLuaPicoEmulator.Buttons.LEFT);
        if (Input.GetKey(KeyCode.S)) processor.SendInput(NLuaPicoEmulator.Buttons.RIGHT);
        if (Input.GetKey(KeyCode.D)) processor.SendInput(NLuaPicoEmulator.Buttons.DOWN);
        processor.Update();
	}
}
