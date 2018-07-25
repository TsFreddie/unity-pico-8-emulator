using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace TsFreddie.Pico8
{
    public class PictureProcessingUnit
    {
        public Texture2D Texture { get; private set; }
        public byte[] screen;
        public PictureProcessingUnit(MemoryModule memory) {
            Texture = new Texture2D(64, 128, TextureFormat.R8, false, true);
            Texture.filterMode = FilterMode.Point;
            screen = memory.GetVideoBuffer();
            Texture.LoadRawTextureData(screen);
            Texture.Apply();
        }

        public void Flip() {
            Texture.LoadRawTextureData(screen);
            Texture.Apply();
        }
    }
}
