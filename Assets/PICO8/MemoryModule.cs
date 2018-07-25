using System;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace TsFreddie.Pico8
{
    public class MemoryModule
    {
 
        byte[] ram;
        public byte[] screen;

        public MemoryModule()
        {
            // 32K Base memory
            ram = new byte[0x6000];
            screen = new byte[0x2000];
        }
        
        public void CopyFromROM(byte[] rom, int offset, int length)
        {
            Buffer.BlockCopy(rom, offset, ram, offset, length);
        }

        public byte[] GetVideoBuffer() {
            return screen;
        }
    }
}


