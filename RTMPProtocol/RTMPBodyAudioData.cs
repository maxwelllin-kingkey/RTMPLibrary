using System;
using System.Collections.Generic;

namespace RTMPLibrary
{
    public partial class RTMPBodyAudioData : RTMPBodyBase
    {
        private byte[] iAudioData;

        public enum enumAudioCodec { 
            NONE = 0x0001,
            ADPCM = 0x0002,
            MP3 = 0x0004,
            INTEL = 0x0008,
            NELLY8 = 0x0020,
            NELLY = 0x0040,
            G711A = 0x0080,
            G711U = 0x0100,
            NELLY16 = 0x0200,
            AAC = 0x0400,
            SPEEX = 0x0800,
            ALL = 0x0FFF
        }


        public byte[] AudioData
        {
            get
            {
                return iAudioData;
            }
            set
            {
                iAudioData = value;
            }
        }

        private RTMPBodyAudioData()
        {
        }

        public RTMPBodyAudioData(byte[] Value, int OffsetIndex, int BodyLength)
        {
            if (BodyLength > 0)
            {
                iAudioData = (byte[])Array.CreateInstance(typeof(byte), BodyLength);
                Array.Copy(Value, OffsetIndex, iAudioData, 0, iAudioData.Length);
            }
        }

        public override byte[] ToByteArray()
        {
            return iAudioData;
        }
    }
}
