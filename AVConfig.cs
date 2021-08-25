namespace RTMPLibrary
{
    public class AVConfig
    {
        public enum enumAVType
        {
            H264SPS,
            H264PPS,
            AAC
        }

        public enumAVType AVType;
        public byte[] ConfigValue;
    }
}
