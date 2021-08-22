namespace RTMPLibrary
{
    public abstract partial class RTMPSocketContaxtInterface
    {
        public abstract byte[] InternalBuffer { get; }

        public abstract void SendData(byte[] DataBuffer, int OffsetIndex, int Length);
        public abstract void Close();
        public abstract System.Net.IPEndPoint RemoteEP();

        public abstract bool IsConnected { get; }

        public void RaiseFlushBuffer()
        {
            FlushBuffer?.Invoke(this);
        }

        public void RaiseDataReceived(int recvCount)
        {
            DataReceived?.Invoke(this, recvCount);
        }

        public void RaiseDisconnect()
        {
            Disconnect?.Invoke(this);
        }

        public delegate void FlushBufferEventHandler(RTMPSocketContaxtInterface sender);
        public event FlushBufferEventHandler FlushBuffer;

        public delegate void DataReceivedEventHandler(RTMPSocketContaxtInterface sender, int recvCount);
        public event DataReceivedEventHandler DataReceived;

        public delegate void DisconnectEventHandler(RTMPSocketContaxtInterface sender);
        public event DisconnectEventHandler Disconnect;
    }
}
