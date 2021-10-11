using System;
using System.Collections;
using System.Collections.Generic;

namespace RTMPLibrary
{
    public partial class RTMP
    {
        public RTMPHead Head = null;
        public RTMPBodyBase Body = null;
        public bool IsFmtType2 = false;
        public bool ExtendedTimestamp = false;
        private Dictionary<int, RTMPChunkStream> iChunkStreamList = new Dictionary<int, RTMPChunkStream>();

        public int IsDataAvailable(byte[] ArrayValue, int ArraySize, int ChunkSize, ref int NeedPacketSize)
        {
            int RetValue = -1;

            if (ChunkSize == 0)
                ChunkSize = 128;

            if (ArraySize >= 1)
            {
                RTMPHead.enumFmtType iFmtType;
                int NeedDataLength1;

                iFmtType = (RTMPHead.enumFmtType)((byte)(ArrayValue[0] & 0xC0) >> 6);
                switch (ArrayValue[0] & 0x3F)
                {
                    case 0:
                        // Chunk type 2 (16bit)
                        NeedDataLength1 = 2;
                        break;
                    case 1:
                        // Chunk type 3 (24bit)
                        NeedDataLength1 = 3;
                        break;
                    default:
                        // Chunk type 1 (8bit)
                        NeedDataLength1 = 1;
                        break;
                }

                if (ArraySize >= NeedDataLength1)
                {
                    bool NeedCheckExtendedTimestamp = false;
                    int NeedDataLength2 = 0;

                    switch (iFmtType)
                    {
                        case RTMPHead.enumFmtType.Type0:
                            NeedDataLength2 = NeedDataLength1 + 11;
                            NeedCheckExtendedTimestamp = true;
                            break;
                        case RTMPHead.enumFmtType.Type1:
                            NeedDataLength2 = NeedDataLength1 + 7;
                            NeedCheckExtendedTimestamp = true;
                            break;
                        case RTMPHead.enumFmtType.Type2:
                            NeedDataLength2 = NeedDataLength1 + 3;
                            NeedCheckExtendedTimestamp = true;
                            break;
                        case RTMPHead.enumFmtType.Type3:
                            NeedDataLength2 = NeedDataLength1;
                            break;
                    }

                    if (NeedCheckExtendedTimestamp)
                    {
                        if ((ArrayValue[NeedDataLength1] == 0xFF) &&
                            (ArrayValue[NeedDataLength1 + 1] == 0xFF) &&
                            (ArrayValue[NeedDataLength1 + 2] == 0xFF))
                        {
                            ExtendedTimestamp = true;
                            NeedDataLength2 += 4;
                        }
                    }

                    if (ArraySize >= NeedDataLength2)
                    {
                        RTMPHead Head = null;
                        int ChunkCount;
                        int BodySize = 0;

                        Head = new RTMPHead(ArrayValue, 0);
                        if ((Head.FmtType == RTMPHead.enumFmtType.Type2) || (Head.FmtType == RTMPHead.enumFmtType.Type3))
                        {
                            // Type2
                            // 長度與上一封包相同
                            if (iChunkStreamList.ContainsKey(Head.ChunkStreamID))
                            {
                                RTMPChunkStream RCS = this.iChunkStreamList[Head.ChunkStreamID];
                                RTMPHead iLastHead = RCS.Head;

                                BodySize = iLastHead.BodySize;
                            }
                        }
                        else
                        {
                            BodySize = Head.BodySize;
                        }

                        if (ChunkSize != -1)
                        {
                            if (BodySize % ChunkSize == 0)
                            {
                                if ((BodySize / ChunkSize) != 0)
                                    ChunkCount = BodySize / ChunkSize - 1;
                                else
                                    ChunkCount = 0;
                            }
                            else
                            {
                                ChunkCount = (BodySize / ChunkSize);
                            }
                        }
                        else
                        {
                            ChunkCount = 0;
                        }

                        NeedPacketSize = BodySize + NeedDataLength2 + ChunkCount;
                        if (ArraySize >= BodySize + NeedDataLength2 + ChunkCount)
                        {
                            if (ExtendedTimestamp == false)
                                RetValue = BodySize + NeedDataLength2 + ChunkCount;
                            else
                                RetValue = BodySize + NeedDataLength2 + ChunkCount + (ChunkCount * 4);
                        }
                    }
                }
            }

            return RetValue;
        }

        public RTMP ParsingFromArray(byte[] Value, int ValueArraySize, int ChunkSize, out int ProcessBytes)
        {
            RTMP RetValue = null;
            int PacketSize;
            int argNeedPacketSize = 0;
            int OutBytes = 0;

            PacketSize = IsDataAvailable(Value, ValueArraySize, ChunkSize, ref argNeedPacketSize);
            if (PacketSize != -1)
            {
                byte[] RTMPArray = null;
                RTMPHead TmpHead = null;
                RTMPHead Head = null;
                RTMPBodyBase Body = null;
                int BodyOffset;
                byte[] BodyArray = null;
                byte[] BodyValue = null;

                RTMPArray = (byte[])Array.CreateInstance(typeof(byte), PacketSize);
                Array.Copy(Value, 0, RTMPArray, 0, RTMPArray.Length);

                TmpHead = new RTMPHead(RTMPArray, 0);

                BodyOffset = TmpHead.HeaderSize;
                if ((TmpHead.FmtType == RTMPHead.enumFmtType.Type2) || (TmpHead.FmtType == RTMPHead.enumFmtType.Type3))
                {
                    if (iChunkStreamList.ContainsKey(TmpHead.ChunkStreamID))
                    {
                        RTMPChunkStream RCS = this.iChunkStreamList[Head.ChunkStreamID];
                        RTMPHead PrevHead;

                        PrevHead = RCS.Head;

                        TmpHead.BodySize = PrevHead.BodySize;
                        TmpHead.ChunkStreamID = PrevHead.ChunkStreamID;
                        TmpHead.StreamID = PrevHead.StreamID;
                        TmpHead.TypeID = PrevHead.TypeID;

                        if (TmpHead.FmtType == RTMPHead.enumFmtType.Type3)
                            TmpHead.Timestamp = PrevHead.Timestamp;
                    }
                }
                else if (iChunkStreamList.ContainsKey(TmpHead.ChunkStreamID))
                {
                    this.iChunkStreamList[TmpHead.ChunkStreamID].Head = TmpHead;
                }
                else
                {
                    RTMPChunkStream RCS = new RTMPChunkStream();

                    RCS.Head = TmpHead;
                    iChunkStreamList.Add(TmpHead.ChunkStreamID, RCS);
                }

                Head = TmpHead;
                BodyArray = (byte[])System.Array.CreateInstance(typeof(byte), RTMPArray.Length - BodyOffset);
                Array.Copy(RTMPArray, BodyOffset, BodyArray, 0, BodyArray.Length);

                System.Console.WriteLine("ChunkStreamID:" + Head.ChunkStreamID + ", Type=" + Head.FmtType + ", BodySize=" + Head.BodySize);
                if ((Head.BodySize > ChunkSize) && (ChunkSize != -1))
                {
                    System.Collections.Generic.List<byte> BodyList = new System.Collections.Generic.List<byte>();
                    UltimateByteArrayClass BodyTmpArrayList = new UltimateByteArrayClass();
                    byte[] ChunkBody = null;

                    BodyTmpArrayList.AddRange(BodyArray);
                    ChunkBody = (byte[])System.Array.CreateInstance(typeof(byte), ChunkSize);

                    BodyTmpArrayList.CopyTo(0, ChunkBody, 0, ChunkBody.Length);
                    BodyTmpArrayList.RemoveRange(0, ChunkBody.Length);
                    BodyList.AddRange(ChunkBody);

                    OutBytes += (Head.HeaderSize + ChunkBody.Length);

                    for (int _Loop = 1; _Loop <= 1000; _Loop++)
                    {
                        RTMPHead ChunkHead = null;
                        long ChunkTimestamp;

                        if (BodyTmpArrayList.Count <= 0)
                            break;

                        if (BodyList.Count >= Head.BodySize)
                            break;

                        ChunkHead = new RTMPHead(BodyTmpArrayList.InternalBuffer, 0);
                        System.Console.WriteLine("ChunkStreamID-2:" + ChunkHead.ChunkStreamID + ", Type=" + ChunkHead.FmtType + ", BodySize=" + ChunkHead.BodySize);

                        if (ChunkHead.ChunkStreamID == Head.ChunkStreamID)
                        {
                            BodyTmpArrayList.RemoveRange(0, ChunkHead.HeaderSize);

                            if (Head.HasExtendedTimestamp)
                            {
                                // Header 後面跟隨 4 bytes timestamp
                                ChunkTimestamp = (long)System.Math.Round(BodyTmpArrayList[0] * System.Math.Pow(256d, 3d) +
                                                                     BodyTmpArrayList[1] * System.Math.Pow(256d, 2d) +
                                                                     BodyTmpArrayList[2] * 256 +
                                                                     BodyTmpArrayList[3]);

                                BodyTmpArrayList.RemoveRange(0, 4);
                                OutBytes += 4;
                            }

                            if ((Head.BodySize - BodyList.Count) >= ChunkSize)
                            {
                                if (BodyTmpArrayList.Count <= ChunkSize)
                                {
                                    ChunkBody = (byte[])System.Array.CreateInstance(typeof(byte), BodyTmpArrayList.Count);
                                }
                                else
                                {
                                    ChunkBody = (byte[])System.Array.CreateInstance(typeof(byte), ChunkSize);
                                }
                            }
                            else
                            {
                                ChunkBody = (byte[])System.Array.CreateInstance(typeof(byte), (Head.BodySize - BodyList.Count));
                            }

                            BodyTmpArrayList.CopyTo(0, ChunkBody, 0, ChunkBody.Length);
                            BodyTmpArrayList.RemoveRange(0, ChunkBody.Length);
                            BodyList.AddRange(ChunkBody);

                            OutBytes += (ChunkHead.HeaderSize + ChunkBody.Length);
                        }
                        else
                        {
                            throw new System.Exception("Invalid chunk stream ID");
                        }
                    }

                    if (BodyList.Count >= Head.BodySize)
                    {
                        BodyValue = BodyList.ToArray();
                    }
                }
                else
                {
                    OutBytes += (Head.HeaderSize + Head.BodySize);
                    BodyValue = BodyArray;
                }


                if (BodyValue != null)
                {
                    switch (Head.TypeID)
                    {
                        case RTMPHead.enumTypeID.AMF0Command:
                            Body = new AMFCommand.AMFCommandBody(BodyValue, 0);

                            break;
                        case RTMPHead.enumTypeID.AMF0Data:
                            Body = new AMFCommand.AMFCommandBody(BodyValue, 0);

                            break;
                        case RTMPHead.enumTypeID.AMF3Command:
                            Body = new AMFCommand.AMFCommandBody(BodyValue, 1);

                            break;
                        case RTMPHead.enumTypeID.SetPeerBandwidth:
                            Body = new RTMPBodyPeerBandwidth(BodyValue, 0);

                            break;
                        case RTMPHead.enumTypeID.SetWindowSize:
                            Body = new RTMPBodyWindowSize(BodyValue, 0);

                            break;
                        case RTMPHead.enumTypeID.SetChunkSize:
                            Body = new RTMPBodyChunkSize(BodyValue, 0);

                            break;
                        case RTMPHead.enumTypeID.VideoData:
                            if (Head.BodySize > 5)
                                Body = new RTMPBodyVideoData(BodyValue, 0, Head.BodySize);

                            break;
                        case RTMPHead.enumTypeID.AudioData:
                            Body = new RTMPBodyAudioData(BodyValue, 0, Head.BodySize);

                            break;
                        case RTMPHead.enumTypeID.UserControlMsg:
                            Body = UCM.UCMBase.ParseUCM(BodyValue, 0);

                            break;
                        case RTMPHead.enumTypeID.Aggregate:
                            Body = new RTMPBodyAggregate(BodyValue, 0, Head.BodySize);

                            break;
                        case RTMPHead.enumTypeID.AboutMsg:
                            Body = new RTMPBodyAbort(BodyValue, 0);

                            break;
                        case RTMPHead.enumTypeID.Ack:
                            Body = new RTMPBodyAcknowledgement(BodyValue, 0);

                            break;
                        case RTMPHead.enumTypeID.Unknow:
                            break;
                        default:
                            throw new System.Exception("Unknow TypeID:" + Head.TypeID);
                    }

                    if (Body != null)
                    {
                        RetValue = new RTMP(Head, Body);

                        if ((TmpHead.FmtType == RTMPHead.enumFmtType.Type2) || (TmpHead.FmtType == RTMPHead.enumFmtType.Type3))
                        {
                            RetValue.IsFmtType2 = true;
                        }
                    }

                    ProcessBytes = OutBytes;
                }
                else
                    ProcessBytes = 0;
            }
            else
                ProcessBytes = 0;

            return RetValue;
        }

        public byte[] ToByteArray(int ChunkSize)
        {
            System.Collections.Generic.List<byte> RetValue = new System.Collections.Generic.List<byte>();
            System.Collections.Generic.List<byte> BodyArray = new System.Collections.Generic.List<byte>();
            byte[] BodyContentArray = null;

            if (Body != null)
            {
                BodyContentArray = Body.ToByteArray();
                if (Head.TypeID == RTMPHead.enumTypeID.AMF3Command)
                {
                    Head.BodySize = BodyContentArray.Length + 1;
                }
                else
                {
                    Head.BodySize = BodyContentArray.Length;
                }
            }
            else
            {
                Head.BodySize = 0;
            }

            RetValue.AddRange(Head.ToByteArray());
            if (Head.TypeID == RTMPHead.enumTypeID.AMF3Command)
            {
                RetValue.Add(0);
            }

            if (BodyContentArray != null)
            {
                if (ChunkSize != -1)
                {
                    byte[] ChunkBody = null;

                    if (BodyContentArray.Length >= ChunkSize)
                        ChunkBody = (byte[])System.Array.CreateInstance(typeof(byte), ChunkSize);
                    else
                        ChunkBody = (byte[])System.Array.CreateInstance(typeof(byte), BodyContentArray.Length);

                    System.Array.Copy(BodyContentArray, 0, ChunkBody, 0, ChunkBody.Length);
                    BodyArray.AddRange(ChunkBody);
                    if (BodyContentArray.Length > ChunkSize)
                    {
                        // 每 128 bytes 建立一個 Chunk (FmtType=3)
                        int ChunkCount;
                        if (BodyContentArray.Length % ChunkSize == 0)
                            ChunkCount = BodyContentArray.Length / ChunkSize - 1;
                        else
                            ChunkCount = BodyContentArray.Length / ChunkSize;

                        for (int I = 1, loopTo = ChunkCount; I <= loopTo; I++)
                        {
                            int ChunkBodySize = ChunkSize;
                            if (BodyContentArray.Length - I * ChunkSize <= ChunkSize)
                            {
                                ChunkBodySize = BodyContentArray.Length - I * ChunkSize;
                            }

                            if (ChunkBodySize > 0)
                            {
                                ChunkBody = (byte[])System.Array.CreateInstance(typeof(byte), ChunkBodySize);
                                System.Array.Copy(BodyContentArray, I * ChunkSize, ChunkBody, 0, ChunkBody.Length);

                                BodyArray.Add((byte)(0xC0 | Head.ChunkStreamID));
                                BodyArray.AddRange(ChunkBody);
                            }
                        }
                    }
                }
                else
                {
                    BodyArray.AddRange(BodyContentArray);
                }
            }

            if (BodyArray.Count > 0)
            {
                RetValue.AddRange(BodyArray.ToArray());
            }

            return RetValue.ToArray();
        }

        public RTMP()
        {
        }

        public RTMP(RTMPHead Head, RTMPBodyBase Body)
        {
            this.Head = Head;
            this.Body = Body;
        }

        public RTMP(RTMPHead.enumFmtType FmtType, RTMPHead.enumTypeID TypeID, RTMPBodyBase RTMPBody)
        {
            Head = new RTMPHead();
            Head.FmtType = FmtType;
            Head.TypeID = TypeID;
            Body = RTMPBody;
        }

        internal class RTMPChunkStream {
            public RTMPHead Head;
            public UltimateByteArrayClass ContentArray;
        }
    }
}
