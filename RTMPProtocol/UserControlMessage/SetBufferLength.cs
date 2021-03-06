using System;

namespace RTMPLibrary.UCM {
    public partial class SetBufferLength : UCMBase
    {
        public uint StreamID;
        public uint BufferLength;

        public SetBufferLength() : base(enumEventType.SetBufferLength)
        {
        }

        public SetBufferLength(byte[] Value, int OffsetIndex) : base(Value, OffsetIndex)
        {
            StreamID = (uint)(base.iEventData[base.iEventDataOffset] * Math.Pow(256d, 3d) + base.iEventData[base.iEventDataOffset + 1] * Math.Pow(256d, 2d) + base.iEventData[base.iEventDataOffset + 2] * 256 + base.iEventData[base.iEventDataOffset + 3]);
            BufferLength = (uint)(base.iEventData[base.iEventDataOffset + 4] * Math.Pow(256d, 3d) + base.iEventData[base.iEventDataOffset + 5] * Math.Pow(256d, 2d) + base.iEventData[base.iEventDataOffset + 6] * 256 + base.iEventData[base.iEventDataOffset + 7]);
        }

        public override byte[] ToByteArray()
        {
            byte[] RetValue = null;

            RetValue = (byte[])Array.CreateInstance(typeof(byte), 10);
            RetValue[0] = (byte)((int)base.EventType / 256);
            RetValue[1] = (byte)((int)base.EventType % 256);

            for (int I = 0; I <= 3; I++)
                RetValue[I + 2] = (byte)((long)Math.Round(StreamID % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I)));

            for (int I = 0; I <= 3; I++)
                RetValue[I + 6] = (byte)((long)Math.Round(BufferLength % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I)));

            return RetValue;
        }
    }
}
