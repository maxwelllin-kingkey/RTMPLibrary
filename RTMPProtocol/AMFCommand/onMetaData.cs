namespace RTMPLibrary.AMFCommand
{
    public partial class onMetaData
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

        public AMF0Objects.AMF0Object Information
        {
            get { return (AMF0Objects.AMF0Object)iRTMPBodyAMFBase.AMF0List[1]; }
        }

        public onMetaData()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "onMetaData" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0Object());

            /*
            Information.AddToProperties("Server", new AMF0Objects.AMF0String());
            Information.AddToProperties("width", new AMF0Objects.AMF0Number());
            Information.AddToProperties("height", new AMF0Objects.AMF0Number());
            Information.AddToProperties("displayWidth", new AMF0Objects.AMF0Number());
            Information.AddToProperties("displayHeight", new AMF0Objects.AMF0Number());
            Information.AddToProperties("duration", new AMF0Objects.AMF0Number());
            Information.AddToProperties("framerate", new AMF0Objects.AMF0Number());
            Information.AddToProperties("fps", new AMF0Objects.AMF0Number());
            Information.AddToProperties("videodatarate", new AMF0Objects.AMF0Number());
            Information.AddToProperties("videocodecid", new AMF0Objects.AMF0Number());
            Information.AddToProperties("audiodatarate", new AMF0Objects.AMF0Number());
            Information.AddToProperties("audiocodecid", new AMF0Objects.AMF0Number());
            Information.AddToProperties("profile", new AMF0Objects.AMF0String());
            Information.AddToProperties("level", new AMF0Objects.AMF0String());
            */
        }

        public onMetaData(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}
