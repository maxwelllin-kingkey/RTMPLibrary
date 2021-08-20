namespace RTMPLibrary.AMFCommand
{
    public partial class CreateStreamResult
    {
        private AMFCommandBody iRTMPBodyAMFBase = null;

        public enum enumResultType
        {
            Result = 0,
            Error = 1
        }

        public AMFCommandBody GetBody
        {
            get
            {
                return iRTMPBodyAMFBase;
            }
        }

        public AMF0Objects.AMF0String CommandName
        {
            get
            {
                return (AMF0Objects.AMF0String)iRTMPBodyAMFBase.AMF0List[0];
            }
        }

        public AMF0Objects.AMF0Number TransactionID
        {
            get
            {
                return (AMF0Objects.AMF0Number)iRTMPBodyAMFBase.AMF0List[1];
            }
        }

        public AMF0Objects.AMF0Null CommandObject
        {
            get
            {
                return (AMF0Objects.AMF0Null)iRTMPBodyAMFBase.AMF0List[2];
            }
        }

        public AMF0Objects.AMF0Number StreamID
        {
            get
            {
                return (AMF0Objects.AMF0Number)iRTMPBodyAMFBase.AMF0List[3];
            }
        }

        public CreateStreamResult(enumResultType ResultType)
        {
            iRTMPBodyAMFBase = new AMFCommandBody();

            switch (ResultType)
            {
                case enumResultType.Result:
                    iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "_result" });
                    break;
                case enumResultType.Error:
                    iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "_error" });
                    break;
            }

            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
        }

        public CreateStreamResult(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}
