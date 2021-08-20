using System;

namespace RTMPLibrary
{
    public partial class RTMPBodyPeerBandwidth : RTMPBodyWindowSize
    {
        public byte LimitType;

        public RTMPBodyPeerBandwidth()
        {
        }

        public RTMPBodyPeerBandwidth(byte[] Value, int OffsetIndex) : base(Value, OffsetIndex)
        {
            LimitType = Value[OffsetIndex + 4];
        }

        public override byte[] ToByteArray()
        {
            byte[] RetValue = null;

            RetValue = (byte[])Array.CreateInstance(typeof(byte), 5);
            Array.Copy(base.ToByteArray(), 0, RetValue, 0, 4);
            RetValue[4] = LimitType;

            return RetValue;
        }
    }
}
