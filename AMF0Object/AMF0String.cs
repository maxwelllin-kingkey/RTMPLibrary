namespace RTMPLibrary.AMF0Objects
{
    public partial class AMF0String : AMF0InheritsBase
    {
        private string iStringValue;

        public string Value
        {
            get
            {
                return iStringValue;
            }

            set
            {
                iStringValue = value;
            }
        }

        public AMF0String(byte[] Value, int OffsetIndex) : base(Value, OffsetIndex)
        {

            // 0 1 2
            // +---+----------------------
            // |Len| ......
            // +---+----------------------

            int iStringLength;

            iStringLength = base.iArrayValue[OffsetIndex + 1] * 256 + base.iArrayValue[OffsetIndex + 2];
            iStringValue = System.Text.Encoding.Default.GetString(base.iArrayValue, OffsetIndex + 3, iStringLength);
        }

        public AMF0String() : base(Common.enumAMF0ObjectType.String)
        {
        }

        public override byte[] ToByteArray()
        {
            System.Collections.Generic.List<byte> RetValue = new System.Collections.Generic.List<byte>();

            RetValue.Add((byte)base.ObjectType);
            if (string.IsNullOrEmpty(iStringValue) == false)
            {
                int iStringLength = System.Text.Encoding.Default.GetByteCount(iStringValue);

                RetValue.AddRange(new byte[] { (byte)(iStringLength / 256), (byte)(iStringLength % 256) });
                RetValue.AddRange(System.Text.Encoding.Default.GetBytes(iStringValue));
            }
            else
            {
                // empty string
                RetValue.AddRange(new byte[] { 0, 0 });
            }

            return RetValue.ToArray();
        }
    }
}
