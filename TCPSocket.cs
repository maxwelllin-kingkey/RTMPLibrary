using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace RTMPLibrary
{
    internal partial class TCPSocket : IDisposable
    {
        private System.Net.Sockets.Socket iSocket;
        private object iTag;
        private int iRecvBufferSize = 8192;
        public byte[] ReceiveBuffer = null;
        private int iSendBufferSize;
        private byte[] iSendBuffer = null;
        private UltimateByteArrayClass iThisPartSend = new UltimateByteArrayClass();  // List(Of Byte)
        private bool iInReceiveLoop = false;
        private bool isClosed = true;
        private bool iSelfClose = false;
        private bool iIsSending = false;
        private Action iConnectedAsyncCB = null;
        private bool iRawMode = false;
        private bool iBlockForSend = false;
        private bool iIsSSL = false;
        private System.Security.Cryptography.X509Certificates.X509Certificate2 sslCert;
        private Stream stm = null;
        private System.Net.Sockets.NetworkStream innerStm = null;

        public event DataReceivedEventHandler DataReceived;

        public delegate void DataReceivedEventHandler(TCPSocket sender, int recvCount);

        public event ConnectedEventHandler Connected;

        public delegate void ConnectedEventHandler(TCPSocket sender);

        public event DisconnectEventHandler Disconnect;

        public delegate void DisconnectEventHandler(TCPSocket sender);

        private ArrayList iSendSyncRoot = new ArrayList();

        public object Tag
        {
            get
            {
                return iTag;
            }

            set
            {
                iTag = value;
            }
        }

        public System.Net.Sockets.Socket SourceSocket
        {
            get
            {
                return iSocket;
            }
        }

        public bool RawMode
        {
            get
            {
                return iRawMode;
            }

            set
            {
                iRawMode = value;
            }
        }

        public int SendTimeout
        {
            get
            {
                if (iSocket != null)
                    return iSocket.SendTimeout;

                return default;
            }

            set
            {
                if (iSocket != null)
                    iSocket.SendTimeout = value;
            }
        }

        public int RecvTimeout
        {
            get
            {
                if (iSocket != null)
                    return iSocket.ReceiveTimeout;

                return default;
            }

            set
            {
                if (iSocket != null)
                    iSocket.ReceiveTimeout = value;
            }
        }

        public void ConnectAsync(string RemoteIP, int RemotePort, Action callback)
        {
            System.Net.IPAddress IPAddr = null;

            if (System.Net.IPAddress.TryParse(RemoteIP, out IPAddr) == false)
            {
                System.Net.IPAddress[] AddrList;

                AddrList = System.Net.Dns.GetHostAddresses(RemoteIP);
                if (AddrList != null)
                {
                    if (AddrList.Length > 0)
                    {
                        IPAddr = AddrList[0];
                    }
                }
            }

            iConnectedAsyncCB = callback;
            if (IPAddr != null)
            {
                iSocket.BeginConnect(new System.Net.IPEndPoint(IPAddr, RemotePort), (ar) =>
                {
                    bool ConnectSuccess = false;

                    try
                    {
                        iSocket.EndConnect(ar);
                        ConnectSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        CloseSocket();
                    }

                    if (ConnectSuccess)
                    {
                        if (iSocket.Connected)
                        {
                            Connected.Invoke(this);
                        }
                        else
                        {
                            isClosed = true;
                            Disconnect.Invoke(this);
                        }
                    }
                    else
                    {
                        isClosed = true;
                        Disconnect.Invoke(this);
                    }

                    if (iConnectedAsyncCB != null)
                    {
                        iConnectedAsyncCB.Invoke();
                    }
                }, null);
            }
            else
            {
                iSocket.BeginConnect(RemoteIP, RemotePort, (ar) =>
                {
                    bool ConnectSuccess = false;

                    try
                    {
                        iSocket.EndConnect(ar);
                        ConnectSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        CloseSocket();
                    }

                    if (ConnectSuccess)
                    {
                        if (iSocket.Connected)
                        {
                            Connected.Invoke(this);
                        }
                        else
                        {
                            isClosed = true;
                            Disconnect.Invoke(this);
                        }
                    }
                    else
                    {
                        isClosed = true;
                        Disconnect.Invoke(this);
                    }

                    if (iConnectedAsyncCB != null)
                    {
                        iConnectedAsyncCB.Invoke();
                    }
                }, null);
            }
        }

        public void Connect(string RemoteIP, int RemotePort, int ConnectTimeoutMS = 10000)
        {
            System.Net.IPAddress IPAddr = null;

            if (System.Net.IPAddress.TryParse(RemoteIP, out IPAddr) == false)
            {
                System.Net.IPAddress[] AddrList;

                AddrList = System.Net.Dns.GetHostAddresses(RemoteIP);
                if (AddrList != null)
                {
                    if (AddrList.Length > 0)
                    {
                        IPAddr = AddrList[0];
                    }
                }
            }

            try
            {
                IAsyncResult iResult;

                Console.WriteLine("Connect to " + IPAddr.ToString() + ":" + RemotePort);
                if (IPAddr != null)
                {
                    iResult = iSocket.BeginConnect(new System.Net.IPEndPoint(IPAddr, RemotePort), null, null);
                }
                // iSocket.Connect(New System.Net.IPEndPoint(IPAddr, RemotePort))
                else
                {
                    iResult = iSocket.BeginConnect(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(RemoteIP), RemotePort), null, null);
                    // iSocket.Connect(RemoteIP, RemotePort)
                }

                if (iResult != null)
                {
                    bool success = iResult.AsyncWaitHandle.WaitOne(ConnectTimeoutMS, true);

                    if (iSocket.Connected)
                    {
                        Console.WriteLine("[" + RemoteIP + ":" + RemotePort + "] Connected");
                        iSocket.EndConnect(iResult);
                    }
                    else
                    {
                        throw new Exception("Connect to remote [" + RemoteIP + ":" + RemotePort + "] failure");
                    }
                }

                isClosed = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCPSocket Connect failure:" + ex.Message);
                CloseSocket();
                throw ex;
            }

            if (isClosed == false)
            {
                PrepareNetworkStream();
                Connected.Invoke(this);
            }
            else
            {
                Disconnect.Invoke(this);
            }
        }

        private void Socket_EndConnect(IAsyncResult ar)
        {
            bool ConnectSuccess = false;

            try
            {
                iSocket.EndConnect(ar);
                ConnectSuccess = true;
            }
            catch (Exception ex)
            {
                CloseSocket();
            }

            if (ConnectSuccess)
            {
                if (iSocket.Connected)
                {
                    Connected.Invoke(this);
                }
                else
                {
                    isClosed = true;
                    Disconnect.Invoke(this);
                }
            }
            else
            {
                isClosed = true;
                Disconnect.Invoke(this);
            }
        }

        public void SendData(byte[] DataBuffer)
        {
            SendData(DataBuffer, 0, DataBuffer.Length);
        }

        public void SendData(byte[] DataBuffer, int OffsetIndex, int Length)
        {
            try { stm.Write(DataBuffer, OffsetIndex, Length); }
            catch (Exception ex) { CloseSocket(); }
        }

        private void ContinueSend(bool IsLastContinueSendCall = false)
        {
            var SendBytes = default(int);
            bool IsMySend = false;
            lock (iSendSyncRoot)
            {
                if ((iSocket != null) && ((iIsSending == false) || (IsLastContinueSendCall == true)))
                {
                    // Console.WriteLine("Set sending = true")
                    iIsSending = true;
                    IsMySend = true;
                    if ((iThisPartSend.Count > 0) && (iSendBuffer != null))
                    {
                        if (iThisPartSend.Count > iSendBufferSize)
                        {
                            SendBytes = iSendBufferSize;
                            iThisPartSend.CopyTo(0, iSendBuffer, 0, SendBytes);
                            iThisPartSend.RemoveRange(0, SendBytes);
                        }
                        else
                        {
                            SendBytes = iThisPartSend.Count;
                            iThisPartSend.CopyTo(0, iSendBuffer, 0, iThisPartSend.Count);
                            iThisPartSend.Clear();
                        }
                    }
                }
            }

            if (IsMySend)
            {
                // Console.WriteLine("Send_Continue, Buffer count:" & iThisPartSend.Count & ", send bytes:" & SendBytes)

                if (SendBytes > 0)
                {
                    // Console.WriteLine("send bytes:" & SendBytes)

                    try
                    {
                        stm.BeginWrite(iSendBuffer, 0, SendBytes, new AsyncCallback(iSocket_EndSend), null);
                    }
                    catch (System.Net.Sockets.SocketException SockEx)
                    {
                        if (SockEx.SocketErrorCode == System.Net.Sockets.SocketError.WouldBlock)
                        {
                            Console.WriteLine("Socket would block (Send)");
                        }
                        else
                        {
                            Console.WriteLine("Send error");
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else if (iIsSending)
                {
                    // Console.WriteLine("Set sending = false")
                    iIsSending = false;
                }
                // Else
                // Console.WriteLine("send data denied")
            }
        }

        public void StartReceive()
        {
            lock (iSocket)
            {
                if (iInReceiveLoop == false)
                {
                    iInReceiveLoop = true;
                    ReceiveBuffer = (byte[])Array.CreateInstance(typeof(byte), iRecvBufferSize + 100);
                    BeginReceive();
                }
            }
        }

        public void Bind()
        {
            Bind(System.Net.IPAddress.Any);
        }

        public void Bind(string IPString)
        {
            System.Net.IPAddress IPAddr;

            IPAddr = System.Net.IPAddress.Parse(IPString);
            Bind(IPAddr);
        }

        public void Bind(System.Net.IPAddress IPAddress)
        {
            var byteTrue = new byte[] { 1, 0, 0, 0 };
            var byteOut = new byte[] { 0, 0, 0, 0 };

            iSocket.Bind(new System.Net.IPEndPoint(IPAddress, 0));
            if (iRawMode)
            {
                iSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.HeaderIncluded, true);
                iSocket.IOControl(System.Net.Sockets.IOControlCode.ReceiveAll, byteTrue, byteOut);
            }
        }

        public void Close()
        {
            iSelfClose = true;
            CloseSocket();
        }

        private void CloseSocket()
        {
            bool RaiseDisconnect = false;

            // SyncLock iSendSyncRoot
            iInReceiveLoop = false;
            isClosed = true;
            iRawMode = false;
            ReceiveBuffer = null;

            if (iSocket != null)
            {
                try { iSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both); }
                catch (Exception ex) { }

                try { iSocket.Disconnect(true); }
                catch (Exception ex) { Console.WriteLine("Close Error:" + ex.Message); }

                try { iSocket.Close(); }
                catch (Exception ex) { }

                iSocket = null;
                iSendBuffer = null;
                iThisPartSend.Clear();
                if (iSelfClose == false)
                {
                    RaiseDisconnect = true;
                }
            }

            if (stm != null)
            {
                try { stm.Close(); }
                catch (Exception ex) { }

                try { stm.Dispose(); }
                catch (Exception ex) { }

                stm = null;
            }

            if (innerStm != null)
            {
                try { innerStm.Close(); }
                catch (Exception ex) { }

                try { innerStm.Dispose(); }
                catch (Exception ex) { }

                innerStm = null;
            }
            // End SyncLock

            if (RaiseDisconnect)
            {
                Disconnect.Invoke(this);
            }
        }

        private void PrepareNetworkStream()
        {
            if (stm == null)
            {
                var NS = new System.Net.Sockets.NetworkStream(iSocket, true);
                System.Net.Security.SslStream ssl;

                innerStm = NS;
                if (iIsSSL)
                {
                    iSocket.ReceiveTimeout = 5000;
                    // sometime hangup
                    try
                    {
                        ssl = new System.Net.Security.SslStream(NS, false);
                        ssl.AuthenticateAsServer(sslCert, false, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls, false);
                        stm = ssl;
                    }
                    catch (Exception ex)
                    {
                        // Console.WriteLine("SSL connection exception:" & ex.ToString)
                        // LogToFile(iLogPath, ex.ToString)
                        CloseSocket();
                        throw ex;
                    }
                }
                else
                {
                    stm = NS;
                }
            }
        }

        private void Init()
        {
            iSocket.SendTimeout = -1;
            // iSocket.LingerState = New System.Net.Sockets.LingerOption(True, 30)
            iSocket.NoDelay = true;
            iSocket.UseOnlyOverlappedIO = true;
            iRecvBufferSize = (int)iSocket.GetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReceiveBuffer);
            iSendBufferSize = (int)iSocket.GetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.SendBuffer);

            // iSocket.SetSocketOption(Net.Sockets.SocketOptionLevel.Socket, Net.Sockets.SocketOptionName.DontLinger, False)
            iSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.Linger, new System.Net.Sockets.LingerOption(true, 30));

            // ReceiveBuffer = Array.CreateInstance(GetType(Byte), iRecvBufferSize)
            iSendBuffer = (byte[])Array.CreateInstance(typeof(byte), iSendBufferSize);
            if (iSocket.Connected)
            {
                PrepareNetworkStream();
                isClosed = false;
            }
            else
            {
                isClosed = true;
            }
        }

        private void BeginReceive()
        {
            if (isClosed == false)
            {
                try { stm.BeginRead(ReceiveBuffer, 0, iRecvBufferSize, new AsyncCallback(iSocket_EndReceive), null); }
                catch (Exception ex) { CloseSocket(); }
            }
        }

        private void iSocket_EndReceive(IAsyncResult ar)
        {
            var iRecvSize = default(int);

            if (stm != null)
            {
                try { iRecvSize = stm.EndRead(ar); }
                catch (Exception ex) { }
            }

            if (iRecvSize > 0)
            {
                DataReceived?.Invoke(this, iRecvSize);
                BeginReceive();
            }
            else
            {
                CloseSocket();
            }
        }

        private void iSocket_EndSend(IAsyncResult ar)
        {
            if (stm != null)
            {
                try { stm.EndWrite(ar); }
                catch (Exception ex) { }

                try { ContinueSend(true); }
                catch (Exception ex) { CloseSocket(); }
            }
            else
            {
                CloseSocket();
            }
        }

        public TCPSocket()
        {
            iRawMode = false;
            CreateSocketInstance(null);
        }

        public TCPSocket(System.Net.Sockets.Socket SetSocket)
        {
            iRawMode = false;
            CreateSocketInstance(SetSocket);
        }

        public TCPSocket(bool createInRawMode)
        {
            iRawMode = true;
            CreateSocketInstance(null);
        }

        public TCPSocket(System.Net.Sockets.Socket ExistSocket, System.Security.Cryptography.X509Certificates.X509Certificate2 cert)
        {
            sslCert = cert;
            iIsSSL = true;
            CreateSocketInstance(ExistSocket);
        }

        private void CreateSocketInstance(System.Net.Sockets.Socket ExistSocket)
        {
            if (ExistSocket == null)
            {
                if (iRawMode)
                {
                    iSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Raw, System.Net.Sockets.ProtocolType.IP);
                }
                else
                {
                    iSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                }
            }
            else
            {
                iSocket = ExistSocket;
            }

            Init();
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
                    try { Close(); }
                    catch (Exception ex) { }
                }
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TCPSocket()
        {
            Dispose();
        }
        #endregion
    }
}
