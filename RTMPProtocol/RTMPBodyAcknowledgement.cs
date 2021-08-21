using System;

namespace RTMPLibrary
{
    public partial class RTMPBodyAcknowledgement : RTMPBodyBase
    {
        public uint SequenceNumber;

        public RTMPBodyAcknowledgement()
        {
        }

        public RTMPBodyAcknowledgement(byte[] Value, int OffsetIndex)
        {
            SequenceNumber = Common.GetUInt(Value, OffsetIndex);
        }

        public override byte[] ToByteArray()
        {
            byte[] RetValue = null;

            RetValue = (byte[])System.Array.CreateInstance(typeof(byte), 4);
            for (int I = 0; I <= 3; I++)
                RetValue[I] = (byte)((long)Math.Round(SequenceNumber % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I)));

            return RetValue;
        }
    }
}
