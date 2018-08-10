using System.Collections;
using System.Collections.Generic;
using System.IO;
using TsFreddie.Pico8;
using UnityEngine;
using System;

public class LuaTest : MonoBehaviour {

    public string filename;
    public MeshRenderer mesh;
    PicoEmulator processor;
    // Use this for initialization
    void Start () {
        //int dis = Screen.currentResolution.refreshRate;
        //QualitySettings.vSyncCount = 2;

        Cartridge cart = new Cartridge();

        processor = new NLuaPicoEmulator();
        /* 
        WWW www = new WWW(filename);
        while(!www.isDone);
        

        cart.LoadFromP8PNG(new MemoryStream(www.bytes));
        */

        cart.LoadFromP8PNG(new FileStream(filename, FileMode.Open));
        processor.Run("srand(100)");
        //processor2.Run("srand(100)");
        // Load script
        processor.LoadCartridge(cart);
        //processor2.LoadCartridge(cart);

        mesh.material.SetTexture("_MainTex", processor.Texture);
        //mesh2.material.SetTexture("_MainTex", processor2.Texture);
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
end");
        
        #if UNITY_EDITOR
            QualitySettings.vSyncCount = 0;  // VSync must be disabled
        #endif
        Application.targetFrameRate = 30;
        */
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

        if (Input.GetKey(KeyCode.N)) processor.SendInput(PicoEmulator.Buttons.CIRCLE); //processor2.SendInput(PicoEmulator.Buttons.CIRCLE); }
        if (Input.GetKey(KeyCode.E)) processor.SendInput(PicoEmulator.Buttons.CROSS); //processor2.SendInput(PicoEmulator.Buttons.CROSS); }
        if (Input.GetKey(KeyCode.W)) processor.SendInput(PicoEmulator.Buttons.UP); //processor2.SendInput(PicoEmulator.Buttons.UP); }
        if (Input.GetKey(KeyCode.A)) processor.SendInput(PicoEmulator.Buttons.LEFT); //processor2.SendInput(PicoEmulator.Buttons.LEFT); }
        if (Input.GetKey(KeyCode.S)) processor.SendInput(PicoEmulator.Buttons.RIGHT); //processor2.SendInput(PicoEmulator.Buttons.RIGHT); }
        if (Input.GetKey(KeyCode.D)) processor.SendInput(PicoEmulator.Buttons.DOWN); //processor2.SendInput(PicoEmulator.Buttons.DOWN); }
        processor.Update();
        //processor2.Update();
	}
}
