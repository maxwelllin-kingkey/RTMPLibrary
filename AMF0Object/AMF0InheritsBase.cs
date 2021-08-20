namespace RTMPLibrary.AMF0Objects
{
    public abstract partial class AMF0InheritsBase
    {
        public Common.enumAMF0ObjectType ObjectType;
        protected internal byte[] iArrayValue = null;
        protected internal int iObjectDataOffset;

        public abstract byte[] ToByteArray();

        protected internal AMF0InheritsBase(Common.enumAMF0ObjectType iType)
        {
            ObjectType = iType;
        }

        public AMF0InheritsBase(byte[] Value, int OffsetIndex)
        {
            ObjectType = (Common.enumAMF0ObjectType)Value[OffsetIndex + 0];
            iArrayValue = Value;
            iObjectDataOffset = OffsetIndex;
        }
    }
}
