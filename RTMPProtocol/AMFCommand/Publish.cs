namespace RTMPLibrary.AMFCommand
{
    public partial class Publish
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

        public AMF0Objects.AMF0String StreamName
        {
            get { return (AMF0Objects.AMF0String)this.iRTMPBodyAMFBase[3]; }
        }

        public AMF0Objects.AMF0String AppName
        {
            get { return (AMF0Objects.AMF0String)this.iRTMPBodyAMFBase[4]; }
        }

        public Publish(int TransactionID, string StreamName, string AppName)
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "publish" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = TransactionID });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = StreamName });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = AppName });
        }

        public Publish()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "publish" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String());
        }

        public Publish(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}