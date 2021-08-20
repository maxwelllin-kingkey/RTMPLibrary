namespace RTMPLibrary.AMFCommand
{
    public partial class CallResult
    {
        private AMFCommandBody iRTMPBodyAMFBase = null;

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

        public AMF0Objects.AMF0Object CommandObject
        {
            get
            {
                return (AMF0Objects.AMF0Object)iRTMPBodyAMFBase.AMF0List[2];
            }
        }

        public AMF0Objects.AMF0Object Response
        {
            get
            {
                return (AMF0Objects.AMF0Object)iRTMPBodyAMFBase.AMF0List[3];
            }
        }

        public CallResult()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());
        }

        public CallResult(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}
