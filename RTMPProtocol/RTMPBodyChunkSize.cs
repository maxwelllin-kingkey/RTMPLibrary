﻿using System;

namespace RTMPLibrary
{
    public partial class RTMPBodyChunkSize : RTMPBodyBase
    {
        public uint ChunkSize;

        public RTMPBodyChunkSize()
        {
        }

        public RTMPBodyChunkSize(byte[] Value, int OffsetIndex)
        {
            ChunkSize = (uint)Math.Round(Value[OffsetIndex] * Math.Pow(256d, 3d) + Value[OffsetIndex + 1] * Math.Pow(256d, 2d) + Value[OffsetIndex + 2] * 256 + Value[OffsetIndex + 3]);
        }

        public override byte[] ToByteArray()
        {
            byte[] RetValue = null;

            RetValue = (byte[])Array.CreateInstance(typeof(byte), 4);

            for (int I = 0; I <= 3; I++)
                RetValue[I] = (byte)((long)Math.Round(ChunkSize % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I)));

            return RetValue;
        }
    }
}
