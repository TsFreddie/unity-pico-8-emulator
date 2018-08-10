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

#region Helper
        public byte PeekHalf(int offset, int element) {
            int addr = offset + element / 2;

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

        public void PokeHalf(int offset, int element, byte val) {
            int addr = offset + element / 2;

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

        public void PokeScreen(int x, int y, byte color) {
            if (x < memory.ClipX0 || y < memory.ClipY0 || x > memory.ClipX1 || y > memory.ClipY1)
                return;
            PokeHalf(MemoryModule.ADDR_VRAM, y*128+x, color);
        }
#endregion

#region APIDelegate
        public delegate void APILine(int x0, int y0, int x1, int y1, int? col);
        public delegate void APIRect(int x0, int y0, int x1, int y1, int? col);
        public delegate void APISpr(int n, int x, int y, double w, double h, bool flip_x, bool flip_y);
        public delegate void APISspr(int sx, int sy, int sw, int sh, int dx, int dy, int dw, int dh, bool flip_x, bool flip_y);
        public delegate void APIMap(int cel_x, int cel_y, int sx, int sy, int cel_w, int cel_h, int layer);
#endregion

#region APITODO
        public void Cursor(int x, int y) {}
        public void Fillp(int pat = 0) {}
        public void Print(string str, int? x, int? y, int? col = null) {}
        public void Sspr(int sx, int sy, int sw, int sh, int dx, int dy, int dw = 0, int dh = 0, bool flip_x = false, bool flip_y = false) {}

#endregion

#region APIImplementation
        public void Clip(int x = 0, int y = 0, int w = 127, int h = 127) {
            int x1 = x + w;
            int y1 = y + h;
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x1 >= 128) x1 = 127;
            if (y1 >= 128) y1 = 127;
        }
        public void Circ(int? x0 = null, int? y0 = null, int r = 4, int? col = null) {
            if (!y0.HasValue) return;
            if (r < 0) return;
            r++;
            int x = r-1;
            int y = 0;
            int dx = 1;
            int dy = 1;
            int err = dx - r;

            while (x >= y)
            {
                Pset(x0 + x, y0 + y, col);
                Pset(x0 + y, y0 + x, col);
                Pset(x0 - y, y0 + x, col);
                Pset(x0 - x, y0 + y, col);
                Pset(x0 - x, y0 - y, col);
                Pset(x0 - y, y0 - x, col);
                Pset(x0 + y, y0 - x, col);
                Pset(x0 + x, y0 - y, col);

                if (err < 0)
                {
                    y++;
                    err += dy;
                    dy += 2;
                }
                if (err >= 0)
                {
                    x--;
                    dx += 2;
                    err += dx - r;
                }
            }
        }
        public void Circfill(int? x0 = null, int? y0 = null, int r = 4, int? col = null) {
            if (!y0.HasValue) return;
            if (r < 0) return;
            r++;
            int x = r-1;
            int y = 0;
            int dx = 1;
            int dy = 1;
            int err = dx - r;

            while (x >= y)
            {
                Line(x0.Value - x, y0.Value + y, x0.Value + x, y0.Value + y, col);
                Line(x0.Value - y, y0.Value + x, x0.Value + y, y0.Value + x, col);
                Line(x0.Value - x, y0.Value - y, x0.Value + x, y0.Value - y, col);
                Line(x0.Value - y, y0.Value - x, x0.Value + y, y0.Value - x, col);
                if (err < 0)
                {
                    y++;
                    err += dy;
                    dy += 2;
                }
                if (err >= 0)
                {
                    x--;
                    dx += 2;
                    err += dx - r;
                }
            }
        }
        public void Cls() {
            memory.MemSet(MemoryModule.ADDR_VRAM, 0, 0x2000);
        }

        public void Camera(int x = 0, int y = 0) {
            memory.CameraX = x;
            memory.CameraY = y;
        }
        public void Color(int col = 0) {
            memory.Pen = (byte)(col & 0xf);
        }
        public object Fget(int n = -1, byte? f = null) {
            if (n < 0 || n >= 256) return null;
            byte flag = memory.Peek(MemoryModule.ADDR_FLAGS + n);
            if (!f.HasValue) {
                return flag;
            }
            byte mask = (byte)(1 << f.Value);
            return ((flag & mask) > 0);
        }
        
        public void Fset(int n = -1, byte? f = null, bool? val = null) {
            if (n < 0 || n >= 256) return;
            if (!f.HasValue) return;
            byte flag = 0;
            if (val.HasValue) {
                flag = (byte)Fget(n);
                if (val.Value){
                    byte mask = (byte)(1 << f.Value);
                    flag = (byte)(flag | mask);

                } else {
                    byte mask = (byte)(0 << f.Value);
                    flag = (byte)(flag & mask);
                }
            } else {
                flag = f.Value;
            }
            memory.Poke(MemoryModule.ADDR_FLAGS + n, (byte)flag);
        }
        public void Line(int x0 = 0, int y0 = 0, int x1 = 0, int y1 = 0, int? col = null) {
            int dx = x1 - x0;
            int dy = y1 - y0;
            int D = 2*dy - dx;
            int y = y0;

            for (int x = x0; x <= x1; x++) {
                Pset(x,y,col);
                if (D > 0) {
                    y += 1;
                    D = D - 2*dx;
                }
                D = D + 2*dy;
            }
        }
        public byte Pget(int? x, int? y) {
            if (!x.HasValue || !y.HasValue) return 0;
            int real_x = x.Value - memory.CameraX;
            int real_y = y.Value - memory.CameraY;

            if (real_x < 0 || real_x >= 128 || real_y < 0 || real_y >= 128)
                return 0;

            return PeekHalf(MemoryModule.ADDR_VRAM, real_y*128+real_x);
        }
        public void Pset(int? x, int? y, int? col) {
            if (!x.HasValue || !y.HasValue) return;
            int real_x = x.Value - memory.CameraX;
            int real_y = y.Value - memory.CameraY;

            if (col.HasValue)
                Color(col.Value);

            if (real_x < 0 || real_x >= 128 || real_y < 0 || real_y >= 128)
                return;

            PokeScreen(real_x, real_y, memory.DrawColor);
        }
        public void Pal(int c0 = 0, byte? c1 = null, byte p = 0) {
            if (!c1.HasValue) {
                memory.Poke(MemoryModule.ADDR_PALETTE_0, 0x10);
                for (byte i = 1; i < 16; i++) {
                    memory.Poke(MemoryModule.ADDR_PALETTE_0 + i, i);
                }
                return;
            }
            int addr = (p == 0) ? MemoryModule.ADDR_PALETTE_0 : MemoryModule.ADDR_PALETTE_1;
            PokeHalf(addr + (c0 & 0xf), 0, (byte)(c1.Value & 0xf));
        }
        public void Palt(byte c = 0, bool? t = false) {
            if (!t.HasValue) {
                PokeHalf(MemoryModule.ADDR_PALETTE_0, 1, 1);
                for (byte i = 1; i < 16; i++) {
                    PokeHalf(MemoryModule.ADDR_PALETTE_0 + i, 1, 0);
                }
                return;
            }
            PokeHalf(MemoryModule.ADDR_PALETTE_0 + (c & 0xf), 1, (byte)(t.Value ? 1 : 0));
        }
        public void Rect(int x0 = 0, int y0 = 0, int x1 = 0, int y1 = 0, int? col = null) {
            if (y0 >= 0 && y0 < 128) {
                for (int x = x0; x <= x1; x++) {
                    if (x < 0 || x >= 128) continue;
                    Pset(x, y0, col);
                }
            }
            for (int y = y0; y <= y1; y++) {
                if (y < 0 || y >= 128) continue;
                if (x0 >= 0 && x0 < 128) {
                    Pset(x0, y, col);
                }
                if (x1 >= 0 && x1 < 128) {
                    Pset(x1, y, col);
                }
            }
            if (y1 >= 0 && y1 < 128) {
                for (int x = x0; x <= x1; x++) {
                    if (x < 0 || x >= 128) continue;
                    Pset(x, y1, col);
                }
            }
        }

        public void Rectfill(int x0 = 0, int y0 = 0, int x1 = 0, int y1 = 0, int? col = null) {
            for (int y = y0; y <= y1; y++) {
                if (y < 0 || y >= 128) continue;
                for (int x = x0; x <= x1; x++) {
                    if (x < 0 || x >= 128) continue;
                    Pset(x, y, col);
                }
            }
        }
        public byte Sget(int x = 0, int y = 0) {
            return PeekHalf(MemoryModule.ADDR_SPRITE, y*128+x); 
        }

        public void Sset(int x = 0, int y = 0, byte c = 0) {
            PokeHalf(MemoryModule.ADDR_SPRITE, y*128+x, 0); 
        }

        public void Spr(int n = 0, int x = 0, int y = 0, double w = 1, double h = 1, bool flip_x = false, bool flip_y = false) {
            // TODO: flip
            int width = (int)(8 * w);
            int height = (int)(8 * h);

            int spr_x = (n % 16) * 8;
            int spr_y = (n / 16) * 8;

            for (int yy = 0; yy < height; yy++) {
                int final_y = y+yy-memory.CameraY;
                int s_y = spr_y+yy;
                if (final_y < 0 || final_y >= 128) continue;
                for (int xx = 0; xx < width; xx++) {
                    int final_x = x+xx-memory.CameraX;
                    if (final_x < 0 || final_x >= 128) continue;
                    
                    byte color = 0;
                    if (flip_x) {
                        color = Sget(spr_x+(width-xx-1), s_y);
                    } else {
                        color = Sget(spr_x+xx, s_y);
                    }
                    if (PeekHalf(MemoryModule.ADDR_PALETTE_0 + color, 1) > 0) {
                        continue;
                    }
                    PokeScreen(final_x, final_y, PeekHalf(MemoryModule.ADDR_PALETTE_0 + color, 0));
                }
            }
        }
        public void Map(int cel_x = 0, int cel_y = 0, int sx = 0, int sy = 0, int cel_w = 16, int cel_h = 16, int layer = 0) {
            int cel_y1 = cel_y + cel_h;
            int cel_x1 = cel_x + cel_w;
            for (int cy_i = cel_y; cy_i < cel_y1; cy_i++) {
                if (cy_i < 0 || cy_i >= 64) continue;
                int map_offset = MemoryModule.ADDR_MAP;
                int cy = cy_i;
                if (cy_i >= 32) {
                    map_offset = MemoryModule.ADDR_SHARED;
                    cy -= 32;
                }
                for (int cx = cel_x; cx < cel_x1; cx++) {
                    if (cx < 0 || cx >= 128) continue;
                    int mem_offset = map_offset + cy*128+cx;
                    int spr_n = memory.Peek(mem_offset);

                    if (spr_n == 0) continue;

                    byte flag = (byte)Fget(spr_n);
                    if ((flag & layer) != layer) {
                        continue;
                    }
                    Spr(spr_n, sx + (cx-cel_x) * 8, sy + (cy_i-cel_y) * 8);
                }
            }
        }
        public byte Mget(int x = 0, int y = 0) {
            if (x < 0 || x >= 128 || y < 0 || y >= 128) return 0;
            int map_offset = MemoryModule.ADDR_MAP;
            if (y >= 32) {
                map_offset = MemoryModule.ADDR_SHARED;
                y -= 32;
            }
            int mem_offset = map_offset + y*128+x;
            byte spr = memory.Peek(mem_offset);
            return spr;
        }

        public void Mset(int x = 0, int y = 0, byte v = 0) {
            if (x < 0 || x >= 128 || y < 0 || y >= 128) return;
            int map_offset = MemoryModule.ADDR_MAP;
            if (y >= 32) {
                map_offset = MemoryModule.ADDR_SHARED;
                y -= 32;
            }
            int mem_offset = map_offset + y*128+x;
            memory.Poke(mem_offset, v);
        }
#endregion
    }
}
