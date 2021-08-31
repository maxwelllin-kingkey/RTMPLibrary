using System;
using System.Collections.Generic;

namespace RTMPLibrary
{
    public partial class RTMPBodyAudioData : RTMPBodyBase
    {
        private byte[] iAudioData;
        private enumAudioCodec iCodec;
        private enumSampleRate iSampleRate;
        private enumBitDepth iBitDepth;
        private enumChannel iChannel;
        private enumAACPacketType iPacketType;

        public enum enumAudioCodec
        {
            PCM_PE = 0,
            ADPCM = 1,
            MP3 = 2,
            PCM_LE = 3,
            NELLY16 = 4,
            NELLY8 = 5,
            NELLY = 6,
            G711A = 7,
            G711U = 8,
            AAC = 10,
            SPEEX = 11,
            MP3_8K = 14,
            DeviceSpecific = 15
        }

        public enum enumSampleRate { 
            SR_5500 = 0,
            SR_11K = 1,
            SR_22K = 2,
            SR_44K = 3
        }

        public enum enumBitDepth
        {
            BD_8bit = 0,
            BD_16bit = 1
        }

        public enum enumChannel
        {
            Mono = 0,
            Stereo = 1
        }

        public enum enumAACPacketType
        {
            AACSequenceHead = 0,
            AACRaw = 1
        }


        public enumAudioCodec Codec
        {
            get { return iCodec; }
            set { iCodec = value; }
        }

        public enumSampleRate SampleRate
        {
            get { return iSampleRate; }
            set { iSampleRate = value; }
        }

        public enumBitDepth BitDepth
        {
            get { return iBitDepth; }
            set { iBitDepth = value; }
        }

        public enumChannel Channel
        {
            get { return iChannel; }
            set { iChannel = value; }
        }

        public byte[] AudioData
        {
            get { return iAudioData; }
            set { iAudioData = value; }
        }

        public RTMPBodyAudioData()
        {
            iCodec = enumAudioCodec.AAC;
            iSampleRate = enumSampleRate.SR_44K;
            iBitDepth = enumBitDepth.BD_16bit;
            iChannel = enumChannel.Stereo;

            // 封包有兩種, 需要先傳送 AAC Head (Type: 0) + AAC Config (2 bytes)
            // 再傳送 AAC Raw (Type: 1) + Raw (n bytes)
        }

        public RTMPBodyAudioData(byte[] Value, int OffsetIndex, int BodyLength)
        {
            if (BodyLength > 0)
            {
                iCodec = (enumAudioCodec)((Value[OffsetIndex] & 0xf0) >> 4);
                iSampleRate = (enumSampleRate)((Value[OffsetIndex] & 0x0c) >> 2);
                iBitDepth = (enumBitDepth)((Value[OffsetIndex] & 0x2) >> 1);
                iChannel = (enumChannel)((Value[OffsetIndex] & 0x1));

                iAudioData = (byte[])Array.CreateInstance(typeof(byte), BodyLength - 1);
                Array.Copy(Value, OffsetIndex + 1, iAudioData, 0, iAudioData.Length);
            }
        }

        public override byte[] ToByteArray()
        {
            byte[] ByteArray;

            if (iAudioData.Length > 0)
            {
                ByteArray = (byte[])Array.CreateInstance(typeof(byte), iAudioData.Length + 1);
                ByteArray[0] = (byte)(((byte)iCodec << 4) | ((byte)iSampleRate << 2) | ((byte)iBitDepth << 1) | (byte)iChannel);

                Array.Copy(iAudioData, 0, ByteArray, 1, iAudioData.Length);
            }
            else
            {
                ByteArray = (byte[])Array.CreateInstance(typeof(byte), 0);
            }

            return ByteArray;
        }
    }
}
