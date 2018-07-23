using System;
using System.IO;
using Hjg.Pngcs;
using UnityEngine;

namespace TsFreddie.Pico8
{
    public class Cartridge
    {
        const int ROM_SIZE = 0x8000;
        const int META_SIZE = 0x5;
        const int CART_SIZE = ROM_SIZE + META_SIZE;

        public byte[] ROM {
            get
            {
                return rom;
            }
        }

        public byte Version { get; private set; }
        public int Build { get; private set; }

        readonly byte[] rom;
        //Color32[] cart;

        public Cartridge()
        {
            rom = new byte[CART_SIZE];
        }

        #region Helpers
        /*
        static byte decodeColor32(Color32 c)
        {
            return (byte)(((c.a & 0x03) << 6) + ((c.r & 0x03) << 4) + ((c.g & 0x03) << 2) + (c.b & 0x03));
        }
        */

        static byte decodeBytes(int[] data, int offset)
        {
            int r = data[offset];
            int g = data[offset + 1];
            int b = data[offset + 2];
            int a = data[offset + 3];
            return ((byte)(((a & 0x03) << 6) + ((r & 0x03) << 4) + ((g & 0x03) << 2) + (b & 0x03)));
        }

        static bool validatePNG(ImageInfo info)
        {
            if (info.Cols != 160 || info.Rows != 205 || !info.Alpha || info.Channels != 4 || info.BitspPixel != 32)
                return false;
            return true;
        }
        #endregion

        /*
        public void LoadFromTexture(Texture2D texture)
        {
            string lua;

            cart = texture.GetPixels32();

            for (int i = 0; i < 32768; i++)
            {
                rom[i] = decodeColor32(cart[i]);
            }
            Version = decodeColor32(cart[0x8000]);
            Build = (decodeColor32(cart[0x8001]) << 24) + (decodeColor32(cart[0x8002]) << 16) + (decodeColor32(cart[0x8003]) << 8) + decodeColor32(cart[0x8004]);
        }
        */

        public void LoadFromP8(string filename)
        {
        }

        public void LoadFromP8PNG(string filename)
        {
            PngReader reader = new PngReader(new FileStream(filename, FileMode.Open));
            if (!validatePNG(reader.ImgInfo))
                throw new BadImageFormatException("Bad Cart");

            int offset = 0;
            for (int row = 0; row < reader.ImgInfo.Rows && offset < CART_SIZE; row++)
            {
                ImageLine line = reader.ReadRowInt(row);
                for (int col = 0; col < line.ImgInfo.Cols && offset < CART_SIZE; col++)
                {
                    rom[offset] = decodeBytes(line.Scanline, col * 4);
                    offset += 1;
                }
            }

            Version = rom[0x8000];
            Build = (rom[0x8001] << 24) + (rom[0x8002] << 16) + (rom[0x8003] << 8) + rom[0x8004];
            /*
            Debug.Log(rom[0]);
            Debug.Log(rom[1]);
            Debug.Log(rom[2]);
            Debug.Log(rom[3]);
            */

        }
        public void LoadFromUrl(string url)
        {

        }

        public string ExtractScript()
        {
            string script = "";
            int index = 0x4300;
            while (index < ROM_SIZE || rom[index] != 0x00)
            {
                script += (char)rom[index];
                index++;
            }

            // uncompressed code
            if (script.Equals(":c:")) return script;

            int size = (rom[0x4304] << 8) + rom[0x4305];
            index = 0x4308;

            script = "";
            string lut = "\n 0123456789abcdefghijklmnopqrstuvwxyz!#%(){}[]<>+=/*:;.,~_";

            // comsume compressed data
            while (script.Length < size && index < 0x8000)
            {
                byte current_byte = rom[index];
                if (current_byte == 0x00)
                {
                    byte next_byte = rom[++index];
                    script += (char)next_byte;
                }
                else if (current_byte < 0x3c)
                {
                    script += lut[current_byte - 0x01];
                }
                else
                {
                    byte next_byte = rom[++index];
                    int copy_offset = (current_byte - 0x3c) * 16 + (next_byte & 0x0f);
                    int copy_length = (next_byte >> 4) + 2;
                    script += script.Substring(script.Length-copy_offset, copy_length);
                }
                index++;
            }
            return script;
        }
    }
}


