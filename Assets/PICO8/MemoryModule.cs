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
        #region ByteTable
        public const int    ADDR_SPRITE = 0x0,
                            ADDR_SHARED = 0x1000,
                            ADDR_MAP = 0x2000,
                            ADDR_FLAGS = 0x3000,
                            ADDR_MUSIC = 0x3100,
                            ADDR_SOUND = 0x3200,
                            ADDR_GENERAL = 0x4300,
                            ADDR_PALETTE_0 = 0x5f00,
                            ADDR_PALETTE_1 = 0x5f10,
                            ADDR_CLIP_X0 = 0x5f20,
                            ADDR_CLIP_Y0 = 0x5f21,
                            ADDR_CLIP_X1 = 0x5f22,
                            ADDR_CLIP_Y1 = 0x5f23,
                            ADDR_PEN = 0x5f25,
                            ADDR_CARTDATA = 0x5e00,
                            ADDR_CURSOR_X = 0x5f26,
                            ADDR_CURSOR_Y = 0x5f27,
                            ADDR_CAMERA_X = 0x5f28,
                            ADDR_CAMERA_Y = 0x5f2a,
                            ADDR_SCREEN_EFFECT = 0x5f2c,
                            ADDR_DEVKIT = 0x5f2d,
                            ADDR_PALETTE_LOCK = 0x5f2e,
                            ADDR_CANCEL_PAUSE = 0x5f34,
                            ADDR_FILL = 0x5f31,
                            ADDR_PRO_COLOR = 0x5f34,
                            ADDR_VRAM = 0x6000;
        #endregion
        readonly byte[] ram;
        readonly byte[] screen;
        public byte[] VideoBuffer { get { Buffer.BlockCopy(ram, 0x6000, screen, 0, 0x2000); return screen; } }

        #region DrawStates
        // Memory caches
        private int? _camera_x = null;
        private int? _camera_y = null;

        public byte DrawColor {
            get {
                return (byte)(ram[ADDR_PALETTE_0 + Pen] & 0xf);
            }
        }
        public byte Pen{
            get { return (byte)(ram[ADDR_PEN] & 0xf); }
            set { ram[ADDR_PEN] = (byte)(ram[ADDR_PEN] & 0xf0 | value & 0xf); }
        }

        public byte ClipX0 { get { return ram[ADDR_CLIP_X0]; } set { ram[ADDR_CLIP_X0] = value; } }
        public byte ClipY0 { get { return ram[ADDR_CLIP_Y0]; } set { ram[ADDR_CLIP_Y0] = value; } }
        public byte ClipX1 { get { return ram[ADDR_CLIP_X1]; } set { ram[ADDR_CLIP_X1] = value; } }
        public byte ClipY1 { get { return ram[ADDR_CLIP_Y1]; } set { ram[ADDR_CLIP_Y1] = value; } }

        public int CameraX {
            get {
                if (!_camera_x.HasValue) {
                    _camera_x = ((sbyte)(ram[ADDR_CAMERA_X+1]) << 8) | ram[ADDR_CAMERA_X];
                }
                return _camera_x.Value;
            }
            set {
                _camera_x = value;
                ram[ADDR_CAMERA_X] = (byte)(value & 0xff);
                ram[ADDR_CAMERA_X+1] = (byte)(value >> 8);
            }
        }
        public int CameraY {
            get {
                if (!_camera_y.HasValue) {
                    _camera_y = ((sbyte)(ram[ADDR_CAMERA_Y+1]) << 8) | ram[ADDR_CAMERA_Y];
                }
                return _camera_y.Value;
            }
            set {
                _camera_y = value;
                ram[ADDR_CAMERA_Y] = (byte)(value & 0xff);
                ram[ADDR_CAMERA_Y+1] = (byte)(value >> 8);
            }
        }

        #endregion

        public MemoryModule()
        {
            // 32K Base memory
            ram = new byte[0x8000];
            screen = new byte[0x2000];
        }
        
        public void InitializeStates() {
            ram[ADDR_PALETTE_0] = 0x10;
            ram[ADDR_PALETTE_1] = 0;
            ram[ADDR_CLIP_X1] = 127;
            ram[ADDR_CLIP_Y1] = 127;
            for (byte i = 1; i < 16; i++) {
                ram[ADDR_PALETTE_0 + i] = i;
                ram[ADDR_PALETTE_1 + i] = i;
            }
        }

        public void CopyFromROM(byte[] rom, int offset, int length)
        {
            Buffer.BlockCopy(rom, offset, ram, offset, length);
        }

        public byte Peek(int addr) {
            if (addr <= 0 || addr >= 0x8000)
                return 0;
        
            return ram[addr];
        }

        public void Poke(int addr, byte val) {
            if (addr >= ADDR_CAMERA_X && addr <= ADDR_CAMERA_X + 1) _camera_x = null;
            if (addr >= ADDR_CAMERA_Y && addr <= ADDR_CAMERA_Y + 1) _camera_y = null;
            // TODO: throw exceptions
            if (addr <= 0 || addr >= 0x8000)
                return;

            ram[addr] = val;
        }

        public void MemSet(int start, byte val, int len) { 
            if (start <= ADDR_CAMERA_X + 1 || start + len >= ADDR_CAMERA_X) _camera_x = null;
            if (start <= ADDR_CAMERA_Y + 1 || start + len >= ADDR_CAMERA_Y) _camera_y = null;

            int block = 4;
            int index = start;
            int length = Math.Min(start + block, start + len);

            while (index < length) {
                ram[index++] = val;
            }

            length = start + len;
            while (index < length) {
                Buffer.BlockCopy(ram, start, ram, index, Math.Min(block, length-index));
                index += block;
                block *= 2;
            }
        }

        public void MemCpy(int dst_addr, int src_addr, int len) {
            if (dst_addr <= ADDR_CAMERA_X + 1 || dst_addr + len >= ADDR_CAMERA_X) _camera_x = null;
            if (dst_addr <= ADDR_CAMERA_Y + 1 || dst_addr + len >= ADDR_CAMERA_Y) _camera_y = null;
            
            Buffer.BlockCopy(ram, src_addr, ram, dst_addr, len);
        }
    }
}


