using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using MoonSharp.Interpreter;

namespace TsFreddie.Pico8
{
    public class PictureProcessingUnit
    {
        public Texture2D Texture { get; private set; }
        public byte Pen{
            get { return memory.Peek((ushort)MemoryModule.BYTE_TABLE.PEN); }
            set { memory.Poke((ushort)MemoryModule.BYTE_TABLE.PEN, value); }
        } 
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
        public delegate void APILine(int x0, int y0, int x1, int y1, int col);
        public delegate void APIRect(int x0, int y0, int x1, int y1, int? col);
        public delegate void APISpr(int n, int x, int y, double w, double h, bool flip_x, bool flip_y);
        public delegate void APISspr(int sx, int sy, int sw, int sh, int dx, int dy, int dw, int dh, bool flip_x, bool flip_y);
        public delegate void APIMap(int cel_x, int cel_y, int sx, int sy, int cel_w, int cel_h, int layer);
#endregion
#region APITODO
        public void Camera(int x = 0, int y = 0) {}
        public void Circ(int x, int y, int r, int col = -1) {}      
        public void Circfill(int x, int y, int r, int col = -1) {}
        public void Clip(int x, int y, int w, int h) {}
        public void Cls() {}
        public void Cursor(int x, int y) {}
        public void Fillp(int pat = 0) {}
        public void Fset(int n, int f = 0, bool val = false) {}
        public void Line(int x0, int y0, int x1, int y1, int col = -1) {}
        public void Pal(byte c0 = 0, byte c1 = 0, byte p = 0) {}
        public void Palt(byte c = 0, bool t = false) {}
        public byte Pget(int x, int y) {return 0;}
        public void Print(string str, int x = 0, int y = 0, int col = -1) {}
        public void Pset(int x, int y, int col = -1) {}
        public byte Sget(int x, int y) { return 0; }
        public void Sset(int x, int y, byte c = 0) {}
        public void Sspr(int sx, int sy, int sw, int sh, int dx, int dy, int dw = 0, int dh = 0, bool flip_x = false, bool flip_y = false) {}
        public byte Mget(int x, int y) { return 0; }
        public void Mset(int x, int y, byte v) {}

#endregion

#region APIImplementation
        public void Color(int? col) {
            if (!col.HasValue) return;
            Pen = (byte)(col & 0xf);
        }
        public DynValue Fget(int n, byte? f) {
            return null;
        }
        public void Rect(int x0, int y0, int x1, int y1, int? col) {
            Color(col);
            byte color = Pen;
            if (y0 >= 0 && y0 < 128) {
                for (int x = x0; x <= x1; x++) {
                    if (x < 0 || x >= 128) continue;
                    PokeHalf((ushort)MemoryModule.BYTE_TABLE.VRAM, y0*128+x, Pen);
                }
            }
            for (int y = y0; y <= y1; y++) {
                if (y < 0 || y >= 128) continue;
                if (x0 >= 0 && x0 < 128) {
                    PokeHalf((ushort)MemoryModule.BYTE_TABLE.VRAM, y*128+x0, Pen);
                }
                if (x1 >= 0 && x1 < 128) {
                    PokeHalf((ushort)MemoryModule.BYTE_TABLE.VRAM, y*128+x1, Pen);
                }
            }
            if (y1 >= 0 && y1 < 128) {
                for (int x = x0; x <= x1; x++) {
                    if (x < 0 || x >= 128) continue;
                    PokeHalf((ushort)MemoryModule.BYTE_TABLE.VRAM, y1*128+x, Pen);
                }
            }
        }

        public void Rectfill(int x0, int y0, int x1, int y1, int? col) {
            Color(col);
            byte color = Pen;
            for (int y = y0; y <= y1; y++) {
                if (y < 0 || y >= 128) continue;
                for (int x = x0; x <= x1; x++) {
                    if (x < 0 || x >= 128) continue;
                    PokeHalf((ushort)MemoryModule.BYTE_TABLE.VRAM, y*128+x, Pen);
                }
            }
        }
        public void Spr(int n = 0, int x = 0, int y = 0, double w = 1, double h = 1, bool flip_x = false, bool flip_y = false) {
            // TODO: flip
            int width = (int)(8 * w);
            int height = (int)(8 * h);

            int spr_offset = (n*4)%64+((n/16)*512);

            for (int yy = 0; yy < height; yy++) {
                int final_y = y+yy;
                if (final_y < 0 || final_y >= 128) continue;
                for (int xx = 0; xx < width; xx++) {
                    int final_x = x+xx;
                    if (final_x < 0 || final_x >= 128) continue;

                    byte color = PeekHalf((ushort)(MemoryModule.BYTE_TABLE.SPRITE+spr_offset), yy*128+xx);
                    // TODO: Transparency based on drawstate
                    if (color == 0) {
                        continue;
                    }
                    PokeHalf((ushort)MemoryModule.BYTE_TABLE.VRAM, final_y*128+final_x, color);
                }
            }
        }
        public void Map(int cel_x = 0, int cel_y = 0, int sx = 0, int sy = 0, int cel_w = 16, int cel_h = 16, int layer = 0) {
            int cel_y1 = cel_y + cel_h;
            int cel_x1 = cel_x + cel_w;
            for (int cy_i = cel_y; cy_i < cel_y1; cy_i++) {
                if (cy_i < 0 || cy_i >= 64) continue;
                ushort map_offset = (ushort)MemoryModule.BYTE_TABLE.MAP;
                int cy = cy_i;
                if (cy_i >= 32) {
                    map_offset = (ushort)MemoryModule.BYTE_TABLE.SHARED;
                    cy -= 32;
                }
                for (int cx = cel_x; cx < cel_x1; cx++) {
                    //Debug.Log(cx);
                    //Debug.Log(cy);
                    if (cx < 0 || cx >= 128) continue;
                    ushort mem_offset = (ushort)(map_offset + cy*128+cx);
                    int spr_n = memory.Peek(mem_offset);
                    //Debug.Log(mem_offset);
                    //Debug.Log(spr_n);
                    //return;
                    Spr(spr_n, sx + (cx-cel_x) * 8, sy + (cy_i-cel_y) * 8);
                }
            }
        }
#endregion
    }
}
