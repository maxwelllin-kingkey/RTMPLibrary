namespace RTMPLibrary.UCM
{
    public abstract partial class UCMBase : RTMPBodyBase
    {
        public enum enumEventType
        {
            StreamBegin = 0,
            StreamEOF = 1,
            StreamDry = 2,
            SetBufferLength = 3,
            StreamIsRecorded = 4,
            PingRequest = 6,
            PingResponse = 7
        }

        public enumEventType EventType;
        protected internal byte[] iEventData = null;
        protected internal int iEventDataOffset;

        public static UCMBase ParseUCM(byte[] Value, int OffsetIndex)
        {
            enumEventType Type = (enumEventType)(Value[OffsetIndex] * 256 + Value[OffsetIndex + 1]);
            UCMBase RetValue = null;
            switch (Type)
            {
                case enumEventType.StreamBegin:
                    {
                        RetValue = new StreamBegin(Value, OffsetIndex);
                        break;
                    }

                case enumEventType.StreamEOF:
                    {
                        RetValue = new StreamEOF(Value, OffsetIndex);
                        break;
                    }

                case enumEventType.StreamDry:
                    {
                        RetValue = new StreamDry(Value, OffsetIndex);
                        break;
                    }

                case enumEventType.StreamIsRecorded:
                    {
                        RetValue = new StreamIsRecorded(Value, OffsetIndex);
                        break;
                    }

                case enumEventType.SetBufferLength:
                    {
                        RetValue = new SetBufferLength(Value, OffsetIndex);
                        break;
                    }

                case enumEventType.PingRequest:
                    {
                        RetValue = new PingRequest(Value, OffsetIndex);
                        break;
                    }

                case enumEventType.PingResponse:
                    {
                        RetValue = new PingResponse(Value, OffsetIndex);
                        break;
                    }
            }

            return RetValue;
        }

        public UCMBase(enumEventType Type)
        {
            EventType = Type;
        }

        public UCMBase(byte[] Value, int OffsetIndex)
        {
            EventType = (enumEventType)(Value[OffsetIndex] * 256 + Value[OffsetIndex + 1]);
            iEventData = Value;
            iEventDataOffset = OffsetIndex + 2;
        }
    }
}
