using System;
using System.Collections.Generic;

namespace RTMPLibrary.AMF0Objects
{
    public partial class AMF0StrictArray : AMF0InheritsBase
    {
        public List<AMF0Objects.AMF0InheritsBase> ArrayList = new List<AMF0Objects.AMF0InheritsBase>();

        public void Add(AMF0InheritsBase AMF0)
        {
            ArrayList.Add(AMF0);
        }

        public AMF0InheritsBase Items(int index)
        {
            return ArrayList[index];
        }

        public int Count()
        {
            return ArrayList.Count;
        }

        public AMF0StrictArray() : base(Common.enumAMF0ObjectType.StrictArray)
        {
        }

        public AMF0StrictArray(byte[] Value, int OffsetIndex, ref int ParsingLength) : base(Value, OffsetIndex)
        {
            if (base.ObjectType == Common.enumAMF0ObjectType.StrictArray)
            {
                int TotalParsingLength = 0;
                int ArrayLength = 0;

                OffsetIndex += 1;
                ArrayLength = (int)Common.GetUInt(Value, OffsetIndex);

                OffsetIndex += 4;

                for(int i = 0; i < ArrayLength; i++)
                {
                    AMF0Objects.AMF0InheritsBase AMFBody = null;
                    int OPLength = 0;

                    AMFBody = Common.ParsingAMF0(Value, OffsetIndex, ref OPLength);
                    ArrayList.Add(AMFBody);

                    OffsetIndex += OPLength;
                    TotalParsingLength += OPLength;
                }

                ParsingLength = TotalParsingLength + 5;
            }
            else if (base.ObjectType == Common.enumAMF0ObjectType.Null)
            {
                // object is null
                ParsingLength = 1;
            }
        }

        public override byte[] ToByteArray()
        {
            System.Collections.Generic.List<byte> RetValue = new System.Collections.Generic.List<byte>();

            RetValue.Add((byte)Common.enumAMF0ObjectType.StrictArray);

            for (int I = 0; I <= 3; I++)
                RetValue.Add((byte)((long)Math.Round(ArrayList.Count % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I))));

            if (ArrayList.Count > 0)
            {
                foreach (AMF0InheritsBase EachAMF in ArrayList)
                    RetValue.AddRange(EachAMF.ToByteArray());
            }

            return RetValue.ToArray();
        }

    }
}
