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
        public enum BYTE_TABLE : UInt16 {
            SPRITE = 0x0,
            MAP = 0x1000,
            FLAGS = 0x3000,
            MUSIC = 0x3100,
            SOUND = 0x3200,
            GENERAL = 0x4300,
            CARTDATA = 0x5e00,
            CURSOR_X = 0x5f26,
            CURSOR_Y,
        }
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

        public byte Peek(UInt16 addr) {
            if (addr < 0 || addr >= 0x8000)
                return 0;

            // accessing vram
            if (addr >= 0x6000)
                return screen[addr-0x6000];
        
            return ram[addr];
        }

        public void Poke(UInt16 addr, byte val) {
            // TODO: throw exceptions
            if (addr < 0 || addr >= 0x8000)
                return;

            // accessing vram
            if (addr >= 0x6000) {
                screen[addr-0x6000] = val;
                return;
            }

            ram[addr] = val;
        }
    }
}


