namespace RTMPLibrary.AMFCommand
{
    public partial class Play
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

        public AMF0Objects.AMF0String StreamName
        {
            get
            {
                return (AMF0Objects.AMF0String)this.iRTMPBodyAMFBase[3];
            }
        }

        public AMF0Objects.AMF0Number Start
        {
            get
            {
                return (AMF0Objects.AMF0Number)this.iRTMPBodyAMFBase[4];
            }
        }

        public AMF0Objects.AMF0Number Duration
        {
            get
            {
                return (AMF0Objects.AMF0Number)this.iRTMPBodyAMFBase[5];
            }
        }

        public Play()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "play" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Null());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = -1000 });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = -1000 });
        }

        public Play(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}
