namespace RTMPLibrary.AMFCommand
{
    public partial class ReleaseStream
    {
        private AMFCommandBody iRTMPBodyAMFBase = null;

        public AMFCommandBody GetBody
        {
            get { return iRTMPBodyAMFBase; }
        }

        public AMF0Objects.AMF0String CommandName
        {
            get { return (AMF0Objects.AMF0String)this.iRTMPBodyAMFBase[0]; }
        }

        public AMF0Objects.AMF0Number TransactionID
        {
            get { return (AMF0Objects.AMF0Number)this.iRTMPBodyAMFBase[1]; }
        }

        public AMF0Objects.AMF0String ReleaseName
        {
            get { return (AMF0Objects.AMF0String)this.iRTMPBodyAMFBase[3]; }
        }

        public ReleaseStream(int TransactionID, string ReleaseName)
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "releaseStream" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = TransactionID });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = ReleaseName });
        }

        public ReleaseStream()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "releaseStream" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String());
        }

        public ReleaseStream(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}