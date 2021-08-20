namespace RTMPLibrary.AMF0Objects
{
    public partial class AMF0Null : AMF0InheritsBase
    {
        public AMF0Null(byte[] Value, int OffsetIndex) : base(Value, OffsetIndex)
        {
        }

        public AMF0Null() : base(Common.enumAMF0ObjectType.Null)
        {
        }

        public override byte[] ToByteArray()
        {
            return new byte[] { System.Convert.ToByte(base.ObjectType) };
        }
    }
}
