using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TsFreddie.Pico8
{
    public class AudioProcessingUnit {
        public struct AudioNote {
            public byte effect;
            public byte volume;
            public byte waveform;
            public byte pitch;
            public bool isCustom;
            public double hz;
        }
        
        public static float Oscillate(int waveform, double time, double hz) {
            switch(waveform) {
                case 0:
                    return (float)Tri(time*hz);
                case 1:
                    return (float)TiltedTri(time*hz);
                case 2:
                    return (float)Saw(time*hz);
                case 3:
                    return (float)Square(time*hz);
                case 4:
                    return (float)Pulse(time*hz);
                case 5:
                    return (float)Organ(time*hz);
                case 6:
                    return (float)Noise(time*hz);
                case 7:
                    return (float)Phaser(time*hz);
            }
            return 0;
        }

        private MemoryModule memory;
        private int[] channelSfxPointers;
        private int[] channelSfxOffset;
        private double[] channelSfxTime;
        private AudioNote[] channelSfxNote;
        private double[] channelSfxNoteLength;
        private double sampleDelta;
        private double gain = 0.5d;
        private static System.Random rand = new System.Random();

        public AudioProcessingUnit(MemoryModule memory, double sampleRate = 48000) {
            this.memory = memory;
            channelSfxPointers = new int[4]{-1,-1,-1,-1};
            channelSfxTime = new double[4]{0,0,0,0};
            channelSfxOffset = new int[4]{0,0,0,0};
            channelSfxNote = new AudioNote[4];
            channelSfxNoteLength = new double[4]{0,0,0,0};
            this.sampleDelta = 1 / sampleRate;
        }

#region Synthesis
        public static AudioNote DecodeNote(byte lowByte, byte highByte) {
            AudioNote note;
            note.pitch = (byte)(lowByte & 0x3f);
            note.volume = (byte)((highByte >> 2) & 0x7);
            note.effect = (byte)((highByte >> 5) & 0x7);
            note.waveform = (byte)((lowByte >> 6) | ((highByte & 0x1) << 2));
            note.isCustom = (highByte >> 7) > 0;
            note.hz = 440*Math.Pow(2,((note.pitch-33)/12d));
            //Debug.Log(string.Format("Decoding: {0} | {1}", lowByte, highByte));
            //Debug.Log(note.pitch);
            return note;
        }
        
        public static double Tri(double x) {
            return (Math.Abs((x % 1)*2-1)*2-1)*0.5d;
            //return (Math.Abs((x-0.5) % 2) - 1) * 2 - 1;
        }

        public static double TiltedTri(double x) {
            double t = x % 1;
            return (((t < 0.875) ? (t*16/7) : ((1-t)*16))-1) * 0.5f;
        }

        public static double Saw(double x) {
            return (x % 1 - 0.5) * 2 / 3d;
        }

        public static double Square(double x) {
            return ((x % 1 < 0.5) ? 1 : -1) * 0.25d;
        }

        public static double Pulse(double x) {
            return ((x % 1 < 0.3125) ? 1 : -1) * 0.25d;
        }

        public static double Organ(double x) {
            x = x * 4;
            return (Math.Abs((x%2)-1)-0.5d+(Math.Abs(((x*0.5)%2)-1)-0.5d)/2-0.1d)*0.5d;
        }

        public static double Noise(double x) {
            return rand.NextDouble() * 2.0 - 1.0;
        }

        public static double Phaser(double x) {
            x = x * 2;
		    return (Math.Abs(((x*127/128)%2)-1)/2d+Math.Abs((x%2)-1)-1)*2/3d;
        }
#endregion

        private void PullNodeForChannel(int channel) {
            int noteAddr = channelSfxPointers[channel]+channelSfxOffset[channel];
            channelSfxNote[channel] = DecodeNote(memory.Peek(noteAddr), memory.Peek(noteAddr+1));
        }

        private void StepNodeForChannel(int channel) {
            channelSfxOffset[channel] += 2;
            if (channelSfxOffset[channel] > 64) {
                channelSfxPointers[channel] = -1;
            }
            else {
                channelSfxTime[channel] -= channelSfxNoteLength[channel];
                PullNodeForChannel(channel);
            }
        }
  
        public void Music(int n, int fade_ms = 0, int channelmask = 0) {
            
        }
        
        public void Sfx(int n, int channel = -1, int offset = 0, int length = 0) {
            if (n < 0) return;
            if (channel < 0) {
                // TODO: find channel
                channel = 0;
            }
            channelSfxPointers[channel] = MemoryModule.ADDR_SOUND + 68 * n;
            channelSfxOffset[channel] = 0;
            channelSfxTime[channel] = 0;
            channelSfxNoteLength[channel] = memory.Peek(channelSfxPointers[channel] + 65) / 128d;
        }
        
        public void FillBuffer(float[] data, int channels) {
            int samples = data.Length;
            if (channelSfxPointers[0] < 0) return;

            for (var i = 0; i < samples; i = i + channels)
            {
                channelSfxTime[0] += sampleDelta;
                double time = channelSfxTime[0] - channelSfxTime[0] % 1 / 22050d;
                data[i] = (float)(gain * (channelSfxNote[0].volume / 7d) * AudioProcessingUnit.Oscillate(channelSfxNote[0].waveform, time, channelSfxNote[0].hz));
                if (channelSfxTime[0] > channelSfxNoteLength[0]) {
                    StepNodeForChannel(0);
                }
                if (channels == 2) data[i+1] = data[i];
            }
        }
    }
}