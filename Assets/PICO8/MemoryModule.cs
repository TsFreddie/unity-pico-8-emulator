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
        public enum BYTE_TABLE : int {
            SPRITE = 0x0,
            MAP = 0x1000,
            FLAGS = 0x3000,
            MUSIC = 0x3100,
            SOUND = 0x3200,
            GENERAL = 0x4300,
            PALETTE_0 = 0x5f00,
            PALETTE_1 = 0x5f10,
            CLIP_X0 = 0x5f20,
            CLIP_Y0,
            CLIP_X1,
            CLIP_Y1,
            PEN = 0x5f25,
            CARTDATA = 0x5e00,
            CURSOR_X = 0x5f26,
            CURSOR_Y,
            CAMERA_X = 0x5f28,
            CAMERA_Y = 0x5f2a,
            SCREEN_EFFECT = 0x5f2c,
            DEVKIT,
            PALETTE_LOCK,
            CANCEL_PAUSE = 0x5f34,
            FILL = 0x5f31,
            PRO_COLOR = 0x5f34,
            VRAM = 0x6000,
        }
        readonly byte[] ram;
        readonly byte[] screen;
        public byte[] VideoBuffer { get { Buffer.BlockCopy(ram, 0x6000, screen, 0, 0x2000); return screen; } }

        public MemoryModule()
        {
            // 32K Base memory
            ram = new byte[0x8000];
            screen = new byte[0x2000];
        }
        
        public void CopyFromROM(byte[] rom, int offset, int length)
        {
            Buffer.BlockCopy(rom, offset, ram, offset, length);
        }

        public byte Peek(ushort addr) {
            if (addr >= 0x8000)
                return 0;
        
            return ram[addr];
        }

        public void Poke(ushort addr, byte val) {
            // TODO: throw exceptions
            if (addr >= 0x8000)
                return;

            ram[addr] = val;
        }

        public void MemSet(ushort start, byte val, ushort len) {
            ushort block = 4;
            ushort index = start;
            ushort length = (ushort)Math.Min(start + block, start + len);

            while (index < length) {
                ram[index++] = val;
            }

            length = (ushort)(start + len);
            while (index < length) {
                Buffer.BlockCopy(ram, start, ram, index, Math.Min(block, length-index));
                index += block;
                block *= 2;
            }
        }

        public void MemCpy(ushort dst_addr, ushort src_addr, ushort len) {
            Buffer.BlockCopy(ram, src_addr, ram, dst_addr, len);
        }
    }
}


