namespace RTMPLibrary.AMFCommand
{
    public partial class CreateStream
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
                return (AMF0Objects.AMF0String)this.iRTMPBodyAMFBase[0];
            }
        }

        public AMF0Objects.AMF0Number TransactionID
        {
            get
            {
                return (AMF0Objects.AMF0Number)this.iRTMPBodyAMFBase[1];
            }
        }

        // Public ReadOnly Property CommandObject As AMF0Object
        // Get
        // Return iRTMPBodyAMFBase(2)
        // End Get
        // End Property

        public CreateStream(int TransactionID)
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "createStream" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = TransactionID });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
        }

        public CreateStream()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "createStream" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
        }

        public CreateStream(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}