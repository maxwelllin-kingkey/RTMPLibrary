namespace RTMPLibrary.AMF0Objects
{
    public partial class AMF0Boolean : AMF0InheritsBase
    {
        private bool iValue;

        public bool Value
        {
            get
            {
                return iValue;
            }

            set
            {
                iValue = value;
            }
        }

        public AMF0Boolean(byte[] Value, int OffsetIndex) : base(Value, OffsetIndex)
        {
            if (base.iArrayValue[OffsetIndex + 1] == 0)
            {
                iValue = false;
            }
            else
            {
                iValue = true;
            }
        }

        public AMF0Boolean() : base(Common.enumAMF0ObjectType.Boolean)
        {
        }

        public override byte[] ToByteArray()
        {
            return new byte[] { System.Convert.ToByte(base.ObjectType), (byte)(iValue ? 1 : 0) };
        }
    }
}
