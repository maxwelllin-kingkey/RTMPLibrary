using System;
using System.Collections.Generic;

namespace RTMPLibrary
{
    public partial class RTMPBodyVideoData : RTMPBodyBase
    {
        private enumFrameType iFrameType;
        private enumVideoCodec iCodecFormat;
        private enumAVPacketType iAVPacketType;
        private int iCompositionTime;
        private List<byte[]> iVideoSliceList = new List<byte[]>();
        // Private iVideoData() As Byte

        public enum enumAVPacketType
        {
            AVC_Header = 0,
            AVC_NALU,
            AVC_EndOfSequence
        }

        public enum enumFrameType
        {
            Unknow = 0,
            KeyFrame = 1,
            NonKeyFrame = 2
        }

        public enum enumVideoCodec
        {
            SorensonH263 = 2,
            ScreenVideo = 3,
            On2VP6 = 4,
            On2VP6WithAlphaChannel = 5,
            ScreenVideo2 = 6,
            H264 = 7
        }

        public enumFrameType FrameType
        {
            get { return iFrameType; }
            set { iFrameType = value; }
        }

        public int CompositionTime
        {
            get { return iCompositionTime; }
            set { iCompositionTime = value; }
        }

        public enumVideoCodec CodecFormat
        {
            get { return iCodecFormat; }
            set { iCodecFormat = value; }
        }

        public enumAVPacketType AVPacketType
        {
            get { return iAVPacketType; }
            set { iAVPacketType = value; }
        }

        // Public Property VideoData As Byte()
        // Get
        // Return iVideoData
        // End Get
        // Set(value As Byte())
        // iVideoData = value
        // End Set
        // End Property

        public List<byte[]> GetSliceList()
        {
            return iVideoSliceList;
        }

        public static RTMPBodyVideoData ImportVideoRaw(enumFrameType FrameType, byte[] VideoRaw, int VideoOffset, int VideoLength)
        {
            var RetValue = new RTMPBodyVideoData();
            byte[] VideoData = null;

            RetValue.iAVPacketType = enumAVPacketType.AVC_NALU;
            RetValue.iFrameType = FrameType;
            RetValue.iCodecFormat = enumVideoCodec.H264;
            if (VideoRaw.Length != VideoLength)
            {
                VideoData = (byte[])Array.CreateInstance(typeof(byte), VideoLength);
                Array.Copy(VideoRaw, VideoOffset, VideoData, 0, VideoData.Length);
            }
            else
            {
                VideoData = VideoRaw;
            }

            RetValue.GetSliceList().Add(VideoData);

            return RetValue;
        }

        public static RTMPBodyVideoData ImportVideoRaw(enumFrameType FrameType, List<byte[]> VideoRawList)
        {
            var RetValue = new RTMPBodyVideoData();

            RetValue.iAVPacketType = enumAVPacketType.AVC_NALU;
            RetValue.iFrameType = FrameType;
            RetValue.iCodecFormat = enumVideoCodec.H264;

            RetValue.GetSliceList().AddRange(VideoRawList.ToArray());

            return RetValue;
        }

        public static RTMPBodyVideoData ImportSPS(byte[] SPS, byte[] PPS)
        {
            AVCDecoderConfigurationRecord AVConfig = null;
            RTMPBodyVideoData RetValue = null;

            AVConfig = AVCDecoderConfigurationRecord.ImportFromSPS(SPS, PPS);

            if (AVConfig != null)
            {
                RetValue = new RTMPBodyVideoData();
                RetValue.iAVPacketType = enumAVPacketType.AVC_Header;
                RetValue.iFrameType = enumFrameType.KeyFrame;
                RetValue.iCodecFormat = enumVideoCodec.H264;
                RetValue.GetSliceList().Add(AVConfig.ToByteArray());
            }

            return RetValue;
        }

        private RTMPBodyVideoData()
        {
        }

        public RTMPBodyVideoData(byte[] Value, int OffsetIndex, int BodyLength)
        {
            iFrameType = (enumFrameType)((Value[OffsetIndex + 0] & 0xF0) >> 4);
            iCodecFormat = (enumVideoCodec)(Value[OffsetIndex + 0] & 0xF);

            if (iCodecFormat == enumVideoCodec.H264)
            {
                iAVPacketType = (enumAVPacketType)Value[OffsetIndex + 1];

                // If iAVPacketType = enumAVPacketType.AVC_NALU Then
                iCompositionTime = (int)Math.Round(Value[OffsetIndex + 2] * Math.Pow(256d, 2d) + Value[OffsetIndex + 3] * 256 + Value[OffsetIndex + 4]);
                // End If

                if (iAVPacketType == enumAVPacketType.AVC_Header)
                {
                    byte[] VideoData = null;

                    VideoData = (byte[])Array.CreateInstance(typeof(byte), BodyLength - 5);
                    Array.Copy(Value, OffsetIndex + 5, VideoData, 0, VideoData.Length);
                    iVideoSliceList.Add(VideoData);
                }
                else
                {
                    int SliceOffset = 0;

                    for (int _Loop = 1; _Loop <= 1000; _Loop++)
                    {
                        uint NALU_Size;
                        byte[] VideoData = null;

                        if (BodyLength <= SliceOffset + 5)
                            break;

                        if (SliceOffset + 5 < BodyLength)
                        {
                            NALU_Size = (uint)Math.Round(Value[OffsetIndex + SliceOffset + 5] * Math.Pow(256, 3) + Value[OffsetIndex + SliceOffset + 6] * Math.Pow(256, 2) + Value[OffsetIndex + SliceOffset + 7] * 256 + Value[OffsetIndex + SliceOffset + 8]);
                            if (NALU_Size < 1000000L)
                            {
                                // If NALU_Size = 2 Then Stop

                                VideoData = (byte[])Array.CreateInstance(typeof(byte), (long)NALU_Size);
                                Array.Copy(Value, OffsetIndex + SliceOffset + 9, VideoData, 0, VideoData.Length);

                                iVideoSliceList.Add(VideoData);

                                SliceOffset += VideoData.Length + 4;
                            }
                            else
                            {
                                // NALU_Packet Invalid
                                break;
                            }
                        }
                        else if (SliceOffset + 5 == BodyLength)
                        {
                            break;
                        }
                        else
                        {
                            // Packet langth invalid
                            break;
                        }
                    }
                }
            }
            else
            {
                byte[] VideoData = null;

                VideoData = (byte[])Array.CreateInstance(typeof(byte), BodyLength - 1);
                Array.Copy(Value, OffsetIndex + 1, VideoData, 0, VideoData.Length);

                iVideoSliceList.Add(VideoData);
            }
        }

        public override byte[] ToByteArray()
        {
            var RetValue = new List<byte>();

            RetValue.AddRange(new byte[] { (byte)((byte)((byte)iFrameType << 4) | (byte)iCodecFormat) });
            if (iCodecFormat == enumVideoCodec.H264)
            {
                RetValue.Add((byte)iAVPacketType);

                for (int I = 0; I <= 2; I++)
                    RetValue.Add((byte)((long)Math.Round(iCompositionTime % Math.Pow(256, 2 - I + 1)) / (long)Math.Round(Math.Pow(256d, 2 - I))));

            }

            foreach (byte[] EachSlice in iVideoSliceList)
            {
                if (iAVPacketType != enumAVPacketType.AVC_Header)
                {
                    for (int I = 0; I <= 3; I++)
                        RetValue.AddRange(new byte[] { (byte)((long)Math.Round(EachSlice.Length % Math.Pow(256, 3 - I + 1)) / (long)Math.Round(Math.Pow(256, 3 - I))) });
                }

                RetValue.AddRange(EachSlice);
            }

            return RetValue.ToArray();
        }

        public partial class AVCDecoderConfigurationRecord
        {
            private byte iConfigurationVersion;
            private byte iAVCProfileIndication;
            private byte iProfile_compatibility;
            private byte iAVCLevelIndication;
            private byte iLengthSizeMinusOne;
            private byte[] iPPSContent = null;
            private byte[] iSPSContent = null;

            public byte[] PPSContent
            {
                get { return iPPSContent; }
                set { iPPSContent = value; }
            }

            public byte[] SPSContent
            {
                get { return iSPSContent; }
                set { iSPSContent = value; }
            }

            public static AVCDecoderConfigurationRecord DecodeFromRTMPVideoData(byte[] Value, int Offset, int Length)
            {
                AVCDecoderConfigurationRecord RetValue = null;

                if (Value[0] == 0x01) {
                    int SPSCount;
                    int PPSCount;
                    int pos;
                    byte[] SPSArray = null;
                    byte[] PPSArray = null;

                    SPSCount = (Value[5] & 0x1f);

                    pos = 6;
                    for (int i = 0; i < SPSCount; i++) {
                        int SPSSize;

                        SPSSize = Value[pos + 0] * 256 + Value[pos + 1];

                        if (SPSArray == null)
                        {
                            SPSArray = (byte[])Array.CreateInstance(typeof(byte), SPSSize);
                            Array.Copy(Value, pos + 2, SPSArray, 0, SPSArray.Length);
                        }

                        pos += (SPSSize + 2);
                    }

                    PPSCount = Value[pos++];
                    for (int i = 0; i < PPSCount; i++)
                    {
                        int PPSSize;

                        PPSSize = Value[pos + 0] * 256 + Value[pos + 1];

                        if (PPSArray == null)
                        {
                            PPSArray = (byte[])Array.CreateInstance(typeof(byte), PPSSize);
                            Array.Copy(Value, pos + 2, PPSArray, 0, PPSArray.Length);
                        }

                        pos += (PPSSize + 2);
                    }

                    RetValue = AVCDecoderConfigurationRecord.ImportFromSPS(SPSArray, PPSArray);
                }

                return RetValue;
            }

            public static AVCDecoderConfigurationRecord ImportFromSPS(byte[] SPSArray, byte[] PPSArray)
            {
                var Parsing = new SPSParsing();
                SPSParsing.SeqParameterSet SPS = default;
                AVCDecoderConfigurationRecord RetValue = null;

                // Console.WriteLine("Import from sps:" & SPSArray(0) & ":" & SPSArray(1) & ":" & SPSArray(2) & ":" & SPSArray(3))
                try
                {
                    SPS = Parsing.seq_parameter_set_rbsp(SPSArray);
                }
                catch (Exception ex)
                {
                    Console.Write(ex.ToString());
                }

                if (SPS != null)
                {
                    RetValue = new AVCDecoderConfigurationRecord();
                    RetValue.iConfigurationVersion = 1;
                    RetValue.iAVCProfileIndication = SPS.profile_idc;
                    RetValue.iProfile_compatibility = 0;
                    RetValue.iAVCLevelIndication = SPS.level_idc;
                    RetValue.iLengthSizeMinusOne = 4;
                    RetValue.iSPSContent = SPSArray;
                    RetValue.iPPSContent = PPSArray;
                }
                else
                {
                    Console.WriteLine("AVDecoder: SPS Is Nothing");
                }

                return RetValue;
            }

            public byte[] ToByteArray()
            {
                byte[] RetValue = null;

                RetValue = (byte[])Array.CreateInstance(typeof(byte), iSPSContent.Length + iPPSContent.Length + 11);
                RetValue[0] = iConfigurationVersion;
                RetValue[1] = iAVCProfileIndication;
                RetValue[2] = iProfile_compatibility;
                RetValue[3] = iAVCLevelIndication;
                RetValue[4] = (byte)((byte)(iLengthSizeMinusOne - 1) | 0xfc);
                RetValue[5] = (byte)((byte)1 | 0xe0);
                RetValue[6] = (byte)(iSPSContent.Length / 256);
                RetValue[7] = (byte)(iSPSContent.Length % 256);

                Array.Copy(iSPSContent, 0, RetValue, 8, iSPSContent.Length);

                RetValue[8 + iSPSContent.Length + 0] = 1;
                RetValue[8 + iSPSContent.Length + 1] = (byte)(iPPSContent.Length / 256);
                RetValue[8 + iSPSContent.Length + 2] = (byte)(iPPSContent.Length % 256);

                Array.Copy(iPPSContent, 0, RetValue, 8 + iSPSContent.Length + 3, iPPSContent.Length);

                return RetValue;
            }

            public AVCDecoderConfigurationRecord(byte[] Value, int OffsetIndex)
            {
                int iSequenceParameterSetLength;
                int iPictureParameterSetLength;
                byte iNumOfSequenceParameterSets;
                byte iNumOfPictureParameterSets;

                iConfigurationVersion = Value[OffsetIndex + 0];
                iAVCProfileIndication = Value[OffsetIndex + 1];
                iProfile_compatibility = Value[OffsetIndex + 2];
                iAVCLevelIndication = Value[OffsetIndex + 3];
                iLengthSizeMinusOne = (byte)((Value[OffsetIndex + 4] & 3) + 1);
                iNumOfSequenceParameterSets = (byte)(Value[OffsetIndex + 5] & 0x1F);
                iSequenceParameterSetLength = Value[OffsetIndex + 6] * 256 + Value[OffsetIndex + 7];
                iSPSContent = (byte[])Array.CreateInstance(typeof(byte), iSequenceParameterSetLength);
                Array.Copy(Value, OffsetIndex + 8, iSPSContent, 0, iSPSContent.Length);
                iNumOfPictureParameterSets = Value[OffsetIndex + 8 + iSPSContent.Length + 0];
                iPictureParameterSetLength = Value[OffsetIndex + 8 + iSPSContent.Length + 1] * 256 + Value[OffsetIndex + 8 + iSPSContent.Length + 2];
                iPPSContent = (byte[])Array.CreateInstance(typeof(byte), iPictureParameterSetLength);

                Array.Copy(Value, OffsetIndex + 8 + iSPSContent.Length + 3, iPPSContent, 0, iPPSContent.Length);
            }

            protected AVCDecoderConfigurationRecord()
            {
            }
        }
    }
}
