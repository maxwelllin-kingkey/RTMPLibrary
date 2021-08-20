namespace RTMPLibrary.AMFCommand
{
    public partial class Call
    {
        private AMFCommandBody iRTMPBodyAMFBase = null;

        public AMFCommandBody GetBody
        {
            get
            {
                return iRTMPBodyAMFBase;
            }
        }

        public AMF0Objects.AMF0String ProcedureName
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

        public Call()
        {
            iRTMPBodyAMFBase = new AMFCommand.AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Number());
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());
        }

        public Call(AMFCommand.AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}

