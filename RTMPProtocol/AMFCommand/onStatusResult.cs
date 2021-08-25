namespace RTMPLibrary.AMFCommand
{
    public partial class onStatusResult
    {
        private AMFCommandBody iRTMPBodyAMFBase = null;

        public AMFCommandBody GetBody
        {
            get { return iRTMPBodyAMFBase; }
        }

        public AMF0Objects.AMF0String CommandName
        {
            get { return (AMF0Objects.AMF0String)iRTMPBodyAMFBase.AMF0List[0]; }
        }

        public AMF0Objects.AMF0Number TransactionID
        {
            get { return (AMF0Objects.AMF0Number)iRTMPBodyAMFBase.AMF0List[1]; }
        }

        public string code {
            get { return ((AMF0Objects.AMF0String)Information.GetValue("code")).Value; }
            set { Information.SetValue("code", value); }
        }

        public AMF0Objects.AMF0Object Information
        {
            get
            {
                return (AMF0Objects.AMF0Object)iRTMPBodyAMFBase.AMF0List[3];
            }
        }

        public onStatusResult(string statusCode)
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "onStatus" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());

            Information.AddToProperties("level", new AMF0Objects.AMF0String());
            Information.AddToProperties("code", new AMF0Objects.AMF0String() { Value = statusCode });
            Information.AddToProperties("description", new AMF0Objects.AMF0String());
        }

        public onStatusResult(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}