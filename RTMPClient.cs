using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RTMPLibrary
{
    public partial class RTMPClient : IDisposable
    {
        private TCPSocket iTCP;
        private UltimateByteArrayClass iBuffer = new UltimateByteArrayClass(1000000);
        private string iServerName;
        private int iPort;
        private string iPath;
        private string iUrl;
        private string iVersion = "9,0,124,2";
        private int iChunkSize = 128;
        private int iServerWindowAckSize = 5000000;
        private int iClientWindowAckSize = 5000000;
        private int iServerBandwidth = 5000000;
        private int iServerBandwidthType = 2; // Dynamic
        private int iBufferLengthMS = 1000; // ms
        private long iTotalBodyBytes = 0L;
        private long iLastACKValue = 0L;
        private int iFmtTimestamp = 0;
        private object iLastMsg;
        private int iPlayStreamID = 1;
        private enumRTMPHandshakeState iHandshakeState;
        private enumRTMPConnectState iConnectState;
        private RTMPHandshake iHandshake;
        private int iUCMStreamID = -1;
        private byte[] iLastPacket = null;
        private RTMP iRTMP = new RTMP();
        private int iTransactionID = 1;
        private bool iBeginStreamReceived = false;
        private bool iIsPublishMode = false;
        private bool iAlreadySendDataFrame = false;
        private string iStreamName;
        private string iAppName;

        private bool iAVConfigSet = false;
        private bool iEnableVideoTrack = false;
        private bool iEnableAudioTrack = false;
        private bool iVideoFirstFrameRaised = false;
        private bool iAudioFirstFrameRaised = false;
        private byte[] H264SPSConfig = null;
        private byte[] H264PPSConfig = null;
        private byte[] AACConfig = null;

        public string tcUrl;
        public string swfUrl;
        public string pageUrl;
        public string VideoUser;
        public string VideoPassword;
        public object Tag;

        public delegate void HandshakeFailEventHandler(RTMPClient sender);
        public event HandshakeFailEventHandler HandshakeFail;

        public delegate void HandshakeCompletedEventHandler(RTMPClient sender);
        public event HandshakeCompletedEventHandler HandshakeCompleted;

        public delegate void OnCommandStatusEventHandler(RTMPClient sender, string Code, AMFCommand.onStatusResult Status);
        public event OnCommandStatusEventHandler OnCommandStatus;

        public delegate void OnDataStatusEventHandler(RTMPClient sender, string Code, AMFData.onStatusResult Status);
        public event OnDataStatusEventHandler OnDataStatus;

        public delegate void OnMetaDataEventHandler(RTMPClient sender, AMFCommand.onMetaData MetaData);
        public event OnMetaDataEventHandler OnMetaData;

        public delegate void VideoDataEventHandler(RTMPClient sender, uint TimeDelta, RTMPBodyVideoData VD);
        public event VideoDataEventHandler VideoData;

        public delegate void AudioDataEventHandler(RTMPClient sender, uint TimeDelta, RTMPBodyAudioData AD);
        public event AudioDataEventHandler AudioData;

        public delegate void DisconnectEventHandler(RTMPClient sender);
        public event DisconnectEventHandler Disconnect;

        public enum enumAudioFrameType
        {
            AudioHeader,
            AudioRaw
        }

        public enum enumVideoFrameType
        {
            KeyFrame,
            NonKeyFrame
        }

        public enum enumRTMPHandshakeState
        {
            HandshakeC0,
            HandshakeC2,
            Completed
        }

        public enum enumRTMPConnectState
        {
            Connect = 0,
            GetConnectResult,
            CreateStream,
            GetCreateStreamResult,
            Play,
            GetPlayResult,
            BeginStream,
            Publish,
            BeginPublish
        }

        public RTMPClient()
        {
            iHandshake = new RTMPHandshake(iVersion);
        }

        public void Close()
        {
            if (iTCP != null)
            {
                iTCP.DataReceived -= iTCP_DataReceived;
                iTCP.Disconnect -= iTCP_Disconnect;

                iTCP.Close();
                iTCP.Dispose();
                iTCP = default;
            }
        }

        public void SetAVConfig(AVConfig[] AVC)
        {
            if (iAVConfigSet == false)
            {
                foreach (AVConfig EachAVC in AVC)
                {
                    switch (EachAVC.AVType)
                    {
                        case AVConfig.enumAVType.H264SPS:
                            H264SPSConfig = EachAVC.ConfigValue;
                            iEnableVideoTrack = true;
                            break;
                        case AVConfig.enumAVType.H264PPS:
                            H264PPSConfig = EachAVC.ConfigValue;
                            break;
                        case AVConfig.enumAVType.AAC:
                            AACConfig = EachAVC.ConfigValue;
                            iEnableAudioTrack = true;
                            break;
                    }
                }

                iAVConfigSet = true;

                if (iConnectState == enumRTMPConnectState.Publish)
                {
                    SendDataFrame();
                    iConnectState = enumRTMPConnectState.BeginPublish;
                }
            }
        }

        public void SendVideo(enumVideoFrameType VideoFrameType, byte[] VideoData, int VideoOffset, int VideoLength, int TimeDeltaMS)
        {
            if (iConnectState == enumRTMPConnectState.BeginPublish)
            {
                RTMPBodyVideoData VideoBody;

                switch (VideoFrameType)
                {
                    case enumVideoFrameType.KeyFrame:
                        if (iVideoFirstFrameRaised == false)
                        {
                            RTMPBodyVideoData RTMPFirstVideoBody = null;

                            // 第一次傳送 KeyFrame
                            // 包含 RTMP Video Header
                            RTMPFirstVideoBody = RTMPBodyVideoData.ImportSPS(H264SPSConfig, H264PPSConfig);
                            if (RTMPFirstVideoBody != null)
                            {
                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, RTMPFirstVideoBody);
                                iVideoFirstFrameRaised = true;
                            }
                        }

                        // Console.WriteLine("Video frame:" & VideoData(VideoOffset + 0) & ":" & VideoData(VideoOffset + 1) & ":" & VideoData(VideoOffset + 2) & ":" & VideoData(VideoOffset + 3))
                        VideoBody = RTMPBodyVideoData.ImportVideoRaw(RTMPBodyVideoData.enumFrameType.KeyFrame, VideoData, VideoOffset, VideoLength);
                        VideoBody.CompositionTime = 0;
                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, VideoBody);

                        break;
                    case enumVideoFrameType.NonKeyFrame:
                        VideoBody = RTMPBodyVideoData.ImportVideoRaw(RTMPBodyVideoData.enumFrameType.NonKeyFrame, VideoData, VideoOffset, VideoLength);
                        VideoBody.CompositionTime = 0;
                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, VideoBody);

                        break;
                }
            }

        }

        public void SendVideo(enumVideoFrameType VideoFrameType, List<byte[]> VideoDataList, int TimeDeltaMS)
        {
            if (iConnectState == enumRTMPConnectState.BeginPublish)
            {
                RTMPBodyVideoData VideoBody;

                switch (VideoFrameType)
                {
                    case enumVideoFrameType.KeyFrame:
                        if (iVideoFirstFrameRaised == false)
                        {
                            RTMPBodyVideoData RTMPFirstVideoBody = null;

                            // 第一次傳送 KeyFrame
                            // 包含 RTMP Video Header
                            RTMPFirstVideoBody = RTMPBodyVideoData.ImportSPS(H264SPSConfig, H264PPSConfig);
                            if (RTMPFirstVideoBody != null)
                            {
                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, RTMPFirstVideoBody);
                                iVideoFirstFrameRaised = true;
                            }
                        }

                        // Console.WriteLine("Video frame:" & VideoData(VideoOffset + 0) & ":" & VideoData(VideoOffset + 1) & ":" & VideoData(VideoOffset + 2) & ":" & VideoData(VideoOffset + 3))
                        VideoBody = RTMPBodyVideoData.ImportVideoRaw(RTMPBodyVideoData.enumFrameType.KeyFrame, VideoDataList);
                        VideoBody.CompositionTime = 0;
                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, VideoBody);

                        break;
                    case enumVideoFrameType.NonKeyFrame:
                        VideoBody = RTMPBodyVideoData.ImportVideoRaw(RTMPBodyVideoData.enumFrameType.NonKeyFrame, VideoDataList);
                        VideoBody.CompositionTime = 0;
                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, VideoBody);

                        break;
                }
            }

        }

        public void SendAudio(enumAudioFrameType AudioFrameType, byte[] AudioData, int Offset, int Length, int TimeDeltaMS)
        {
            if (iConnectState == enumRTMPConnectState.BeginPublish)
            {
                RTMPBodyAudioData AudioBody;
                byte[] AudioCopyData;

                if (Length > 0)
                {
                    if (AudioFrameType == enumAudioFrameType.AudioHeader)
                    {
                        // Push 時, Timestamp 使用相同時間軸
                        // 因此以 VideoTimestamp 為基礎
                        RTMPBodyAudioData RTMPFirstAudioBody = null;
                        byte[] AudioConfigData;

                        AudioConfigData = (byte[])Array.CreateInstance(typeof(byte), Length + 1);
                        AudioConfigData[0] = 0x00;
                        Array.Copy(AudioData, Offset, AudioConfigData, 1, Length);

                        // 第一次傳送 KeyFrame
                        // 包含 RTMP Video Header
                        RTMPFirstAudioBody = new RTMPBodyAudioData();
                        RTMPFirstAudioBody.AudioData = AudioConfigData;

                        AACConfig = AudioConfigData;

                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 4, TimeDeltaMS, RTMPHead.enumTypeID.AudioData, iPlayStreamID, RTMPFirstAudioBody);
                        iAudioFirstFrameRaised = true;
                    }
                    else if (AudioFrameType == enumAudioFrameType.AudioRaw)
                    {
                        // Console.WriteLine("Video frame:" & VideoData(VideoOffset + 0) & ":" & VideoData(VideoOffset + 1) & ":" & VideoData(VideoOffset + 2) & ":" & VideoData(VideoOffset + 3))
                        AudioCopyData = (byte[])Array.CreateInstance(typeof(byte), Length + 1);
                        AudioCopyData[0] = 0x01;
                        Array.Copy(AudioData, Offset, AudioCopyData, 1, Length);

                        AudioBody = new RTMPBodyAudioData();
                        AudioBody.AudioData = AudioCopyData;

                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 4, TimeDeltaMS, RTMPHead.enumTypeID.AudioData, iPlayStreamID, AudioBody);
                    }
                }
            }
        }

        public void PushURL(string URL)
        {
            iIsPublishMode = true;

            RequestRTMP(URL);
        }

        public void PlayURL(string URL)
        {
            iIsPublishMode = false;

            RequestRTMP(URL);
        }

        private void RequestRTMP(string URL)
        {
            Uri URI = new Uri(URL);
            bool IsConnected = false;
            string TmpString;

            if (URI.Scheme.ToUpper() == "RTMP".ToUpper())
            {
                iUrl = URL;
                iServerName = URI.Host;
                iUCMStreamID = -1;
                iTotalBodyBytes = 0;
                iLastACKValue = 0;
                iFmtTimestamp = 0;
                iTransactionID = 1;
                iAlreadySendDataFrame = false;

                iAVConfigSet = false;
                iEnableVideoTrack = false;
                iEnableAudioTrack = false;
                iVideoFirstFrameRaised = false;
                iAudioFirstFrameRaised = false;
                H264SPSConfig = null;
                H264PPSConfig = null;
                AACConfig = null;

                TmpString = URI.AbsolutePath;
                if (string.IsNullOrEmpty(TmpString) == false)
                {
                    int Tmp1;

                    if (TmpString.Substring(0, 1) == @"/")
                        TmpString = TmpString.Substring(1);

                    Tmp1 = TmpString.LastIndexOf("/");
                    if (Tmp1 != -1)
                    {
                        iStreamName = TmpString.Substring(Tmp1 + 1);
                        iAppName = TmpString.Substring(0, Tmp1);
                    }
                    else
                    {
                        iStreamName = TmpString;
                        iAppName = string.Empty;
                    }

                    if (string.IsNullOrEmpty(URI.Query) == false)
                        iStreamName += URI.Query;
                }
                else
                {
                    iStreamName = string.Empty;
                    iAppName = string.Empty;
                }

                if (URI.IsDefaultPort)
                    iPort = 1935;
                else
                    iPort = URI.Port;

                iPath = URI.PathAndQuery;
                iHandshakeState = enumRTMPHandshakeState.HandshakeC0;
                iBeginStreamReceived = false;

                iTCP = new TCPSocket();
                iTCP.DataReceived += iTCP_DataReceived;
                iTCP.Disconnect += iTCP_Disconnect;

                try
                {
                    iTCP.Connect(iServerName, iPort);
                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    Close();
                    throw ex;
                }

                if (IsConnected)
                {
                    iTCP.StartReceive();
                    ProcessHandshake(null);
                }
            }
            else
            {
                throw new Exception("Unknow URL:" + URI.Scheme);
            }
        }

        private void ProcessHandshake(byte[] RecvPacket)
        {
            switch (iHandshakeState)
            {
                case enumRTMPHandshakeState.HandshakeC0:
                    iTCP.SendData(CreateHandshake(Common.enumHandshakeType.C0C1, default));
                    break;
                case enumRTMPHandshakeState.HandshakeC2:
                    iTCP.SendData(CreateHandshake(Common.enumHandshakeType.C2, RecvPacket));
                    break;
            }
        }

        private byte[] CreateHandshake(Common.enumHandshakeType HandshakeType, byte[] input)
        {
            byte[] RetValue = null;
            var R = new Random();

            // 1537
            switch (HandshakeType)
            {
                case Common.enumHandshakeType.C0C1:
                    {
                        byte[] C1 = iHandshake.CreateC1();

                        // version + 1536
                        RetValue = (byte[])Array.CreateInstance(typeof(byte), 1537);
                        RetValue[0] = 3;
                        Array.Copy(C1, 0, RetValue, 1, C1.Length);

                        break;
                    }

                case Common.enumHandshakeType.S0S1S2:
                    {
                        // version + 1536 + 1536 = 3073
                        RTMPHandshake.PackageValidate C1PV;
                        byte[] C1 = null;
                        byte[] S1S2 = null;

                        RetValue = (byte[])Array.CreateInstance(typeof(byte), 3073);
                        RetValue[0] = 3;
                        if (input.Length >= 1537)
                        {
                            if (input[0] == 3)
                            {
                                C1 = (byte[])Array.CreateInstance(typeof(byte), 1536);
                                Array.Copy(input, 1, C1, 0, C1.Length);
                                C1PV = iHandshake.ValidC1(C1);
                                S1S2 = iHandshake.CreateS1S2(C1PV);
                                Array.Copy(S1S2, 0, RetValue, 1, S1S2.Length);
                            }
                        }

                        break;
                    }

                case Common.enumHandshakeType.C2:
                    {
                        // 1536
                        byte[] S1 = null;
                        RTMPHandshake.PackageValidate S1PV = default;

                        if (input.Length >= 1537)
                        {
                            if (input[0] == 3)
                            {
                                // Version=3
                                S1 = (byte[])Array.CreateInstance(typeof(byte), 1536);
                                Array.Copy(input, 1, S1, 0, S1.Length);
                                S1PV = iHandshake.ValidS1(S1);
                                if (S1PV != null)
                                {
                                    RetValue = iHandshake.CreateC2(S1PV);
                                }
                                else
                                {
                                    throw new Exception("S1 not valid");
                                }
                            }
                        }

                        break;
                    }
            }

            return RetValue;
        }

        private void SendRTMPConnect()
        {
            AMFCommand.Connect RTMPConnect = new AMFCommand.Connect();
            string[] PathArray = null;
            string iTcUrl = tcUrl;

            // iHandshakeState = enumRTMPHandshakeState.HandshakeC0

            // 取得第一個路徑做為 app
            if (iPath.Substring(0, 1) == "/")
                PathArray = iPath.Substring(1).Split("/");
            else
                PathArray = iPath.Split("/");


            // 重組 tcUrl
            if (string.IsNullOrEmpty(iTcUrl))
            {
                iTcUrl = "rtmp://" + iServerName + ":" + iPort;
                if (PathArray.Length >= 2)
                {
                    for (int I = 0; I <= PathArray.Length - 2; I++)
                        iTcUrl = iTcUrl + "/" + PathArray[I];
                }
            }

            RTMPConnect.CommandName.Value = "connect";
            RTMPConnect.TransactionID.Value = iTransactionID++;
            RTMPConnect.CommandObject.SetValue("app", iAppName);
            RTMPConnect.CommandObject.SetValue("flashVer", "LNX " + iVersion);
            RTMPConnect.CommandObject.SetValue("tcUrl", iTcUrl);
            RTMPConnect.CommandObject.SetValue("swfUrl", swfUrl);
            RTMPConnect.CommandObject.SetValue("pageUrl", pageUrl);
            RTMPConnect.CommandObject.SetValue("fpad", false);
            //RTMPConnect.CommandObject.SetValue("capabilities", 15);
            //RTMPConnect.CommandObject.SetValue("audioCodecs", 4071);
            //RTMPConnect.CommandObject.SetValue("videoCodecs", 252);
            //RTMPConnect.CommandObject.SetValue("videoFunction", 1);

            if ((string.IsNullOrEmpty(VideoUser) == false) || (string.IsNullOrEmpty(VideoPassword) == false))
            {
                RTMPConnect.GetBody.AMF0List.Add(new AMF0Objects.AMF0String() { Value = VideoUser });
                RTMPConnect.GetBody.AMF0List.Add(new AMF0Objects.AMF0String() { Value = VideoPassword });
            }

            SendRTMPBody(RTMPHead.enumFmtType.Type0, 3, 0, RTMPHead.enumTypeID.AMF0Command, 0, RTMPConnect.GetBody);
        }

        private void SendRTMPBody(RTMPHead.enumFmtType FmtType, int ChunkStreamID, int Timestamp, RTMPHead.enumTypeID TypeID, int StreamID, RTMPBodyBase Body)
        {
            iTCP.SendData(GetRTMPBodyArray(FmtType, ChunkStreamID, Timestamp, TypeID, StreamID, Body));
        }

        private byte[] GetRTMPBodyArray(RTMPHead.enumFmtType FmtType, int ChunkStreamID, int Timestamp, RTMPHead.enumTypeID TypeID, int StreamID, RTMPBodyBase Body)
        {
            RTMP R = new RTMP(FmtType, TypeID, Body);

            R.Head.ChunkStreamID = ChunkStreamID;
            R.Head.Timestamp = (uint)Timestamp;
            R.Head.StreamID = (uint)StreamID;

            return R.ToByteArray(iChunkSize);
        }

        private void iTCP_DataReceived(TCPSocket sender, int recvCount)
        {
            if (recvCount > 0)
            {
                iBuffer.AddRange(sender.ReceiveBuffer, 0, recvCount);
                iTotalBodyBytes = (iTotalBodyBytes + recvCount) % uint.MaxValue;

                // Console.WriteLine("Recv size:" & recvCount & " / " & iBuffer.Count)
            }

            switch (iHandshakeState)
            {
                case enumRTMPHandshakeState.HandshakeC0:
                    if (iBuffer.Count >= 3073)
                    {
                        iHandshakeState = enumRTMPHandshakeState.HandshakeC2;
                        ProcessHandshake(iBuffer.ToArray());
                        iBuffer.Clear();
                        iHandshakeState = enumRTMPHandshakeState.Completed;
                        iConnectState = enumRTMPConnectState.Connect;

                        // 呼叫 connect
                        SendRTMPConnect();
                    }

                    break;
                case enumRTMPHandshakeState.Completed:
                    bool BufferProcessed = false;

                    for (int _Loop = 1; _Loop <= 10000; _Loop++)
                    {
                        if (iBuffer.Count > 0)
                        {
                            int TotalCount = -1;
                            int NeedPacketSize = 0;

                            TotalCount = iRTMP.IsDataAvailable(iBuffer.InternalBuffer, iBuffer.Count, iChunkSize, ref NeedPacketSize);
                            if (TotalCount != -1)
                            {
                                RTMP ServerR = null;

                                //try
                                {
                                    ServerR = iRTMP.ParsingFromArray(iBuffer.InternalBuffer, iBuffer.Count, iChunkSize);
                                }
                                //catch (Exception ex)
                                //{
                                //    Console.WriteLine("Invalid packet:" + ex.Message);
                                //    iBuffer.Clear();
                                //    BufferProcessed = true;
                                //    break;
                                //}

                                if (ServerR != null)
                                {
                                    iFmtTimestamp += (int)ServerR.Head.Timestamp;

                                    if (ServerR.Body == null)
                                    {
                                        // 透過 chunk stream id 辨識
                                        if ((ServerR.Head.ChunkStreamID == iUCMStreamID) && (ServerR.IsFmtType2 == false))
                                            TotalCount = ServerR.Head.HeaderSize + 6;
                                    }
                                    else
                                    {
                                        // Console.WriteLine(ServerR.Head.TypeID.ToString & "  FmtType:" & ServerR.Head.FmtType & "(" & ServerR.IsFmtType2.ToString & "), ChunkStreamID:" & ServerR.Head.ChunkStreamID & ", StreamID:" & ServerR.Head.StreamID & ", Timestamp:" & ServerR.Head.Timestamp & ", Bodysize:" & ServerR.Head.BodySize)

                                        switch (ServerR.Head.TypeID)
                                        {
                                            case RTMPHead.enumTypeID.SetChunkSize:
                                                {
                                                    RTMPBodyChunkSize Body = (RTMPBodyChunkSize)ServerR.Body;

                                                    iChunkSize = (int)Body.ChunkSize;

                                                    break;
                                                }
                                            case RTMPHead.enumTypeID.SetWindowSize:
                                                {
                                                    RTMPBodyWindowSize Body = (RTMPBodyWindowSize)ServerR.Body;

                                                    iServerWindowAckSize = (int)Body.WindowSize;

                                                    break;
                                                }
                                            case RTMPHead.enumTypeID.SetPeerBandwidth:
                                                {
                                                    RTMPBodyPeerBandwidth Body = (RTMPBodyPeerBandwidth)ServerR.Body;

                                                    iServerBandwidth = (int)Body.WindowSize;
                                                    iServerBandwidthType = Body.LimitType;

                                                    break;
                                                }
                                            case RTMPHead.enumTypeID.Aggregate:
                                                {
                                                    // 混合封包
                                                    RTMPBodyAggregate AggreMsg;
                                                    AggreMsg = (RTMPBodyAggregate)ServerR.Body;

                                                    // Console.WriteLine("Aggregate package:" & AggreMsg.GetList.Count)
                                                    foreach (RTMPBodyAggregate.AggregateMessage EachMsg in AggreMsg.GetList())
                                                    {
                                                        // Console.WriteLine("TypeID [" & EachMsg.TypeID.ToString & "] Aggregate Timestamp:" & EachMsg.Timestamp & ", LastVideoTimestamp:" & iLastVideoTimestamp)

                                                        switch (EachMsg.TypeID)
                                                        {
                                                            case RTMPHead.enumTypeID.VideoData:
                                                                {
                                                                    RTMPBodyVideoData VideoBody = null;

                                                                    try { VideoBody = new RTMPBodyVideoData(EachMsg.Body, EachMsg.BodyOffset, EachMsg.BodyLength); }
                                                                    catch (Exception ex) { Console.WriteLine("Aggregate invalid video:" + ex.Message); }

                                                                    // Composition Time 等同 RTSP Timestamp (上一個封包的總合經過時間)
                                                                    // Time Delta 是每一個封包的時間差, 一個封包內可能包含許多 Video Data

                                                                    if (VideoBody != null)
                                                                    {
                                                                        VideoData?.Invoke(this, (uint)EachMsg.Timestamp - ServerR.Head.Timestamp, VideoBody);
                                                                    }

                                                                    break;
                                                                }
                                                            case RTMPHead.enumTypeID.AudioData:
                                                                {
                                                                    RTMPBodyAudioData AudioBody = null;

                                                                    try { AudioBody = new RTMPBodyAudioData(EachMsg.Body, EachMsg.BodyOffset, EachMsg.BodyLength); }
                                                                    catch (Exception ex) { Console.WriteLine("Aggregate invalid audio:" + ex.Message); }

                                                                    if (AudioBody != null)
                                                                    {
                                                                        AudioData?.Invoke(this, (uint)EachMsg.Timestamp - ServerR.Head.Timestamp, AudioBody);
                                                                    }

                                                                    break;
                                                                }
                                                            default:
                                                                {
                                                                    if (Enum.IsDefined(typeof(RTMPHead.enumTypeID), EachMsg.TypeID) == false)
                                                                    {
                                                                        Console.WriteLine("Aggregate invalid type id:" + EachMsg.TypeID + ", BodyLength:" + EachMsg.BodyLength);
                                                                    }

                                                                    break;
                                                                }
                                                        }
                                                    }

                                                    break;
                                                }
                                            case RTMPHead.enumTypeID.VideoData:
                                                {
                                                    RTMPBodyVideoData VideoBody = null;

                                                    try { VideoBody = (RTMPBodyVideoData)ServerR.Body; }
                                                    catch (Exception ex) { Console.WriteLine("Video packet invalid"); }

                                                    if (VideoBody != null)
                                                    {
                                                        // Console.WriteLine("VideoData TimeDelta:" & TimeDelta & ", CompositionTime:" & VideoBody.CompositionTime)
                                                        // RaiseEvent VideoData(Me, TimeDelta, ServerR.Body)
                                                        VideoData?.Invoke(this, ServerR.Head.Timestamp, VideoBody);
                                                        // RaiseEvent VideoData(Me, 0, ServerR.Body)
                                                    }

                                                    break;
                                                }
                                            case RTMPHead.enumTypeID.AudioData:
                                                {
                                                    RTMPBodyAudioData AudioBody = null;

                                                    try { AudioBody = (RTMPBodyAudioData)ServerR.Body; }
                                                    catch (Exception ex) { Console.WriteLine("Audio packet invalid"); }

                                                    if (AudioBody != null)
                                                    {
                                                        AudioData?.Invoke(this, ServerR.Head.Timestamp, AudioBody);
                                                    }

                                                    break;
                                                }
                                            case RTMPHead.enumTypeID.UserControlMsg:
                                                UCM.UCMBase UCMBody = (UCM.UCMBase)ServerR.Body;

                                                if (UCMBody != null)
                                                {
                                                    if (iUCMStreamID == -1)
                                                        iUCMStreamID = ServerR.Head.ChunkStreamID;

                                                    switch (UCMBody.EventType)
                                                    {
                                                        case UCM.UCMBase.enumEventType.StreamBegin:
                                                            if (iConnectState == enumRTMPConnectState.GetPlayResult)
                                                                iConnectState = enumRTMPConnectState.BeginStream;
                                                            else
                                                                iBeginStreamReceived = true;

                                                            break;
                                                        case UCM.UCMBase.enumEventType.PingRequest:
                                                            UCM.PingRequest PReqBody = (UCM.PingRequest)UCMBody;
                                                            UCM.PingResponse PRespBody = new UCM.PingResponse();

                                                            PRespBody.Timestamp = PReqBody.Timestamp;
                                                            SendRTMPBody(RTMPHead.enumFmtType.Type0, 2, 0, RTMPHead.enumTypeID.UserControlMsg, (int)ServerR.Head.StreamID, PRespBody);

                                                            break;
                                                    }
                                                }

                                                break;
                                            case RTMPHead.enumTypeID.AMF0Data:
                                                switch (iConnectState)
                                                {
                                                    case enumRTMPConnectState.BeginStream:
                                                    case enumRTMPConnectState.GetPlayResult:
                                                        // 可能會有 onMetaData
                                                        AMFCommand.AMFCommandBody AMFBody = (AMFCommand.AMFCommandBody)ServerR.Body;
                                                        if (AMFBody.AMF0List.Count > 0)
                                                        {
                                                            if (AMFBody.AMF0List[0].ObjectType == Common.enumAMF0ObjectType.String)
                                                            {
                                                                AMF0Objects.AMF0String AMFCode = (AMF0Objects.AMF0String)AMFBody.AMF0List[0];

                                                                if (AMFCode.Value.ToUpper() == "onStatus".ToUpper())
                                                                {
                                                                    AMFData.onStatusResult AMFStatus = new AMFData.onStatusResult((AMFCommand.AMFCommandBody)ServerR.Body);
                                                                    AMF0Objects.AMF0String AMFStringCode = null;

                                                                    AMFStringCode = (AMF0Objects.AMF0String)AMFStatus.Information.GetValue("code");
                                                                    if (AMFStringCode != null)
                                                                    {
                                                                        OnDataStatus?.Invoke(this, AMFStringCode.Value, AMFStatus);
                                                                    }
                                                                }
                                                                else if (AMFCode.Value.ToUpper() == "onMetaData".ToUpper())
                                                                {
                                                                    AMFCommand.onMetaData MetaBody = new AMFCommand.onMetaData((AMFCommand.AMFCommandBody)ServerR.Body);

                                                                    OnMetaData?.Invoke(this, MetaBody);
                                                                }
                                                                else
                                                                {
                                                                    // other event or message
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // other variable
                                                            }
                                                        }

                                                        break;
                                                }

                                                break;
                                            case RTMPHead.enumTypeID.AMF0Command:
                                            case RTMPHead.enumTypeID.AMF3Command:
                                                switch (iConnectState)
                                                {
                                                    case enumRTMPConnectState.Connect:
                                                        AMFCommand.ConnectResult ConnectResult = new AMFCommand.ConnectResult((AMFCommand.AMFCommandBody)ServerR.Body);
                                                        AMF0Objects.AMF0String AMFString = null;

                                                        AMFString = (AMF0Objects.AMF0String)ConnectResult.Information.GetValue("code");
                                                        if (AMFString != null)
                                                        {
                                                            if (AMFString.Value.ToUpper().Contains("Success".ToUpper()))
                                                            {
                                                                iConnectState = enumRTMPConnectState.GetConnectResult;

                                                                HandshakeCompleted?.Invoke(this);
                                                            }
                                                            else
                                                            {
                                                                HandshakeFail?.Invoke(this);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            HandshakeFail?.Invoke(this);
                                                        }

                                                        break;

                                                    case enumRTMPConnectState.CreateStream:
                                                        AMFCommand.CreateStreamResult CSResult = new AMFCommand.CreateStreamResult((AMFCommand.AMFCommandBody)ServerR.Body);
                                                        if (CSResult.CommandName != null)
                                                        {
                                                            if (CSResult.CommandName.Value == "_result")
                                                            {
                                                                iPlayStreamID = Convert.ToInt32(CSResult.StreamID.Value);
                                                                iConnectState = enumRTMPConnectState.GetCreateStreamResult;
                                                            }
                                                            else
                                                            {
                                                                HandshakeFail?.Invoke(this);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            HandshakeFail?.Invoke(this);
                                                        }

                                                        break;

                                                    case enumRTMPConnectState.Play:
                                                        AMFCommand.onStatusResult PlayResult = new AMFCommand.onStatusResult((AMFCommand.AMFCommandBody)ServerR.Body);

                                                        if (PlayResult.CommandName.Value == "onStatus")
                                                        {
                                                            if (PlayResult.code != null)
                                                            {
                                                                OnCommandStatus?.Invoke(this, PlayResult.code, PlayResult);

                                                                if (iIsPublishMode)
                                                                {
                                                                    // publish 
                                                                    if (PlayResult.code.ToUpper().Contains("Publish.Start".ToUpper()))
                                                                    {
                                                                        iConnectState = enumRTMPConnectState.Publish;
                                                                    }
                                                                    else
                                                                    {
                                                                        // other event
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    // play
                                                                    if (PlayResult.code.ToUpper().Contains("Play.Start".ToUpper()))
                                                                    {
                                                                        if (iBeginStreamReceived == false)
                                                                        {
                                                                            iConnectState = enumRTMPConnectState.GetPlayResult;
                                                                        }
                                                                        else
                                                                        {
                                                                            iConnectState = enumRTMPConnectState.BeginStream;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        // other event
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                HandshakeFail?.Invoke(this);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            HandshakeFail?.Invoke(this);
                                                        }

                                                        break;
                                                    default:
                                                        AMFCommand.AMFCommandBody AMFBody = (AMFCommand.AMFCommandBody)ServerR.Body;

                                                        if (AMFBody.AMF0List.Count > 0)
                                                        {
                                                            if (AMFBody.AMF0List[0].ObjectType == Common.enumAMF0ObjectType.String)
                                                            {
                                                                AMF0Objects.AMF0String AMFCode = (AMF0Objects.AMF0String)AMFBody.AMF0List[0];

                                                                if (AMFCode.Value.ToUpper() == "onStatus".ToUpper())
                                                                {
                                                                    AMFCommand.onStatusResult AMFStatus = new AMFCommand.onStatusResult((AMFCommand.AMFCommandBody)ServerR.Body);
                                                                    AMF0Objects.AMF0String AMFStringCode = null;

                                                                    AMFStringCode = (AMF0Objects.AMF0String)AMFStatus.Information.GetValue("code");
                                                                    if (AMFStringCode != null)
                                                                    {
                                                                        OnCommandStatus?.Invoke(this, AMFStringCode.Value, AMFStatus);
                                                                    }
                                                                }
                                                                else if (AMFCode.Value.ToUpper() == "onMetaData".ToUpper())
                                                                {
                                                                    AMFCommand.onMetaData MetaBody = new AMFCommand.onMetaData((AMFCommand.AMFCommandBody)ServerR.Body);

                                                                    OnMetaData?.Invoke(this, MetaBody);
                                                                }
                                                                else
                                                                {
                                                                    // other event or message
                                                                    Console.WriteLine("Unknow event:" + AMFCode.Value);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // other variable
                                                                throw new Exception("Unable to process ObjectType:" + AMFBody.AMF0List[0].ObjectType);
                                                            }
                                                        }

                                                        break;
                                                }

                                                break;
                                            default:
                                                Console.WriteLine("Invalid ID:" + ServerR.Head.TypeID);

                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("ServerR is null!");
                                }

                                // iLastPacket = Array.CreateInstance(GetType(Byte), TotalCount)
                                // iBuffer.CopyTo(0, iLastPacket, 0, iLastPacket.Length)

                                iBuffer.RemoveRange(0, TotalCount);
                            }
                            // Console.WriteLine("Remove count:" & TotalCount & ", FmtType:" & ServerR.Head.FmtType.ToString & ", IsType2:" & ServerR.IsFmtType2.ToString & ", TypeID:" & ServerR.Head.TypeID.ToString & " ChunkStreamID:" & ServerR.Head.ChunkStreamID)
                            else
                            {
                                if (NeedPacketSize > 1000000 | iBuffer.Count >= 500000)
                                {
                                    // 需要空間超過 1M (封包錯誤)
                                    Console.WriteLine("WARNING: Buffer clean, Need size:" + NeedPacketSize + ", Buffer Count:" + iBuffer.Count);
                                    // TotalCount = iRTMP.IsDataAvailable(iBuffer.InternalBuffer, iBuffer.Count, iChunkSize, NeedPacketSize)
                                    iBuffer.Clear();
                                    // iBuffer.AddRange(sender.ReceiveBuffer, 0, recvCount)
                                }

                                BufferProcessed = true;
                                break;
                            }
                        }
                        else
                        {
                            BufferProcessed = true;
                            break;
                        }
                    }

                    if (BufferProcessed == false)
                    {
                        if (iBuffer.Count > 0)
                            iBuffer.Clear();
                    }

                    if (iHandshakeState == enumRTMPHandshakeState.Completed)
                    {
                        switch (iConnectState)
                        {
                            case enumRTMPConnectState.GetConnectResult:
                                {
                                    List<byte> SendContentArray = new List<byte>();

                                    if (iIsPublishMode)
                                    {
                                        // Push stream
                                        // 發送 SetChunkSize
                                        RTMPBodyChunkSize ClientChunkSize = new RTMPBodyChunkSize();
                                        ClientChunkSize.ChunkSize = 4096;
                                        SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type0, 2, 0, RTMPHead.enumTypeID.SetChunkSize, 0, ClientChunkSize));

                                        // 發送 ReleaseStream
                                        AMFCommand.ReleaseStream RelStream = new AMFCommand.ReleaseStream(iTransactionID++, iStreamName);
                                        SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 3, 0, RTMPHead.enumTypeID.AMF0Command, 0, RelStream.GetBody));

                                        // 發送 FCPublish
                                        AMFCommand.FCPublish FCPub = new AMFCommand.FCPublish(iTransactionID++, iStreamName);
                                        SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 3, 0, RTMPHead.enumTypeID.AMF0Command, 0, FCPub.GetBody));
                                    }
                                    else
                                    {
                                        // Pull stream

                                        // 發送 Window ack
                                        RTMPBodyWindowSize WindowBody = new RTMPBodyWindowSize();

                                        WindowBody.WindowSize = (uint)iClientWindowAckSize;
                                        SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type0, 2, 0, RTMPHead.enumTypeID.SetWindowSize, 0, WindowBody));
                                    }

                                    // 發送 CreateStream
                                    AMFCommand.CreateStream ConnectBody = new AMFCommand.CreateStream(iTransactionID++);
                                    SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 3, 0, RTMPHead.enumTypeID.AMF0Command, 0, ConnectBody.GetBody));
                                    iTCP.SendData(SendContentArray.ToArray());

                                    SendContentArray.Clear();

                                    iConnectState = enumRTMPConnectState.CreateStream;  // 等待 Result
                                }

                                break;
                            case enumRTMPConnectState.GetCreateStreamResult:
                                {
                                    List<byte> SendContentArray = new List<byte>();

                                    if (iIsPublishMode)
                                    {
                                        // 發送 publish
                                        AMFCommand.Publish pubBody = new AMFCommand.Publish(iTransactionID++, iStreamName, iAppName);
                                        SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type0, 8, 0, RTMPHead.enumTypeID.AMF0Command, iPlayStreamID, pubBody.GetBody));
                                    }
                                    else
                                    {
                                        // 發送 Play
                                        AMFCommand.GetStreamLength StreamLenBody = new AMFCommand.GetStreamLength();
                                        StreamLenBody.StreamName.Value = iStreamName;
                                        StreamLenBody.TransactionID.Value = iTransactionID++;
                                        SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type0, 8, 0, RTMPHead.enumTypeID.AMF0Command, 0, StreamLenBody.GetBody));

                                        AMFCommand.Play PlayBody = new AMFCommand.Play();
                                        PlayBody.StreamName.Value = iStreamName;
                                        PlayBody.TransactionID.Value = iTransactionID++;
                                        SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type0, 8, 0, RTMPHead.enumTypeID.AMF0Command, iPlayStreamID, PlayBody.GetBody));

                                        UCM.SetBufferLength BufferLengthBody = new UCM.SetBufferLength();
                                        BufferLengthBody.StreamID = 1;
                                        BufferLengthBody.BufferLength = (uint)iBufferLengthMS;
                                        SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 2, 0, RTMPHead.enumTypeID.UserControlMsg, 0, BufferLengthBody));
                                    }

                                    iTCP.SendData(SendContentArray.ToArray());
                                    SendContentArray.Clear();

                                    iConnectState = enumRTMPConnectState.Play;
                                }

                                break;
                            case enumRTMPConnectState.Publish:
                                {
                                    if (iAVConfigSet)
                                    {
                                        SendDataFrame();
                                        iConnectState = enumRTMPConnectState.BeginPublish;
                                    }
                                }

                                break;
                            default:
                                {
                                    if ((iTotalBodyBytes - iLastACKValue) >= (iServerWindowAckSize / 2))
                                    {
                                        if (iLastACKValue == 0)
                                            iTCP.SendData(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 2, 0, RTMPHead.enumTypeID.Ack, 0, new RTMPBodyAcknowledgement() { SequenceNumber = (uint)iTotalBodyBytes }));
                                        else
                                            iTCP.SendData(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 2, 0, RTMPHead.enumTypeID.Ack, 0, new RTMPBodyAcknowledgement() { SequenceNumber = (uint)iTotalBodyBytes }));

                                        iLastACKValue = iTotalBodyBytes;
                                    }

                                }

                                break;
                        }
                    }

                    break;
            }
        }

        private void SendDataFrame()
        {
            AMFCommand.setDataFrame DataFrame = new AMFCommand.setDataFrame();
            RTMPBodyVideoData RTMPFirstVideoBody = null;
            RTMPBodyAudioData RTMPFirstAudioBody = null;

            DataFrame.Information.AddToProperties("title", new AMF0Objects.AMF0String() { Value = "RTMPLibrary" });
            DataFrame.Information.AddToProperties("encoder", new AMF0Objects.AMF0String() { Value = "" });

            if (iEnableVideoTrack)
            {
                if ((H264SPSConfig != null) && (H264PPSConfig != null))
                {
                    SPSParsing Parsing = new SPSParsing();
                    SPSParsing.SeqParameterSet SPS = null;
                    int iVideoWidth = 1024;
                    int iVideoHeight = 768;

                    try { SPS = Parsing.seq_parameter_set_rbsp(H264SPSConfig); }
                    catch (Exception ex) { }

                    if (SPS != null)
                    {
                        iVideoWidth = SPS.Width();
                        iVideoHeight = SPS.Height();
                    }

                    DataFrame.Information.AddToProperties("videocodecid", new AMF0Objects.AMF0Number() { Value = 7 });
                    DataFrame.Information.AddToProperties("width", new AMF0Objects.AMF0Number() { Value = iVideoWidth });
                    DataFrame.Information.AddToProperties("height", new AMF0Objects.AMF0Number() { Value = iVideoHeight });
                    DataFrame.Information.AddToProperties("videodatarate", new AMF0Objects.AMF0Number() { Value = 0 });
                }
            }

            if (iEnableAudioTrack)
            {
                if (AACConfig != null)
                {
                    AACConfigDecoder ACD = new AACConfigDecoder();

                    ACD.AACConfigDecode(AACConfig);

                    DataFrame.Information.AddToProperties("audiocodecid", new AMF0Objects.AMF0Number() { Value = 10 });
                    DataFrame.Information.AddToProperties("audiodatarate", new AMF0Objects.AMF0Number() { Value = 0 });
                    DataFrame.Information.AddToProperties("audiosamplerate", new AMF0Objects.AMF0Number() { Value = ACD.sampleFrequency });
                    DataFrame.Information.AddToProperties("audiosamplesize", new AMF0Objects.AMF0Number() { Value = 16 });
                    DataFrame.Information.AddToProperties("stereo", new AMF0Objects.AMF0Boolean() { Value = true });
                }
            }


            SendRTMPBody(RTMPHead.enumFmtType.Type0, 4, 0, RTMPHead.enumTypeID.AMF0Data, iPlayStreamID, DataFrame.GetBody);
            if (iEnableVideoTrack)
            {
                if (iVideoFirstFrameRaised == false)
                {
                    if ((H264SPSConfig != null) && (H264PPSConfig != null))
                    {
                        if (RTMPFirstVideoBody != null)
                        {
                            // 第一次傳送 KeyFrame
                            // 包含 RTMP Video Header
                            RTMPFirstVideoBody = RTMPBodyVideoData.ImportSPS(H264SPSConfig, H264PPSConfig);

                            SendRTMPBody(RTMPHead.enumFmtType.Type0, 7, 0, RTMPHead.enumTypeID.VideoData, iPlayStreamID, RTMPFirstVideoBody);
                            iVideoFirstFrameRaised = true;
                        }
                    }
                }
            }

            if (iEnableAudioTrack)
            {
                if (iAudioFirstFrameRaised == false)
                {
                    if (AACConfig != null)
                    {
                        byte[] AudioConfigData;

                        AudioConfigData = (byte[])Array.CreateInstance(typeof(byte), AACConfig.Length + 1);
                        AudioConfigData[0] = 0;
                        Array.Copy(AACConfig, 0, AudioConfigData, 1, AACConfig.Length);

                        // 第一次傳送 KeyFrame
                        // 包含 RTMP Video Header
                        RTMPFirstAudioBody = new RTMPBodyAudioData();
                        RTMPFirstAudioBody.AudioData = AudioConfigData;

                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 4, 0, RTMPHead.enumTypeID.AudioData, iPlayStreamID, RTMPFirstAudioBody);
                        iAudioFirstFrameRaised = true;
                    }
                }
            }
        }

        private void iTCP_Disconnect(TCPSocket sender)
        {
            Disconnect?.Invoke(this);
        }

        #region IDisposable Support
        private bool disposedValue;

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
