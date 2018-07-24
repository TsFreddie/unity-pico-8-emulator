using System;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TsFreddie.Pico8
{
    public class MemoryModule
    {
        byte[] ram;

        public MemoryModule()
        {
            // 32K Base memory
            ram = new byte[32767];
        }
        
        public void CopyFromROM(byte[] rom, int offset, int length)
        {
            Buffer.BlockCopy(rom, offset, ram, offset, length);
        }
    }
}


