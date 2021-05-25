using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JCServer
{
    /// <summary>
    /// 解决半包问题
    /// </summary>
    class TCPSocket
    {
        public delegate void MsgHandleBytes(byte[] message);
        public delegate byte[] MsgGetBytes();

        private class HandleBytesToString
        {
            private string result = "";
            public string Result
            {
                get
                {
                    return result;
                }
            }

            public void HandleBytes(byte[] message)
            {
                result += Encoding.UTF8.GetString(message);
            }

            public void Clear() => result = "";
        }

        public static void Send(Socket s, byte[] message)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes((long)message.Length));
            ms.Position = ms.Length;
            ms.Write(message);
            s.Send(ms.ToArray(), 0);
        }

        public static void Send(Socket s, long length, MsgGetBytes getting)
        {
            s.Send(BitConverter.GetBytes(length));
            long sendLen = 0;
            while (sendLen < length)
            {
                byte[] getBytes = getting();
                if (getBytes == null || getBytes.Length == 0) throw new Exception("错误 : 回调数据不能为空");

                s.Send(getBytes, 0);
                sendLen += getBytes.Length;
            }
        }

        public static void Recv(Socket s, MsgHandleBytes handle, long RecvSplitLength = 4194304)
        {
            byte[] header = new byte[8];
            s.Receive(header, header.Length, 0);
            long totalLength, recvLength = 0;

            using (MemoryStream ms = new MemoryStream(header))
            {
                using (BinaryReader br = new BinaryReader(ms))
                    totalLength = br.ReadInt64();
            }

            while (recvLength < totalLength)
            {
                byte[] bytes = new byte[RecvSplitLength];
                int len = s.Receive(bytes, bytes.Length, 0);
                recvLength += len;

                handle(bytes.Take(len).ToArray()); //数据回调
            }
        }

        public static void Send(Socket s, string message)
        {
            Send(s, Encoding.UTF8.GetBytes(message));
        }

        public static string Recv(Socket s)
        {
            HandleBytesToString r = new HandleBytesToString();
            Recv(s, r.HandleBytes);
            return r.Result;
        }


    }
}
