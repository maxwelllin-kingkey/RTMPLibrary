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
            SequenceNumber = (uint)System.Math.Round(Value[OffsetIndex] * System.Math.Pow(256d, 3d) + Value[OffsetIndex + 1] * System.Math.Pow(256d, 2d) + Value[OffsetIndex + 2] * 256 + Value[OffsetIndex + 3]);
        }

        public override byte[] ToByteArray()
        {
            byte[] RetValue = null;

            RetValue = (byte[])System.Array.CreateInstance(typeof(byte), 4);
            for (int I = 0; I <= 3; I++)
                RetValue[I] = (byte)((long)System.Math.Round(SequenceNumber % System.Math.Pow(256d, 3 - I + 1)) / (long)System.Math.Round(System.Math.Pow(256d, 3 - I)));

            return RetValue;
        }
    }
}
