namespace RTMPLibrary.AMFCommand
{
    public partial class ConnectResult
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

        public AMF0Objects.AMF0Object Properties
        {
            get
            {
                return (AMF0Objects.AMF0Object)iRTMPBodyAMFBase.AMF0List[2];
            }
        }

        public AMF0Objects.AMF0Object Information
        {
            get
            {
                return (AMF0Objects.AMF0Object)iRTMPBodyAMFBase.AMF0List[3];
            }
        }

        public ConnectResult(enumResultType ResultType)
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

            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = 1 });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());
            Properties.AddToProperties("fmsVer", new AMF0Objects.AMF0String());
            Properties.AddToProperties("capabilities", new AMF0Objects.AMF0Number());
            Information.AddToProperties("level", new AMF0Objects.AMF0String());
            Information.AddToProperties("code", new AMF0Objects.AMF0String());
            Information.AddToProperties("description", new AMF0Objects.AMF0String());
            Information.AddToProperties("objectEncoding", new AMF0Objects.AMF0Number());
        }

        public ConnectResult(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}

