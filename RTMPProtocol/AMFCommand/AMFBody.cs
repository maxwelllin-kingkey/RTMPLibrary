using System.Collections.Generic;

namespace RTMPLibrary.AMFCommand
{
    public partial class AMFCommandBody : RTMPBodyBase
    {
        protected internal List<RTMPLibrary.AMF0Objects.AMF0InheritsBase> AMF0List = new List<RTMPLibrary.AMF0Objects.AMF0InheritsBase>();

        public object this[int Index]
        {
            get
            {
                if (AMF0List.Count - 1 >= Index)
                {
                    return AMF0List[Index];
                }

                return default;
            }
        }

        public AMFCommandBody()
        {
        }

        public AMFCommandBody(byte[] Value, int OffsetIndex)
        {
            while (true)
            {
                int ParsingLength = 0;
                RTMPLibrary.AMF0Objects.AMF0InheritsBase AMF0 = null;

                if (OffsetIndex < (Value.Length - 1))
                {
                    AMF0 = Common.ParsingAMF0(Value, OffsetIndex, ref ParsingLength);
                    AMF0List.Add(AMF0);
                    OffsetIndex += ParsingLength;
                }
                else
                    break;
            }
        }

        public override byte[] ToByteArray()
        {
            var RetValue = new List<byte>();

            foreach (RTMPLibrary.AMF0Objects.AMF0InheritsBase EachAMF0 in AMF0List)
                RetValue.AddRange(EachAMF0.ToByteArray());

            return RetValue.ToArray();
        }
    }
}
