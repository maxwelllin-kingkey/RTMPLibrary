namespace RTMPLibrary.AMFCommand
{
    public partial class Connect
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

        public AMF0Objects.AMF0Object CommandObject
        {
            get
            {
                return (AMF0Objects.AMF0Object)this.iRTMPBodyAMFBase[2];
            }
        }

        public AMF0Objects.AMF0Object OptionalArguments
        {
            get
            {
                AMF0Objects.AMF0Object RetValue = (AMF0Objects.AMF0Object)this.iRTMPBodyAMFBase[3];
                if (RetValue == null)
                {
                    RetValue = new AMF0Objects.AMF0Object();
                    iRTMPBodyAMFBase.AMF0List.Add(RetValue);
                }

                return RetValue;
            }
        }

        public Connect()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "connect" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number() { Value = 1 });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());

            // prepare command object
            // CommandObject.AddToProperties("app", New AMF0String)
            // CommandObject.AddToProperties("flashVer", New AMF0String)
            // CommandObject.AddToProperties("tcUrl", New AMF0String)
            // CommandObject.AddToProperties("fpad", New AMF0Boolean)
            // CommandObject.AddToProperties("audioCodecs", New AMF0Number)
            // CommandObject.AddToProperties("videoCodecs", New AMF0Number)

        }

        public Connect(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}
