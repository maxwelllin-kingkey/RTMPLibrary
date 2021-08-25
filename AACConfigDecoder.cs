using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTMPLibrary
{
    internal partial class AACConfigDecoder
    {
        public int audioObjectType;
        public long sampleFrequency;
        public int channelConfig;

        private int GetSampleFrequencyIndex(long Freq)
        {
            int RetValue = 15;

            switch (Freq)
            {
                case 96000:
                        RetValue = 0;
                        break;
                case 88200:
                        RetValue = 1;
                        break;
                case 64000:
                        RetValue = 2;
                        break;
                case 48000:
                        RetValue = 3;
                        break;
                case 44100:
                        RetValue = 4;
                        break;
                case 32000:
                        RetValue = 5;
                        break;
                case 24000:
                        RetValue = 6;
                        break;
                case 22050:
                        RetValue = 7;
                        break;
                case 16000:
                        RetValue = 8;
                        break;
                case 12000:
                        RetValue = 9;
                        break;
                case 11025:
                        RetValue = 10;
                        break;
                case 8000:
                        RetValue = 11;
                        break;
                case 7350:
                        RetValue = 12;
                        break;
            }

            return RetValue;
        }

        private long GetSampleFrequencyByIndex(int index)
        {
            long RetValue = 0;

            switch (index)
            {
                case 0:
                        RetValue = 96000;
                        break;
                case 1:
                        RetValue = 88200;
                        break;
                case 2:
                        RetValue = 64000;
                        break;
                case 3:
                        RetValue = 48000;
                        break;
                case 4:
                        RetValue = 44100;
                        break;
                case 5:
                        RetValue = 32000;
                        break;
                case 6:
                        RetValue = 24000;
                        break;
                case 7:
                        RetValue = 22050;
                        break;
                case 8:
                        RetValue = 16000;
                        break;
                case 9:
                        RetValue = 12000;
                        break;
                case 10:
                        RetValue = 11025;
                        break;
                case 11:
                        RetValue = 8000;
                        break;
                case 12:
                        RetValue = 7350;
                        break;
            }

            return RetValue;
        }


        public void AACConfigDecode(byte[] config)
        {
            // 1188
            // 從字串轉 hex
            long configValue = (config[0] * 256) + config[1];
            int sampleFrequencyIndex;

            configValue = configValue & 0xFFF8L;  // 只取 13bits
            audioObjectType = (int)((configValue & 0xF800) >> 11);
            sampleFrequencyIndex = (int)((configValue & 0x780) >> 7);
            channelConfig = (int)((configValue & 0x78) >> 3);

            sampleFrequency = GetSampleFrequencyByIndex(sampleFrequencyIndex);
        }

        public void AACConfigDecode(string config)
        {
            // 1188
            // 從字串轉 hex
            string configTmp = config.Substring(0, 4);
            long configValue = uint.Parse(config, System.Globalization.NumberStyles.HexNumber);
            int sampleFrequencyIndex;

            configValue = configValue & 0xFFF8L;  // 只取 13bits
            audioObjectType = (int)((configValue & 0xF800) >> 11);
            sampleFrequencyIndex = (int)((configValue & 0x780) >> 7);
            channelConfig = (int)((configValue & 0x78) >> 3);

            sampleFrequency = GetSampleFrequencyByIndex(sampleFrequencyIndex);
        }
    }
}
