namespace RTMPLibrary.AMFData
{
    public partial class onStatusResult
    {
        private AMFCommand.AMFCommandBody iRTMPBodyAMFBase = null;

        public AMFCommand.AMFCommandBody GetBody
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

        public AMF0Objects.AMF0Object Information
        {
            get
            {
                return (AMF0Objects.AMF0Object)iRTMPBodyAMFBase.AMF0List[1];
            }
        }

        public onStatusResult(string statusCode)
        {
            iRTMPBodyAMFBase = new AMFCommand.AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "onStatus" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());
            Information.AddToProperties("code", new AMF0Objects.AMF0String() { Value = statusCode });
        }

        public onStatusResult(AMFCommand.AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}