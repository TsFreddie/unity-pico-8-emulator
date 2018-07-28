using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace TsFreddie.Pico8
{
    public class PictureProcessingUnit
    {
        public Texture2D Texture { get; private set; }
        public MemoryModule memory;
        public PictureProcessingUnit(MemoryModule memory) {
            Texture = new Texture2D(64, 128, TextureFormat.R8, false, true);
            Texture.filterMode = FilterMode.Point;
            this.memory = memory;
            Flip();
        }

        public void Flip() {
            Texture.LoadRawTextureData(memory.VideoBuffer);
            Texture.Apply();
            // TODO: Wait for frame
        }

        public byte PeekHalf(ushort offset, int element) {
            ushort addr = (ushort)(offset + element / 2);

            if (addr >= 0x8000)
                return 0;

            byte result = memory.Peek(addr);
            if (element % 2 == 0) {
                result = (byte)(result & 0x0f);
            } else {
                result = (byte)(result >> 4);
            }
            return result;
        }

        public void PokeHalf(ushort offset, int element, byte val) {
            ushort addr = (ushort)(offset + element / 2);

            byte result = memory.Peek(addr);
            if (element % 2 == 0) {
                result = (byte)(result & 0xf0);
                val = (byte)(val & 0x0f);
            } else {
                result = (byte)(result & 0x0f);
                val = (byte)(val << 4);
            }
            memory.Poke(addr, (byte)(result + val));
        }

#region APIDelegate
        public delegate void APILine(int x0, int y0, int x1, int y1, byte col);
        public delegate void APIRect(int x0, int y0, int x1, int y1, byte col);
        public delegate void APISpr(int n, int x, int y, double w, double h, bool flip_x, bool flip_y);
        public delegate void APISspr(int sx, int sy, int sw, int sh, int dx, int dy, int dw, int dh, bool flip_x, bool flip_y);
        public delegate void APIMap(int cel_x, int cel_y, int sx, int sy, int cel_w, int cel_h, int layer);
#endregion
#region APITODO
        public void Camera(int x = 0, int y = 0) {}
        public void Circ(int x, int y, int r, byte col = 0) {}      
        public void Circfill(int x, int y, int r, byte col = 0) {}
        public void Clip(int x, int y, int w, int h) {}
        public void Cls() {}
        public void Color(byte col) {}
        public void Cursor(int x, int y) {}
        public int Fget(int n, byte f = 0) { return 0; }
        public void Fillp(int pat = 0) {}
        public void Fset(int n, int f = 0, bool val = false) {}
        public void Line(int x0, int y0, int x1, int y1, byte col = 0) {}
        public void Pal(byte c0 = 0, byte c1 = 0, byte p = 0) {}
        public void Palt(byte c = 0, bool t = false) {}
        public byte Pget(int x, int y) {return 0;}
        public void Print(string str, int x = 0, int y = 0, byte col = 0) {}
        public void Pset(int x, int y, byte col = 0) {}
        public void Rect(int x0, int y0, int x1, int y1, byte col = 0) {}
        public void Rectfill(int x0, int y0, int x1, int y1, byte col = 0) {}
        public byte Sget(int x, int y) { return 0; }
        public void Sset(int x, int y, byte c = 0) {}
        public void Sspr(int sx, int sy, int sw, int sh, int dx, int dy, int dw = 0, int dh = 0, bool flip_x = false, bool flip_y = false) {}
        public void Map(int cel_x, int cel_y, int sx, int sy, int cel_w, int cel_h, int layer = 0) {}
        public byte Mget(int x, int y) { return 0; }
        public void Mset(int x, int y, byte v) {}

#endregion

#region APIImplementation  
        public void Spr(int n, int x, int y, double w = 1, double h = 1, bool flip_x = false, bool flip_y = false) {
            // TODO: flip
            int width = (int)(8 * w);
            int height = (int)(8 * h);

            int spr_offset = (n*4)%64+n/16;

            for (int yy = 0; yy < height; yy++) {
                for (int xx = 0; xx < width; xx++) {
                    int final_y = y+yy;
                    int final_x = x+xx;
                    if (final_y < 0 || final_x < 0 || final_y >= 128 || final_x >= 128) {
                        continue;
                    }

                    byte color = PeekHalf((ushort)(MemoryModule.BYTE_TABLE.SPRITE+spr_offset), yy*128+xx);
                    // TODO: Transparency based on drawstate
                    if (color == 0) {
                        continue;
                    }
                    PokeHalf((ushort)MemoryModule.BYTE_TABLE.VRAM, final_y*128+final_x, color);
                }
            }
        }
#endregion
    }
}
