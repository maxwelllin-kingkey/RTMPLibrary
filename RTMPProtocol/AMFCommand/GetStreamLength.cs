namespace RTMPLibrary.AMFCommand
{
    public partial class GetStreamLength
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
            get { return (AMF0Objects.AMF0String)this.iRTMPBodyAMFBase[2]; }
        }

        public GetStreamLength(int TransactionID, string StreamName)
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "getStreamLength" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = 1 });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = StreamName });
        }

        public GetStreamLength()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "getStreamLength" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = 1 });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String());
        }

        public GetStreamLength(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}
