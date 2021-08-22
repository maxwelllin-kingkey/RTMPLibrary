using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RTMPLibrary
{
    public partial class RTMPServerSession : IDisposable
    {
        private RTMPSocketContaxtInterface iTCP;

        public enum enumVideoPrivateType
        {
            NAL_SPS,
            NAL_PPS
        }

        public enum enumVideoFrameType
        {
            KeyFrame,
            NonKeyFrame
        }

        public enum enumHandshakeState
        {
            NoConnection,
            S1S2,
            Completed
        }

        public enum enumConnectionState
        {
            None,
            Connect,
            Connect_Result,
            CreateStream,
            CreateStream_Result,
            Play,
            StreamBegin,
            Ready
        }

        private UltimateByteArrayClass iRecvBuffer = new UltimateByteArrayClass(1000000);
        private enumHandshakeState iHandshakeState;
        private enumConnectionState iConnectionState;
        private int iChunkSize = 128;
        private int iWindowAckSize = 5000000;
        private int iBufferLengthMS = 1000;
        private int iPeerBandwidth = 5000000;
        private string iServerVersion = "3,0,1,123";
        private int iObjectEncodingNumber = 0;
        private int iPlayStreamID = 1;
        private int iClientTransactionID = 1;
        private uint iLastVideoTimestamp;
        private DateTime iLastVideoSendDate = System.DateTime.Now;
        private float iVideoFPS = 0f; // 根據影像封包到達時間計算 Time Delta
        private int iFrameTime = 0; // 已經經過的 frame 時間(ms)(用來計算 Time Delta)
        private Queue<int> iFrameTimeQueue = new Queue<int>();
        private bool iVideoFirstKeyFrameRaised = false;
        private byte[] iSPSContent = null;
        private byte[] iPPSContent = null;
        private RTMPHandshake iHandshake;
        private RTMP iRTMP = new RTMP();


        public delegate void DisconnectEventHandler(RTMPServerSession sender);
        public event DisconnectEventHandler Disconnect;

        public delegate void HandshakeExceptionEventHandler(RTMPServerSession sender);
        public event HandshakeExceptionEventHandler HandshakeException;

        public delegate void RTMPCommandConnectEventHandler(RTMPServerSession sender, AMFCommand.Connect Body, string StreamName);
        public event RTMPCommandConnectEventHandler RTMPCommandConnect;

        public delegate void RTMPCommandCreateStreamEventHandler(RTMPServerSession sender, AMFCommand.CreateStream Body);
        public event RTMPCommandCreateStreamEventHandler RTMPCommandCreateStream;

        public delegate void RTMPCommandPlayEventHandler(RTMPServerSession sender, AMFCommand.Play Body, string PlayStreamName);
        public event RTMPCommandPlayEventHandler RTMPCommandPlay;

        public delegate void RTMPMessageEventHandler(RTMPServerSession sender, RTMPHead Head, RTMPBodyBase Body);
        public event RTMPMessageEventHandler RTMPMessage;

        public DateTime LastUpdateDate = System.DateTime.Now;
        public object Tag;

        public RTMPSocketContaxtInterface GetSourceSocket
        {
            get
            {
                return iTCP;
            }
        }

        public enumHandshakeState HandshakeState
        {
            get
            {
                return iHandshakeState;
            }
        }

        public enumConnectionState ConnectionState
        {
            get
            {
                return iConnectionState;
            }
        }

        public int ChunkSize
        {
            get
            {
                return iChunkSize;
            }

            set
            {
                iChunkSize = value;
            }
        }

        public int WindowAckSize
        {
            get
            {
                return iWindowAckSize;
            }

            set
            {
                iWindowAckSize = value;
            }
        }

        public int BufferLengthMS
        {
            get
            {
                return iBufferLengthMS;
            }

            set
            {
                iBufferLengthMS = value;
            }
        }

        public int PeerBandwidth
        {
            get
            {
                return iPeerBandwidth;
            }

            set
            {
                iPeerBandwidth = value;
            }
        }

        public float VideoFPS
        {
            get
            {
                return iVideoFPS;
            }

            set
            {
                iVideoFPS = value;
            }
        }

        public void SetVideoPrivateData(enumVideoPrivateType PrivateType, byte[] Content)
        {
            switch (PrivateType)
            {
                case enumVideoPrivateType.NAL_SPS:
                    iSPSContent = Content;

                    break;
                case enumVideoPrivateType.NAL_PPS:
                    iPPSContent = Content;

                    break;
            }

            CheckMetaData();
        }

        public void ResetFrameCount()
        {
            iFrameTimeQueue.Clear();
            iFrameTime = 0;
        }

        public void AddVideoData(string VideoName, enumVideoFrameType VideoFrameType, byte[] VideoData, int VideoOffset, int VideoLength, uint VideoArrivalTimestampMS)
        {
            if (iHandshakeState == enumHandshakeState.Completed)
            {
                if (iConnectionState == enumConnectionState.Ready)
                {
                    if (VideoData != null)
                    {
                        RTMPBodyVideoData VideoBody = default;

                        // 必須要已經傳送過 KeyFrame 才允許開始 Video 傳輸
                        if ((iVideoFirstKeyFrameRaised) || (VideoFrameType == enumVideoFrameType.KeyFrame))
                        {
                            int TimeDeltaMS = 0;
                            if (iVideoFPS == 0f)
                            {
                                if (VideoArrivalTimestampMS == 0)
                                {
                                    if (System.DateTime.Now >= iLastVideoSendDate)
                                        TimeDeltaMS = Convert.ToInt32(System.DateTime.Now.Subtract(iLastVideoSendDate).TotalMilliseconds);
                                }
                                else if (iLastVideoTimestamp > 0)
                                {
                                    if (VideoArrivalTimestampMS >= iLastVideoTimestamp)
                                        TimeDeltaMS = (int)(VideoArrivalTimestampMS - iLastVideoTimestamp);
                                    else
                                        TimeDeltaMS = (int)(uint.MaxValue - iLastVideoTimestamp + VideoArrivalTimestampMS);

                                    if (TimeDeltaMS > 5000)
                                        TimeDeltaMS = 0;
                                }

                                iLastVideoTimestamp = VideoArrivalTimestampMS;
                            }
                            else
                            {
                                if (iFrameTime >= 1000)
                                {
                                    for (int _Loop = 1; _Loop <= 100; _Loop++)
                                    {
                                        int FirstDelayTime = 0;

                                        if (iFrameTime < 1000)
                                            break;

                                        FirstDelayTime = iFrameTimeQueue.Dequeue();
                                        iFrameTime -= FirstDelayTime;
                                    }

                                    TimeDeltaMS = 1000 - iFrameTime;
                                }
                                else if (iVideoFPS > iFrameTimeQueue.Count)
                                {
                                    TimeDeltaMS = System.Convert.ToInt32((1000 - iFrameTime) / (iVideoFPS - iFrameTimeQueue.Count));
                                }
                                else
                                {
                                    TimeDeltaMS = System.Convert.ToInt32(1000 / iVideoFPS);
                                }

                                iFrameTimeQueue.Enqueue(TimeDeltaMS);
                                iFrameTime += TimeDeltaMS;
                            }

                            // Console.WriteLine("Transfer video:" & VideoFrameType.ToString & " - Time delta: " & LastTimeMS & " - " & VideoData.Length)

                            iLastVideoSendDate = System.DateTime.Now;
                            switch (VideoFrameType)
                            {
                                case enumVideoFrameType.KeyFrame:
                                        // Console.WriteLine("RTMP video frame type:" & VideoFrameType.ToString & ", FirstKey?" & iVideoFirstKeyFrameRaised.ToString & ", PPS?" & iPPSContent.Length & ", SPS?" & iSPSContent.Length)

                                        if (iVideoFirstKeyFrameRaised == false)
                                        {
                                            RTMPBodyVideoData RTMPFirstVideoBody = null;

                                            // 第一次傳送 KeyFrame
                                            // 包含 RTMP Video Header
                                            RTMPFirstVideoBody = RTMPBodyVideoData.ImportSPS(iSPSContent, iPPSContent);
                                            if (RTMPFirstVideoBody != null)
                                            {
                                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, RTMPFirstVideoBody);
                                                iVideoFirstKeyFrameRaised = true;
                                            }
                                        }

                                        // Console.WriteLine("Video frame:" & VideoData(VideoOffset + 0) & ":" & VideoData(VideoOffset + 1) & ":" & VideoData(VideoOffset + 2) & ":" & VideoData(VideoOffset + 3))
                                        VideoBody = RTMPBodyVideoData.ImportVideoRaw(RTMPBodyVideoData.enumFrameType.KeyFrame, VideoData, VideoOffset, VideoLength);
                                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, VideoBody);

                                        break;
                                case enumVideoFrameType.NonKeyFrame:
                                        VideoBody = RTMPBodyVideoData.ImportVideoRaw(RTMPBodyVideoData.enumFrameType.NonKeyFrame, VideoData, VideoOffset, VideoLength);
                                        SendRTMPBody(RTMPHead.enumFmtType.Type1, 7, TimeDeltaMS, RTMPHead.enumTypeID.VideoData, iPlayStreamID, VideoBody);

                                        break;
                            }
                        }
                    }
                }
            }
        }

        public RTMPServerSession(RTMPSocketContaxtInterface TCP)
        {
            ResetStatus();

            LastUpdateDate = System.DateTime.Now;

            iTCP = TCP;
            iTCP.DataReceived += iTCP_DataReceived;
            iTCP.Disconnect += iTCP_Disconnect;
            iTCP.FlushBuffer += iTCP_FlushBuffer;

            ProcessRecvBuffer();
        }

        public void Close()
        {
            ResetStatus();
            if (iTCP != null)
            {
                LastUpdateDate = System.DateTime.Now;
                iTCP.Close();

                iTCP.DataReceived -= iTCP_DataReceived;
                iTCP.Disconnect -= iTCP_Disconnect;
                iTCP.FlushBuffer -= iTCP_FlushBuffer;
            }

            iTCP = null;
        }

        public System.Net.IPEndPoint GetRemoteEP()
        {
            if (iTCP != null) 
                return iTCP.RemoteEP();
            else
                return null;
        }

        private void ResetStatus()
        {
            iVideoFirstKeyFrameRaised = false;
            iClientTransactionID = 1;
            iSPSContent = null;
            iPPSContent = null;
            iHandshakeState = enumHandshakeState.NoConnection;
            iConnectionState = enumConnectionState.None;
            iRecvBuffer.Clear();
        }

        private void CheckMetaData()
        {
            if ((iHandshakeState == enumHandshakeState.Completed) && (iConnectionState == enumConnectionState.StreamBegin))
            {
                if ((iSPSContent != null) && (iPPSContent != null))
                {
                    SPSParsing Parsing = new SPSParsing();
                    SPSParsing.SeqParameterSet SPS = null;
                    int iVideoWidth = 1024;
                    int iVideoHeight = 768;
                    AMFCommand.onMetaData MetaBody = new AMFCommand.onMetaData();

                    try { SPS = Parsing.seq_parameter_set_rbsp(iSPSContent); }
                    catch (Exception ex) { }

                    if (SPS != null)
                    {
                        //iVideoWidth = (SPS.pic_width_in_mbs_minus1 + 1) * 16;
                        //iVideoHeight = (SPS.pic_height_in_map_units_minus1 + 1) * 16;
                        iVideoWidth = SPS.Width();
                        iVideoHeight = SPS.Height();
                    }

                    MetaBody.Information.SetValue("Server", "TopNVR RTMP Library");
                    MetaBody.Information.SetValue("width", iVideoWidth);
                    MetaBody.Information.SetValue("height", iVideoHeight);
                    MetaBody.Information.SetValue("displayWidth", iVideoWidth);
                    MetaBody.Information.SetValue("displayHeight", iVideoHeight);
                    MetaBody.Information.SetValue("duration", 0);
                    MetaBody.Information.SetValue("framerate", iVideoFPS);
                    MetaBody.Information.SetValue("fps", iVideoFPS);
                    MetaBody.Information.SetValue("videodatarate", 100);
                    MetaBody.Information.SetValue("videocodecid", 0);
                    MetaBody.Information.SetValue("audiodatarate", 0);
                    MetaBody.Information.SetValue("audiocodecid", 16);
                    MetaBody.Information.SetValue("profile", "");
                    MetaBody.Information.SetValue("level", "");

                    SendRTMPBody(RTMPHead.enumFmtType.Type0, 5, 0, RTMPHead.enumTypeID.AMF0Data, iPlayStreamID, MetaBody.GetBody);
                    iConnectionState = enumConnectionState.Ready;
                }
            }
        }

        private void SendRTMPBody(RTMPHead.enumFmtType FmtType, int ChunkStreamID, int Timestamp, RTMPHead.enumTypeID TypeID, int StreamID, RTMPBodyBase Body)
        {
            SendData(GetRTMPBodyArray(FmtType, ChunkStreamID, Timestamp, TypeID, StreamID, Body));
        }

        private byte[] GetRTMPBodyArray(RTMPHead.enumFmtType FmtType, int ChunkStreamID, int Timestamp, RTMPHead.enumTypeID TypeID, int StreamID, RTMPBodyBase Body)
        {
            var RTMP = new RTMP(FmtType, TypeID, Body);

            RTMP.Head.ChunkStreamID = ChunkStreamID;
            RTMP.Head.Timestamp = (uint)Timestamp;
            RTMP.Head.StreamID = (uint)StreamID;

            return RTMP.ToByteArray(iChunkSize);
        }

        private void SendData(byte[] Data, int Index, int Length)
        {
            iTCP.SendData(Data, Index, Length);
            LastUpdateDate = System.DateTime.Now;
        }

        private void SendData(byte[] Data)
        {
            iTCP.SendData(Data, 0, Data.Length);
            LastUpdateDate = System.DateTime.Now;
        }

        private void ProcessRecvBuffer()
        {
            if (iRecvBuffer.Count > 0)
            {
                bool BufferProcess = false;
                int _chunkSize = iChunkSize;
                for (int _Loop = 1; _Loop <= 10000; _Loop++)
                {
                    if ((iRecvBuffer.Count > 0) && (iTCP != null))
                    {
                        if (iHandshakeState == enumHandshakeState.Completed)
                        {
                            int TotalCount = -1;
                            int _NeedPacketSize = 0;

                            TotalCount = iRTMP.IsDataAvailable(iRecvBuffer.InternalBuffer, iRecvBuffer.Count, _chunkSize, ref _NeedPacketSize);
                            if (TotalCount != -1)
                            {
                                RTMP ClientR = default;
                                ClientR = iRTMP.ParsingFromArray(iRecvBuffer.InternalBuffer, iRecvBuffer.Count, _chunkSize);

                                switch (ClientR.Head.TypeID)
                                {
                                    case RTMPHead.enumTypeID.AMF0Command:
                                    case RTMPHead.enumTypeID.AMF3Command:
                                        switch (iConnectionState)
                                        {
                                            case enumConnectionState.None:
                                                // 預期 Connect
                                                AMFCommand.Connect ConnectBody = new AMFCommand.Connect((AMFCommand.AMFCommandBody)ClientR.Body);

                                                if (ConnectBody.CommandName.Value.ToUpper() == "connect".ToUpper())
                                                {
                                                    AMF0Objects.AMF0String StreamName = null;
                                                    AMF0Objects.AMF0Number objectEncoding = null;

                                                    iConnectionState = enumConnectionState.Connect;
                                                    iClientTransactionID = Convert.ToInt32(ConnectBody.TransactionID.Value);
                                                    StreamName = (AMF0Objects.AMF0String)ConnectBody.CommandObject.GetValue("app");
                                                    objectEncoding = (AMF0Objects.AMF0Number)ConnectBody.CommandObject.GetValue("objectEncoding");

                                                    if (objectEncoding != null)
                                                    {
                                                        iObjectEncodingNumber = Convert.ToInt32(objectEncoding.Value);
                                                    }

                                                    if (StreamName != null)
                                                    {
                                                        RTMPCommandConnect?.Invoke(this, ConnectBody, StreamName.Value);
                                                    }
                                                    else
                                                    {
                                                        RTMPCommandConnect?.Invoke(this, ConnectBody, string.Empty);
                                                    }
                                                    // Else
                                                    // RaiseEvent HandshakeException(Me)
                                                    // Close()

                                                    // Exit Do
                                                }

                                                break;
                                            case enumConnectionState.Connect_Result:
                                                // 預期 createStream
                                                AMFCommand.CreateStream CreateStreamBody = new AMFCommand.CreateStream((AMFCommand.AMFCommandBody)ClientR.Body);

                                                if (CreateStreamBody.CommandName.Value.ToUpper() == "createStream".ToUpper())
                                                {
                                                    iClientTransactionID = Convert.ToInt32(CreateStreamBody.TransactionID.Value);
                                                    iConnectionState = enumConnectionState.CreateStream;

                                                    RTMPCommandCreateStream?.Invoke(this, CreateStreamBody);
                                                }

                                                break;
                                            case enumConnectionState.CreateStream_Result:
                                                // 預期 play
                                                AMFCommand.AMFCommandBody BaseBody = (AMFCommand.AMFCommandBody)ClientR.Body;

                                                if (BaseBody.AMF0List.Count > 0)
                                                {
                                                    if (BaseBody.AMF0List[0].ObjectType == Common.enumAMF0ObjectType.String)
                                                    {
                                                        AMF0Objects.AMF0String AMFString = null;

                                                        AMFString = (AMF0Objects.AMF0String)BaseBody.AMF0List[0];
                                                        if (AMFString.Value == "play")
                                                        {
                                                            AMFCommand.Play PlayBody = new AMFCommand.Play((AMFCommand.AMFCommandBody)ClientR.Body);

                                                            iClientTransactionID = Convert.ToInt32(PlayBody.TransactionID.Value);
                                                            iConnectionState = enumConnectionState.Play;

                                                            if (PlayBody.StreamName != null)
                                                                RTMPCommandPlay?.Invoke(this, PlayBody, PlayBody.StreamName.Value);
                                                            else
                                                                RTMPCommandPlay?.Invoke(this, PlayBody, string.Empty);
                                                        }
                                                    }
                                                }

                                                break;
                                            case enumConnectionState.StreamBegin:
                                                break;
                                        }

                                        break;
                                    case RTMPHead.enumTypeID.AMF0Data:
                                        break;
                                    default:
                                        RTMPMessage?.Invoke(this, ClientR.Head, ClientR.Body);

                                        break;
                                }

                                iRecvBuffer.RemoveRange(0, TotalCount);
                            }
                            else
                            {
                                BufferProcess = true;
                                break;
                            }
                        }
                        else
                        {
                            // Handshake
                            switch (iHandshakeState)
                            {
                                case enumHandshakeState.NoConnection:
                                    // wait c0+c1 (1537)
                                    // 計算後傳送 S0S1S2
                                    if (iRecvBuffer.Count >= 1537)
                                    {
                                        if (this.iRecvBuffer[0] == 3)
                                        {
                                            iHandshakeState = enumHandshakeState.S1S2;
                                            SendData(CreateHandshake(Common.enumHandshakeType.S0S1S2, iRecvBuffer.ToArray()));
                                            iRecvBuffer.RemoveRange(0, 1537);
                                        }
                                        else
                                        {
                                            HandshakeException?.Invoke(this);
                                            Close();
                                            BufferProcess = true;

                                            break;
                                        }
                                    }
                                    else
                                    {
                                        BufferProcess = true;

                                        break;
                                    }

                                    break;
                                case enumHandshakeState.S1S2:
                                    // wait c2 (1536)
                                    if (iRecvBuffer.Count >= 1536)
                                    {
                                        iRecvBuffer.RemoveRange(0, 1536);
                                        iHandshakeState = enumHandshakeState.Completed;
                                        iConnectionState = enumConnectionState.None;
                                    }
                                    else
                                    {
                                        BufferProcess = true;

                                        break;
                                    }

                                    break;
                            }
                        }
                    }
                    else
                    {
                        BufferProcess = true;
                        break;
                    }
                }

                if (BufferProcess == false)
                {
                    if (iRecvBuffer.Count > 0)
                    {
                        iRecvBuffer.Clear();
                    }
                }
                else if (iTCP != null)
                {
                    if (iHandshakeState == enumHandshakeState.Completed)
                    {
                        switch (iConnectionState)
                        {
                            case enumConnectionState.Connect:
                                RTMPBodyWindowSize WindowBody = new RTMPBodyWindowSize();

                                WindowBody.WindowSize = (uint)iWindowAckSize;
                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 2, 0, RTMPHead.enumTypeID.SetWindowSize, 0, WindowBody);

                                RTMPBodyPeerBandwidth BWBody = new RTMPBodyPeerBandwidth();
                                BWBody.LimitType = 2;
                                BWBody.WindowSize = (uint)iPeerBandwidth;

                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 2, 0, RTMPHead.enumTypeID.SetPeerBandwidth, 0, BWBody);
                                iChunkSize = 4096;

                                RTMPBodyChunkSize ChunkBody = new RTMPBodyChunkSize();
                                ChunkBody.ChunkSize = (uint)iChunkSize;
                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 2, 0, RTMPHead.enumTypeID.SetChunkSize, 0, ChunkBody);

                                AMFCommand.ConnectResult ConnectResultBody = new AMFCommand.ConnectResult(AMFCommand.ConnectResult.enumResultType.Result);
                                ConnectResultBody.TransactionID.Value = iClientTransactionID;
                                ConnectResultBody.Properties.SetValue("fmsVer", "FMS/" + iServerVersion);
                                ConnectResultBody.Properties.SetValue("capabilities", 31);
                                ConnectResultBody.Information.SetValue("level", "status");
                                ConnectResultBody.Information.SetValue("code", "NetConnection.Connect.Success");
                                ConnectResultBody.Information.SetValue("description", "Connection succeeded.");
                                ConnectResultBody.Information.SetValue("objectEncoding", iObjectEncodingNumber);

                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 3, 0, RTMPHead.enumTypeID.AMF0Command, 0, ConnectResultBody.GetBody);
                                iConnectionState = enumConnectionState.Connect_Result;

                                break;
                            case enumConnectionState.CreateStream:
                                // 回應 RESULT
                                AMFCommand.CreateStreamResult CSResultBody = new AMFCommand.CreateStreamResult(AMFCommand.CreateStreamResult.enumResultType.Result);

                                CSResultBody.TransactionID.Value = iClientTransactionID;
                                CSResultBody.StreamID.Value = iPlayStreamID;

                                iConnectionState = enumConnectionState.CreateStream_Result;
                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 3, 0, RTMPHead.enumTypeID.AMF0Command, 0, CSResultBody.GetBody);

                                break;
                            case enumConnectionState.Play:
                                // 回應 Stream Begin
                                UCM.StreamBegin StreamBeginUCM = new UCM.StreamBegin();

                                StreamBeginUCM.StreamID = (uint)iPlayStreamID;

                                iConnectionState = enumConnectionState.StreamBegin;
                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 2, 0, RTMPHead.enumTypeID.UserControlMsg, 0, StreamBeginUCM);

                                // 傳送 Play.Start
                                AMFCommand.onStatusResult StatusBody = new AMFCommand.onStatusResult("NetStream.Play.Start");

                                StatusBody.Information.SetValue("level", "status");
                                StatusBody.Information.SetValue("description", "Start live");

                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 5, 0, RTMPHead.enumTypeID.AMF0Command, 1, StatusBody.GetBody);

                                AMFCommand.AMFCommandBody RtmpSampleAccessBody = new AMFCommand.AMFCommandBody();
                                RtmpSampleAccessBody.AMF0List.Add(new AMF0Objects.AMF0String() { Value = "|RtmpSampleAccess" });
                                RtmpSampleAccessBody.AMF0List.Add(new AMF0Objects.AMF0Boolean() { Value = true });
                                RtmpSampleAccessBody.AMF0List.Add(new AMF0Objects.AMF0Boolean() { Value = true });

                                SendRTMPBody(RTMPHead.enumFmtType.Type0, 5, 0, RTMPHead.enumTypeID.AMF0Data, 1, RtmpSampleAccessBody);

                                // 檢查是否允許傳送 MetaData
                                CheckMetaData();

                                break;
                                // 到此結束, 等待第一組封包後產生 onStatus
                        }
                    }
                }
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
                        RTMPHandshake.PackageValidate C1PV = default;
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
                                    RetValue = iHandshake.CreateC2(S1PV);
                                else
                                    throw new Exception("S1 not valid");
                            }
                        }

                        break;
                    }
            }

            return RetValue;
        }

        private void iTCP_DataReceived(RTMPSocketContaxtInterface sender, int recvCount)
        {
            if (recvCount > 0)
            {
                try { iRecvBuffer.AddRange(sender.InternalBuffer, 0, recvCount); }
                catch (Exception ex) { }
            }

            try
            {
                ProcessRecvBuffer();
                LastUpdateDate = System.DateTime.Now;
            }
            catch (Exception ex)
            {
            }
        }

        private void iTCP_Disconnect(RTMPSocketContaxtInterface sender)
        {
            LastUpdateDate = System.DateTime.Now;
            Disconnect?.Invoke(this);
        }

        private void iTCP_FlushBuffer(RTMPSocketContaxtInterface sender)
        {
            LastUpdateDate = System.DateTime.Now;
            if (iRecvBuffer != null)
                iRecvBuffer.Clear();
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
                    if (iTCP != null)
                    {
                        try { iTCP.Close(); }
                        catch (Exception ex) { }

                        iTCP = null;
                    }

                    iSPSContent = null;
                    iPPSContent = null;
                    iHandshake = null;
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