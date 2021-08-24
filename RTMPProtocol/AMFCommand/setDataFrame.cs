namespace RTMPLibrary.AMFCommand
{
    public partial class setDataFrame
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
            get { return (AMF0Objects.AMF0Object)iRTMPBodyAMFBase.AMF0List[2]; }
        }

        public setDataFrame()
        {
            iRTMPBodyAMFBase = new AMFCommandBody();
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "@setDataFrame" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "onMetaData" });
            iRTMPBodyAMFBase.AMF0List.Add(new AMF0Objects.AMF0EMCAArray());

            Information.AddToProperties("width", new AMF0Objects.AMF0Number());
            Information.AddToProperties("height", new AMF0Objects.AMF0Number());
            Information.AddToProperties("videodatarate", new AMF0Objects.AMF0Number());
            Information.AddToProperties("videocodecid", new AMF0Objects.AMF0Number());
            Information.AddToProperties("audiodatarate", new AMF0Objects.AMF0Number());
            Information.AddToProperties("audiosamplerate", new AMF0Objects.AMF0Number());
            Information.AddToProperties("audiosamplesize", new AMF0Objects.AMF0Number());
            Information.AddToProperties("stereo", new AMF0Objects.AMF0Number());
            Information.AddToProperties("audiocodecid", new AMF0Objects.AMF0Number());
            Information.AddToProperties("title", new AMF0Objects.AMF0String());
            Information.AddToProperties("encoder", new AMF0Objects.AMF0String());
        }

        public setDataFrame(AMFCommandBody Body)
        {
            iRTMPBodyAMFBase = Body;
        }
    }
}
