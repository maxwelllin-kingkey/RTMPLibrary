namespace RTMPLibrary
{
    public partial class RTMPHead
    {
        private int iChunkId;
        private enumFmtType iFmtType;
        private int iChunkStreamID;
        private uint iTimeStamp;
        private int iBodySize;
        private enumTypeID iTypeID;
        private int iStreamID;

        public enum enumFmtType
        {
            Type0 = 0,
            Type1,
            Type2,
            Type3
        }

        public enum enumTypeID
        {
            // AMF0Command = 0
            SetChunkSize = 1,
            AboutMsg = 2,
            Ack = 3,
            UserControlMsg = 4,
            SetWindowSize = 5,
            SetPeerBandwidth = 6,
            AudioData = 8,
            VideoData = 9,
            AMF3Command = 0x11,
            AMF0Data = 0x12,
            AMF0Command = 0x14,
            Aggregate = 0x16
        }

        public int HeaderSize
        {
            get
            {
                int RetValue = 0;

                switch (iFmtType)
                {
                    case enumFmtType.Type0:
                        // Timestamp (3) + BodySize (3) + TypeId (1) +  StreamId (4)
                        RetValue = 11 + ChunkSize + (HasExtendedTimestamp ? 4 : 0);
                        break;
                    case enumFmtType.Type1:
                        // Timestamp (3) + BodySize (3) + TypeId (1)
                        RetValue = 7 + ChunkSize + (HasExtendedTimestamp ? 4 : 0);
                        break;
                    case enumFmtType.Type2:
                        // 只有 Timestamp (3 bytes)
                        RetValue = 3 + ChunkSize + (HasExtendedTimestamp ? 4 : 0);
                        break;
                    case enumFmtType.Type3:
                        RetValue = ChunkSize;
                        break;
                }

                return RetValue;
            }
        }

        public int ChunkSize
        {
            get
            {
                if ((iChunkId >= 2) && (iChunkId <= 63))
                    return 1;
                else if ((iChunkId >= 64) && (iChunkId <= 319))
                    return 2;
                else
                    return 3;
            }
        }

        public bool HasExtendedTimestamp
        {
            get
            {
                bool RetValue = false;

                if (iTimeStamp >= 0xFFFFFFL)
                    RetValue = true;

                return RetValue;
            }
        }

        public enumFmtType FmtType
        {
            get
            {
                return iFmtType;
            }

            set
            {
                iFmtType = value;
            }
        }

        public int ChunkStreamID
        {
            get
            {
                return iChunkId;
            }

            set
            {
                if (value >= 2)
                    iChunkId = value;
                else
                    throw new System.Exception("Chunk Id invalid");
            }
        }

        // Only FmtType: 0, 1, 2
        public uint Timestamp
        {
            get
            {
                return iTimeStamp;
            }

            set
            {
                iTimeStamp = value;
            }
        }

        // 不包含 Header 長度
        // Only FmtType: 0, 1
        public int BodySize
        {
            get
            {
                return iBodySize;
            }

            set
            {
                iBodySize = value;
            }
        }

        // Only FmtType: 0, 1
        public enumTypeID TypeID
        {
            get
            {
                return iTypeID;
            }

            set
            {
                iTypeID = value;
            }
        }

        // Only FmtType: 0
        public uint StreamID
        {
            get
            {
                return (uint)iStreamID;
            }

            set
            {
                iStreamID = (int)value;
            }
        }

        public byte[] ToByteArray()
        {
            byte[] RetValue = null;
            int HeadOffset;

            RetValue = (byte[])System.Array.CreateInstance(typeof(byte), HeaderSize);

            switch (iFmtType)
            {
                case enumFmtType.Type0:
                    RetValue[0] = 0;
                    break;
                case enumFmtType.Type1:
                    RetValue[0] = 0x40;
                    break;
                case enumFmtType.Type2:
                    RetValue[0] = 0x80;
                    break;
                case enumFmtType.Type3:
                    RetValue[0] = 0xC0;
                    break;
            }

            if ((iChunkId >= 2) && (iChunkId <= 63))
            {
                RetValue[0] = (byte)(RetValue[0] | iChunkId);
                HeadOffset = 1;
            }
            else if ((iChunkId >= 64) && (iChunkId <= 319))
            {
                RetValue[1] = (byte)(iChunkId - 64);
                HeadOffset = 2;
            }
            else
            {
                RetValue[0] = (byte)(RetValue[0] | 1);
                RetValue[1] = (byte)((iChunkId - 64) / 256);
                RetValue[2] = (byte)((iChunkId - 64) % 256);
                HeadOffset = 3;
            }

            if ((iFmtType == enumFmtType.Type0) ||
                (iFmtType == enumFmtType.Type1) ||
                (iFmtType == enumFmtType.Type2))
            {
                if (iTimeStamp >= 0xFFFFFFL)
                {
                    RetValue[HeadOffset + 0] = 0xFF;
                    RetValue[HeadOffset + 1] = 0xFF;
                    RetValue[HeadOffset + 2] = 0xFF;

                    //RetValue(RetValue.Length - (4 - I)) = ((iTimeStamp Mod (256 ^ (3 - I + 1))) \ (256 ^ (3 - I)))
                    for (int I = 0; I <= 3; I++)
                        RetValue[RetValue.Length - (4 - I)] = (byte)((long)System.Math.Round(iTimeStamp % System.Math.Pow(256d, 3 - I + 1)) / (long)System.Math.Round(System.Math.Pow(256d, 3 - I)));
                }
                else
                {
                    //RetValue(HeadOffset + I) = ((iTimeStamp Mod (256 ^ (2 - I + 1))) \ (256 ^ (2 - I)))
                    for (int I = 0; I <= 2; I++)
                        RetValue[HeadOffset + I] = (byte)((long)System.Math.Round(iTimeStamp % System.Math.Pow(256d, 2 - I + 1)) / (long)System.Math.Round(System.Math.Pow(256d, 2 - I)));
                }
            }

            if ((iFmtType == enumFmtType.Type0) ||
                (iFmtType == enumFmtType.Type1))
            {
                //RetValue(HeadOffset + 3 + I) = ((iBodySize Mod (256 ^ (2 - I + 1))) \ (256 ^ (2 - I)))
                for (int I = 0; I <= 2; I++)
                    RetValue[HeadOffset + 3 + I] = (byte)((long)System.Math.Round(iBodySize % System.Math.Pow(256d, 2 - I + 1)) / (long)System.Math.Round(System.Math.Pow(256d, 2 - I)));

                RetValue[HeadOffset + 6] = (byte)iTypeID;
            }

            if (iFmtType == enumFmtType.Type0)
            {
                System.Array.Copy(System.BitConverter.GetBytes((uint)iStreamID), 0, RetValue, HeadOffset + 7, 4);
                // For I As Integer = 0 To 4
                // RetValue(I + 7) = ((iStreamID Mod (256 ^ (3 - I + 1))) \ (256 ^ (3 - I)))
                // Next
            }

            return RetValue;
        }

        public RTMPHead()
        {
        }

        public RTMPHead(byte[] ArrayValue)
        {
            int Offset;

            iFmtType = (enumFmtType)((ArrayValue[0] & 0xC0) >> 6);
            switch (ArrayValue[0] & 0x3F)
            {
                case 0:
                    // Chunk type 2 (16bit)
                    iChunkId = ArrayValue[1] + 64;
                    Offset = 2;
                    break;
                case 1:
                    // Chunk type 3 (24bit)
                    iChunkId = ArrayValue[1] * 256 + ArrayValue[2] + 64;
                    Offset = 3;
                    break;
                default:
                    // Chunk type 1 (8bit)
                    iChunkId = ArrayValue[0] & 0x3F;
                    Offset = 1;
                    break;
            }

            if ((iFmtType == enumFmtType.Type0) || (iFmtType == enumFmtType.Type1) || (iFmtType == enumFmtType.Type2))
            {
                if (ArrayValue[Offset + 0] == 0xFF & ArrayValue[Offset + 1] == 0xFF & ArrayValue[Offset + 2] == 0xFF)
                {
                    int HS = 0;

                    switch (iFmtType)
                    {
                        case enumFmtType.Type0:
                            // Timestamp (3) + BodySize (3) + TypeId (1) +  StreamId (4)
                            HS = 11 + ChunkSize;
                            break;
                        case enumFmtType.Type1:
                            // Timestamp (3) + BodySize (3) + TypeId (1)
                            HS = 7 + ChunkSize;
                            break;
                        case enumFmtType.Type2:
                            // 只有 Timestamp (3 bytes)
                            HS = 3 + ChunkSize;
                            break;
                        case enumFmtType.Type3:
                            HS = ChunkSize;
                            break;
                    }

                    //iTimeStamp = (ArrayValue(HS + 0) * (256 ^ 3)) + (ArrayValue(HS + 1) * (256 ^ 2)) + ArrayValue(HS + 2) * 256 + ArrayValue(HS + 3)
                    iTimeStamp = (uint)System.Math.Round(ArrayValue[HS + 0] * System.Math.Pow(256d, 3d) + ArrayValue[HS + 1] * System.Math.Pow(256d, 2d) + ArrayValue[HS + 2] * 256 + ArrayValue[HS + 3]);
                }
                else
                {
                    //iTimeStamp = (ArrayValue(Offset + 0) * (256 ^ 2)) + ArrayValue(Offset + 1) * 256 + ArrayValue(Offset + 2)
                    iTimeStamp = (uint)System.Math.Round(ArrayValue[Offset + 0] * System.Math.Pow(256d, 2d) + ArrayValue[Offset + 1] * 256 + ArrayValue[Offset + 2]);
                }
            }

            if ((iFmtType == enumFmtType.Type0) || (iFmtType == enumFmtType.Type1))
            {
                //iBodySize = (ArrayValue(Offset + 3) * (256 ^ 2)) + ArrayValue(Offset + 4) * 256 + ArrayValue(Offset + 5)
                iBodySize = (int)System.Math.Round(ArrayValue[Offset + 3] * System.Math.Pow(256d, 2d) + ArrayValue[Offset + 4] * 256 + ArrayValue[Offset + 5]);
                iTypeID = (enumTypeID)ArrayValue[Offset + 6];
            }

            if (iFmtType == enumFmtType.Type0)
            {
                iStreamID = System.BitConverter.ToInt32(ArrayValue, Offset + 7);
                // iStreamID = (ArrayValue(Offset + 7) * (256 ^ 3)) + (ArrayValue(Offset + 8) * (256 ^ 2)) + ArrayValue(Offset + 9) * 256 + ArrayValue(Offset + 10)
            }
        }
    }
}
