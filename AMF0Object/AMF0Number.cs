namespace RTMPLibrary.AMF0Objects
{
    public partial class AMF0Number : AMF0InheritsBase
    {
        private double iValue;

        public double Value
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

        public AMF0Number(byte[] Value, int OffsetIndex) : base(Value, OffsetIndex)
        {
            byte[] ValueArray = null;

            ValueArray = (byte[])System.Array.CreateInstance(typeof(byte), 8);
            System.Array.Copy(Value, OffsetIndex + 1, ValueArray, 0, 8);

            System.Array.Reverse<byte>(ValueArray);

            iValue = System.BitConverter.ToDouble(ValueArray, 0);
        }

        public AMF0Number() : base(Common.enumAMF0ObjectType.Number)
        {
        }

        public override byte[] ToByteArray()
        {
            byte[] ValueArray;
            byte[] RetValue;

            RetValue = (byte[])System.Array.CreateInstance(typeof(byte), 9);

            ValueArray = System.BitConverter.GetBytes(iValue);
            System.Array.Reverse<byte>(ValueArray);

            RetValue[0] = (byte)base.ObjectType;

            System.Array.Copy(ValueArray, 0, RetValue, 1, ValueArray.Length);

            return RetValue;
        }
    }
}
