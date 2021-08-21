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
        private int iFmtLastVideoTimestamp = 0;
        private int iFmtTimestamp = 0;
        private int iLastVideoTimestamp = 0;
        private object iLastMsg;
        private int iPlayStreamID = 1;
        private enumRTMPHandshakeState iHandshakeState;
        private enumRTMPConnectState iConnectState;
        private RTMPHandshake iHandshake;
        private int iUCMStreamID = -1;
        private byte[] iLastPacket = null;
        private RTMP iRTMP = new RTMP();
        private bool iBeginStreamReceived = false;
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

        public delegate void VideoDataEventHandler(RTMPClient sender, uint TimeDelta, RTMPBodyVideoData VD);
        public event VideoDataEventHandler VideoData;

        public delegate void DisconnectEventHandler(RTMPClient sender);
        public event DisconnectEventHandler Disconnect;

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
            BeginStream
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

        public void RequestRTMP(string URL)
        {
            Uri URI = new Uri(URL);
            bool IsConnected = false;

            iUrl = URL;
            iServerName = URI.Host;
            iUCMStreamID = -1;
            iTotalBodyBytes = 0L;
            iLastACKValue = 0L;
            iFmtTimestamp = 0;
            iFmtLastVideoTimestamp = 0;
            iLastVideoTimestamp = 0;

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
            string appName;
            string iTcUrl = tcUrl;

            // iHandshakeState = enumRTMPHandshakeState.HandshakeC0

            // 取得第一個路徑做為 app
            if (iPath.Substring(0, 1) == "/")
                PathArray = iPath.Substring(1).Split("/");
            else
                PathArray = iPath.Split("/");

            appName = PathArray[0];

            // 重組 tcUrl
            if (string.IsNullOrEmpty(iTcUrl))
            {
                iTcUrl = "rtmp://" + iServerName + ":" + iPort;
                if (PathArray.Length >= 2)
                {
                    for (int I = 0, loopTo = PathArray.Length - 2; I <= loopTo; I++)
                        iTcUrl = iTcUrl + "/" + PathArray[I];
                }
            }

            RTMPConnect.CommandName.Value = "connect";
            RTMPConnect.TransactionID.Value = 1;
            RTMPConnect.CommandObject.SetValue("app", appName);
            RTMPConnect.CommandObject.SetValue("flashVer", "LNX " + iVersion);
            RTMPConnect.CommandObject.SetValue("tcUrl", iTcUrl);
            RTMPConnect.CommandObject.SetValue("swfUrl", swfUrl);
            RTMPConnect.CommandObject.SetValue("pageUrl", pageUrl);
            RTMPConnect.CommandObject.SetValue("fpad", false);
            RTMPConnect.CommandObject.SetValue("capabilities", 15);
            RTMPConnect.CommandObject.SetValue("audioCodecs", 4071);
            RTMPConnect.CommandObject.SetValue("videoCodecs", 252);
            RTMPConnect.CommandObject.SetValue("videoFunction", 1);

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
            var RTMP = new RTMP(FmtType, TypeID, Body);
            RTMP.Head.ChunkStreamID = ChunkStreamID;
            RTMP.Head.Timestamp = (uint)Timestamp;
            RTMP.Head.StreamID = (uint)StreamID;
            return RTMP.ToByteArray(iChunkSize);
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

                        // 延遲 0.5s
                        System.Threading.Thread.Sleep(200);
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
                                RTMP ServerR = default;
                                try
                                {
                                    ServerR = iRTMP.ParsingFromArray(iBuffer.InternalBuffer, iBuffer.Count, iChunkSize);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Invalid packet:" + ex.Message);
                                    iBuffer.Clear();
                                    BufferProcessed = true;
                                    break;
                                }

                                // If ServerR.Head.TypeID = RTMPHead.enumTypeID.UserControlMsg And
                                // ServerR.Head.FmtType = RTMPHead.enumFmtType.Type0 And
                                // ServerR.IsFmtType2 = True Then Stop

                                if (ServerR != null)
                                {
                                    iFmtTimestamp += (int)ServerR.Head.Timestamp;
                                    // Console.WriteLine("0 FmtTimestamp:" & iFmtTimestamp & "  FmtLastVideoTimestamp:" & iFmtLastVideoTimestamp & "  [+" & ServerR.Head.Timestamp & "]")

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
                                                    bool HasVideoPacket = false;
                                                    long TotalDelayMS = 0L;
                                                    AggreMsg = (RTMPBodyAggregate)ServerR.Body;

                                                    // Console.WriteLine("Aggregate package:" & AggreMsg.GetList.Count)
                                                    foreach (RTMPBodyAggregate.AggregateMessage EachMsg in AggreMsg.GetList())
                                                    {
                                                        if (iLastVideoTimestamp == 0)
                                                            iLastVideoTimestamp = EachMsg.Timestamp;

                                                        // Console.WriteLine("TypeID [" & EachMsg.TypeID.ToString & "] Aggregate Timestamp:" & EachMsg.Timestamp & ", LastVideoTimestamp:" & iLastVideoTimestamp)

                                                        switch (EachMsg.TypeID)
                                                        {
                                                            case RTMPHead.enumTypeID.VideoData:
                                                                {
                                                                    RTMPBodyVideoData VideoBody = null;
                                                                    int DelayTimerMS;
                                                                    try
                                                                    {
                                                                        VideoBody = new RTMPBodyVideoData(EachMsg.Body, EachMsg.BodyOffset, EachMsg.BodyLength);
                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        Console.WriteLine("Aggregate invalid video:" + ex.Message);
                                                                    }

                                                                    if (EachMsg.Timestamp >= iLastVideoTimestamp)
                                                                    {
                                                                        DelayTimerMS = EachMsg.Timestamp - iLastVideoTimestamp;
                                                                    }
                                                                    else
                                                                    {
                                                                        DelayTimerMS = Math.Abs(0xFFFFFF - iLastVideoTimestamp + EachMsg.Timestamp);
                                                                        if (DelayTimerMS > 2000)
                                                                        {
                                                                            DelayTimerMS = 0;
                                                                        }
                                                                    }

                                                                    // Composition Time 等同 RTSP Timestamp (上一個封包的總合經過時間)
                                                                    // Time Delta 是每一個封包的時間差, 一個封包內可能包含許多 Video Data

                                                                    if (VideoBody != null)
                                                                    {
                                                                        VideoData.Invoke(this, (uint)DelayTimerMS, VideoBody);
                                                                    }

                                                                    // If DelayTimerMS < 25 Then Stop

                                                                    HasVideoPacket = true;
                                                                    TotalDelayMS += DelayTimerMS;
                                                                    iLastVideoTimestamp = EachMsg.Timestamp;

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

                                                    if (HasVideoPacket)
                                                    {
                                                        iFmtLastVideoTimestamp = (int)(iFmtTimestamp + TotalDelayMS);
                                                        // Console.WriteLine("1 FmtTimestamp:" & iFmtTimestamp & "  FmtLastVideoTimestamp:" & iFmtLastVideoTimestamp)
                                                    }

                                                    break;
                                                }
                                            case RTMPHead.enumTypeID.VideoData:
                                                {
                                                    RTMPBodyVideoData VideoBody = null;
                                                    uint TimeDelta = 0U; // ServerR.Head.Timestamp

                                                    // Console.WriteLine("2 FmtTimestamp:" & iFmtTimestamp & "  FmtLastVideoTimestamp:" & iFmtLastVideoTimestamp)

                                                    if (iFmtTimestamp >= iFmtLastVideoTimestamp)
                                                    {
                                                        TimeDelta = (uint)(iFmtTimestamp - iFmtLastVideoTimestamp);
                                                    }
                                                    else
                                                    {
                                                        TimeDelta = (uint)Math.Abs(0xFFFFFF - iFmtLastVideoTimestamp + iFmtTimestamp);
                                                        if (TimeDelta > 2000L)
                                                        {
                                                            TimeDelta = 0U;
                                                        }
                                                    }

                                                    if (iLastVideoTimestamp > 0)
                                                    {
                                                        iLastVideoTimestamp = (int)(iLastVideoTimestamp + TimeDelta);
                                                    }

                                                    iFmtLastVideoTimestamp = iFmtTimestamp;
                                                    try
                                                    {
                                                        VideoBody = (RTMPBodyVideoData)ServerR.Body;
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine("Video packet invalid");
                                                    }

                                                    if (VideoBody != null)
                                                    {
                                                        // Console.WriteLine("VideoData TimeDelta:" & TimeDelta & ", CompositionTime:" & VideoBody.CompositionTime)
                                                        // RaiseEvent VideoData(Me, TimeDelta, ServerR.Body)
                                                        VideoData?.Invoke(this, TimeDelta, VideoBody);
                                                        // RaiseEvent VideoData(Me, 0, ServerR.Body)
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
                                                        AMFCommand.onMetaData MetaBody = new AMFCommand.onMetaData((AMFCommand.AMFCommandBody)ServerR.Body);
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
                                                                HandshakeCompleted.Invoke(this);
                                                            }
                                                            else
                                                            {
                                                                HandshakeFail.Invoke(this);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            HandshakeFail.Invoke(this);
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
                                                            AMF0Objects.AMF0String AMFStringCode = null;

                                                            AMFStringCode = (AMF0Objects.AMF0String)PlayResult.Information.GetValue("code");
                                                            if (AMFStringCode != null)
                                                            {
                                                                if (AMFStringCode.Value.ToUpper().Contains("Play.Start".ToUpper()))
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
                                                                    HandshakeFail.Invoke(this);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                HandshakeFail.Invoke(this);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            HandshakeFail.Invoke(this);
                                                        }

                                                        break;
                                                }

                                                break;
                                            default:
                                                Console.WriteLine("Invalid ID");

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
                                    // 發送 Window ack
                                    RTMPBodyWindowSize WindowBody = new RTMPBodyWindowSize();

                                    WindowBody.WindowSize = (uint)iClientWindowAckSize;
                                    SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type0, 2, 0, RTMPHead.enumTypeID.SetWindowSize, 0, WindowBody));

                                    // 發送 CreateStream
                                    AMFCommand.CreateStream ConnectBody = null;

                                    ConnectBody = new AMFCommand.CreateStream(2);
                                    SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 3, 0, RTMPHead.enumTypeID.AMF0Command, 0, ConnectBody.GetBody));
                                    iTCP.SendData(SendContentArray.ToArray());
                                    SendContentArray.Clear();

                                    // ' 發送 SetBufferLength
                                    // Dim BufferLengthBody As New RTMPBodyUCM_SetBufferLength
                                    // BufferLengthBody.StreamID = 0
                                    // BufferLengthBody.BufferLength = iBufferLengthMS
                                    // SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 2, 0, RTMPHead.enumTypeID.UserControlMsg, 0, BufferLengthBody))

                                    // ' 發送 CheckBandwidth
                                    // Dim CheckBandwidthBody As New RTMPBodyAMFBase
                                    // CheckBandwidthBody.AMF0List.Add(New AMF0String With {.Value = "checkBandwidth"})
                                    // CheckBandwidthBody.AMF0List.Add(New AMF0Number With {.Value = 0})
                                    // CheckBandwidthBody.AMF0List.Add(New AMF0Null)
                                    // SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 3, 0, RTMPHead.enumTypeID.AMF3Command, 0, CheckBandwidthBody))

                                    // iTCP.SendData2(SendContentArray.ToArray)

                                    iConnectState = enumRTMPConnectState.CreateStream;  // 等待 Result

                                    break;
                                }
                            case enumRTMPConnectState.GetCreateStreamResult:
                                {
                                    // 發送 Play
                                    List<byte> SendContentArray = new List<byte>();
                                    int TmpIndex;
                                    string PlayStreamName;

                                    TmpIndex = iUrl.LastIndexOf("/");
                                    if (TmpIndex != -1)
                                        PlayStreamName = iUrl.Substring(TmpIndex + 1);
                                    else
                                        PlayStreamName = iUrl;

                                    AMFCommand.GetStreamLength StreamLenBody = new AMFCommand.GetStreamLength();
                                    StreamLenBody.StreamName.Value = PlayStreamName;
                                    StreamLenBody.TransactionID.Value = 3;
                                    SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type0, 8, 0, RTMPHead.enumTypeID.AMF0Command, 0, StreamLenBody.GetBody));

                                    AMFCommand.Play PlayBody = new AMFCommand.Play();
                                    PlayBody.StreamName.Value = PlayStreamName;
                                    PlayBody.TransactionID.Value = 4;
                                    SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type0, 8, 0, RTMPHead.enumTypeID.AMF0Command, iPlayStreamID, PlayBody.GetBody));

                                    UCM.SetBufferLength BufferLengthBody = new UCM.SetBufferLength();
                                    BufferLengthBody.StreamID = 1;
                                    BufferLengthBody.BufferLength = (uint)iBufferLengthMS;
                                    SendContentArray.AddRange(GetRTMPBodyArray(RTMPHead.enumFmtType.Type1, 2, 0, RTMPHead.enumTypeID.UserControlMsg, 0, BufferLengthBody));

                                    iTCP.SendData(SendContentArray.ToArray());

                                    iConnectState = enumRTMPConnectState.Play;

                                    break;
                                }

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

                                    break;
                                }
                        }
                    }

                    break;
            }
        }

        private void iTCP_Disconnect(TCPSocket sender)
        {
            Disconnect.Invoke(this);
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
