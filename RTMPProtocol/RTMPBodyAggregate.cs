using System;

namespace RTMPLibrary
{
    public partial class RTMPBodyAggregate : RTMPBodyBase
    {
        private System.Collections.Generic.List<AggregateMessage> iMsgList = new System.Collections.Generic.List<AggregateMessage>();

        public System.Collections.Generic.List<AggregateMessage> GetList()
        {
            return iMsgList;
        }

        private RTMPBodyAggregate()
        {
        }

        public RTMPBodyAggregate(byte[] Value, int OffsetIndex, int BodyLength)
        {
            int Index = 0;

            for (int _Loop = 1; _Loop <= 1000; _Loop++)
            {
                int MsgSize = -1;
                int Size;
                int ValidSize;
                int TypeID;
                AggregateMessage Msg = null;

                if (Index >= BodyLength)
                    break;

                TypeID = Value[OffsetIndex + Index + 0];
                Size = (int)Math.Round(Value[OffsetIndex + Index + 1] * Math.Pow(256d, 2d) + Value[OffsetIndex + Index + 2] * 256 + Value[OffsetIndex + Index + 3]);

                ValidSize = (int)Common.GetUInt(Value, OffsetIndex + Index + Size + 11); // (int)Math.Round(Value[OffsetIndex + Index + Size + 11] * Math.Pow(256d, 3d) + Value[OffsetIndex + Index + Size + 12] * Math.Pow(256d, 2d) + Value[OffsetIndex + Index + Size + 13] * 256 + Value[OffsetIndex + Index + Size + 14]);
                if (ValidSize == Size)
                {
                    Msg = new AggregateMessage();

                    Msg.TypeID = (RTMPHead.enumTypeID)TypeID;
                    Msg.Timestamp = (int)System.Math.Round(Value[OffsetIndex + Index + 4] * System.Math.Pow(256d, 2d) + Value[OffsetIndex + Index + 5] * 256 + Value[OffsetIndex + Index + 6]);
                    Msg.TimeExtended = Value[OffsetIndex + Index + 7];
                    Msg.StreamID = (int)System.Math.Round(Value[OffsetIndex + Index + 8] * System.Math.Pow(256d, 2d) + Value[OffsetIndex + Index + 9] * 256 + Value[OffsetIndex + Index + 10]);
                    Msg.Body = Value;
                    Msg.BodyOffset = OffsetIndex + Index + 11;
                    Msg.BodyLength = Size;

                    iMsgList.Add(Msg);

                    Index += Size + 15;
                }
                else
                {
                    break;
                }
            }
        }

        public override byte[] ToByteArray()
        {
            var RetValue = new System.Collections.Generic.List<byte>();

            foreach (AggregateMessage EachMsg in iMsgList)
                RetValue.AddRange(EachMsg.ToByteArray());

            return RetValue.ToArray();
        }

        public partial class AggregateMessage
        {
            // Type: 1 byte (9=video)
            // data size: 3 byte
            // Timestamp: 3 byte
            // Time extended: 1 byte
            // StreamID: 3 byte
            // data: (data size) byte
            // back packet size: 4 byte (same with data size)

            public RTMPHead.enumTypeID TypeID;
            public byte[] Body;
            public int BodyOffset;
            public int BodyLength;
            public int Timestamp;
            public int TimeExtended;
            public int StreamID;

            public byte[] ToByteArray()
            {
                byte[] RetValue;
                RetValue = (byte[])System.Array.CreateInstance(typeof(byte), BodyLength + 15);
                RetValue[0] = (byte)TypeID;

                for (int I = 0; I <= 3; I++)
                    RetValue[I + 1] = (byte)((long)Math.Round(BodyLength % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I)));

                for (int I = 0; I <= 4; I++)
                    RetValue[I + BodyLength + 11] = (byte)((long)Math.Round(BodyLength % Math.Pow(256d, 4 - I + 1)) / (long)Math.Round(Math.Pow(256d, 4 - I)));

                for (int I = 0; I <= 3; I++)
                    RetValue[I + 4] = (byte)((long)Math.Round(Timestamp % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I)));

                RetValue[7] = (byte)TimeExtended;

                for (int I = 0; I <= 3; I++)
                    RetValue[I + 8] = (byte)((long)Math.Round(StreamID % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I)));

                System.Array.Copy(Body, BodyOffset, RetValue, 11, BodyLength);

                return RetValue;
            }
        }
    }
}
