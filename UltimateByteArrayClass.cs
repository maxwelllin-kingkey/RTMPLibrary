using System;
using System.Collections.Generic;

namespace RTMPLibrary
{
    internal class UltimateByteArrayClass : IEnumerable<byte>
    {
        private int iBlock_Size = 32000; // 每次增加 32k
        private int iBufferMax = -1; // 最大可配置空間
        private int iCurrentSize = 0;  // 目前陣列大小
        protected internal byte[] iInternalArray = (byte[])Array.CreateInstance(typeof(byte), 0);

        public byte[] InternalBuffer
        {
            get
            {
                return iInternalArray;
            }
        }

        public int Count
        {
            get
            {
                return iCurrentSize;
            }
            set
            {
                // If value < 0 Then
                // Throw New Exception("value < 0:" & value)
                // End If
                iCurrentSize = Math.Min(Math.Max(value, 0), iBufferMax);
            }
        }

        /// <summary>
        ///     ''' Don't use, very slow, please change code to access InternalBuffer
        ///     ''' </summary>
        ///     ''' <param name="Index"></param>
        ///     ''' <value></value>
        ///     ''' <returns></returns>
        ///     ''' <remarks></remarks>
        public byte this[int Index]
        {
            get
            {
                if ((Index >= 0) & (Index < iCurrentSize))
                    return iInternalArray[Index];
                else
                    throw new IndexOutOfRangeException();
            }
            set
            {
                if ((Index >= 0) & (Index < iCurrentSize))
                    iInternalArray[Index] = value;
                else
                    throw new IndexOutOfRangeException();
            }
        }

        public void AddRange(UltimateByteArrayClass B)
        {
            this.AddRange(B.iInternalArray, 0, B.Count);
        }

        public void AddRange(UltimateByteArrayClass B, int offsetIndex, int Length)
        {
            this.AddRange(B.iInternalArray, offsetIndex, Length);
        }

        public void AddRange(byte[] B)
        {
            this.AddRange(B, 0, B.Length);
        }

        public void AddRange(byte[] B, int index, int length)
        {
            if (length > 0)
            {
                lock (iInternalArray)
                {
                    CheckInternalBuffer(iCurrentSize + length);

                    if (iBufferMax != -1)
                    {
                        if (length >= iBufferMax)
                        {
                            // 來源資料長度已超過 Buffer 容量
                            // 直接擷取最後一段
                            Array.Copy(B, (length - iBufferMax) + index, iInternalArray, 0, iBufferMax);
                            iCurrentSize = iBufferMax;
                        }
                        else
                        {
                            // 來源資料長度尚未超過容量
                            int FreeSize;

                            FreeSize = (iBufferMax - length);
                            if (iCurrentSize <= FreeSize)
                            {
                                try
                                {
                                    Array.Copy(B, index, iInternalArray, iCurrentSize, length);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("index:" + index + ",iCurrentSize:" + iCurrentSize + ",length:" + length + ",internalArray:" + iInternalArray.Length + ",B:" + B.Length + "\r\n" + ex.ToString());
                                }

                                iCurrentSize += length;
                            }
                            else
                            {
                                // 空間不足
                                // tmpBytes = Array.CreateInstance(GetType(Byte), iArrayLength)

                                // iInt_Buffer.CopyTo(iInt_Buffer.Count - FreeSize, tmpBytes, 0, FreeSize)
                                // Array.Copy(B, Index, tmpBytes, FreeSize, Length)

                                Array.Copy(iInternalArray, iCurrentSize - FreeSize, iInternalArray, 0, FreeSize);
                                Array.Copy(B, index, iInternalArray, FreeSize, length);
                            }
                        }
                    }
                    else
                    {
                        Array.Copy(B, index, iInternalArray, iCurrentSize, length);
                        iCurrentSize += length;
                    }
                }
            }
        }

        public void RemoveRange(int index, int length)
        {
            lock (iInternalArray)
            {
                if (iCurrentSize > (length + index))
                    Array.Copy(iInternalArray, index + length, iInternalArray, index, iCurrentSize - (index + length));

                if (length <= iCurrentSize)
                    iCurrentSize -= length;
                else
                    // Throw New Exception("length > iCurrentsize:" & length & "," & iCurrentSize)
                    iCurrentSize = 0;
            }
        }

        public void CopyTo(int ArrayIndex, byte[] Destination, int DestinationIndex, int CopyLength)
        {
            if (CopyLength > 0)
                Array.Copy(iInternalArray, ArrayIndex, Destination, DestinationIndex, CopyLength);
        }

        public void Clear()
        {
            iCurrentSize = 0;
        }

        public byte[] ToArray()
        {
            byte[] RetValue = null;

            RetValue = (byte[])Array.CreateInstance(typeof(byte), iCurrentSize);
            Array.Copy(iInternalArray, 0, RetValue, 0, RetValue.Length);

            return RetValue;
        }

        private byte[] CopyArraySegment(byte[] Src, int Index, int Length)
        {
            if ((Index == 0) & (Length == Src.Length))
                return Src;
            else
            {
                byte[] iPartOfArray = null;

                iPartOfArray = (byte[])Array.CreateInstance(typeof(byte), Length);
                Array.Copy(Src, Index, iPartOfArray, 0, iPartOfArray.Length);

                return iPartOfArray;
            }
        }


        private void CheckInternalBuffer(int totalSizeRequire)
        {
            if ((iInternalArray.Length < iBufferMax) | (iBufferMax == -1))
            {
                bool needAlloc = false;
                int newSize;
                int oldSize;

                oldSize = iInternalArray.Length;

                if (totalSizeRequire > iInternalArray.Length)
                    needAlloc = true;

                if (needAlloc)
                {
                    if ((totalSizeRequire % iBlock_Size) != 0)
                        newSize = iBlock_Size * ((totalSizeRequire / iBlock_Size) + 1);
                    else
                        newSize = iBlock_Size * (totalSizeRequire / iBlock_Size);

                    if ((newSize > oldSize))
                    {
                        byte[] newBuffer = null;

                        newBuffer = (byte[])Array.CreateInstance(typeof(byte), newSize);
                        if (iCurrentSize > 0)
                            Array.Copy(iInternalArray, 0, newBuffer, 0, iCurrentSize);

                        iInternalArray = newBuffer;
                    }
                }
            }
        }

        public UltimateByteArrayClass(int MaxBufferSize)
        {
            iBufferMax = MaxBufferSize;
        }

        public UltimateByteArrayClass(int MaxBufferSize, byte[] Source)
        {
            iBufferMax = MaxBufferSize;
            this.AddRange(Source);
        }

        public UltimateByteArrayClass(byte[] Source)
        {
            iBufferMax = -1;
            this.AddRange(Source);
        }

        public UltimateByteArrayClass()
        {
            iBufferMax = -1;
        }

        public IEnumerator<byte> GetEnumeratorOfByte()
        {
            return (IEnumerator<byte>)this.GetEnumerator();
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            return (IEnumerator<byte>)this.GetEnumerator();
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return new ArrayEnumerator(this);
        }

        public class ArrayEnumerator : IEnumerator<byte>
        {
            private UltimateByteArrayClass iCreator;
            private int iPosition = -1;

            protected internal ArrayEnumerator(UltimateByteArrayClass Creator)
            {
                iCreator = Creator;
            }

            public object Current
            {
                get
                {
                    if ((iPosition >= 0) & (iPosition <= iCreator.Count))
                        return iCreator[iPosition];
                    else
                        throw new IndexOutOfRangeException();
                }
            }

            public bool MoveNext()
            {
                iPosition += 1;
                return iPosition < iCreator.Count;
            }

            public void Reset()
            {
                iPosition = -1;
            }

            public byte CurrentOfByte
            {
                get
                {
                    return (byte)this.Current;
                }
            }

            byte IEnumerator<byte>.Current
            {
                get
                {
                    return (byte)this.Current;
                }
            }

            private bool disposedValue;

            // IDisposable
            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                        iCreator = null;
                }
                this.disposedValue = true;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
