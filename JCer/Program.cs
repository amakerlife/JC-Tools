using JCServer;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JCer
{
    class Program
    {
        static void OutputErrorMessage(Exception e)
        {
            /*Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("$ error = " + e.Message);
            Console.ForegroundColor = ConsoleColor.White;*/
        }

        static string Recv(Socket s)
        {
            try
            {
                byte[] bytes = new byte[4 * 1024]; //4MB
                int len = s.Receive(bytes, bytes.Length, 0);
                return Encoding.UTF8.GetString(bytes, 0, len);
            }
            catch (Exception e)
            {
                OutputErrorMessage(e);
            }
            return "quit";
        }

        static bool Send(Socket s, string message)
        {
            try
            {
                s.Send(Encoding.UTF8.GetBytes(message), 0);
                return true;
            }
            catch (Exception e)
            {
                OutputErrorMessage(e);
            }
            return false;
        }

        //注意，这些功能仅限 WindowsOS
        [DllImport("user32.dll")]
        private static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        static void RequestConnect(object o)
        {
            try
            {
                Socket accept = (Socket)o;

                string recv = Recv(accept);
                while (recv != "quit")
                {
                    InforProc m = new InforProc(recv);
                    if (m[0] == "mouse")
                    {
                        if (m[1] == "move" && m.Count == 4)
                        {
                            SetCursorPos(int.Parse(m[2]), int.Parse(m[3]));
                            if (!Send(accept, "$ result = ok"))
                                break;
                        }
                        else
                        {
                            if (!Send(accept, "$ error = unrecognized command"))
                                break;
                        }
                    }
                    else if (m[0] == "key")
                    {
                        if (m[1] == "down" || m[1] == "up" || m[1] == "push" && m.Count >= 3)
                        {
                            if (m[1] == "down")
                                keybd_event((byte)long.Parse(m[2]), 0, 0, 0);

                            else if (m[1] == "up" && m.Count == 3)
                            {
                                keybd_event((byte)long.Parse(m[2]), 0, 2, 0);
                            }
                            else if (m[1] == "push" && m.Count == 3)
                            {
                                foreach (char c in m[2])
                                {
                                    keybd_event((byte)c, 0, 0, 0);
                                    keybd_event((byte)c, 0, 2, 0);
                                }
                            }
                            if (!Send(accept, "$ result = ok"))
                                break;
                        }
                        else
                        {
                            if (!Send(accept, "$ error = unrecognized command"))
                                break;
                        }
                    }
                    else if (m[0] == "shell" && m.Count >= 2)
                    {
                        string arg = "";
                        for (int i = 2; i < m.Count; ++i) arg += "\"" + m[i] + "\" ";
                        Process.Start(m[1], arg);

                        if (!Send(accept, "$ result = ok"))
                            break;
                    }
                    else if (m[0] == "download" && m.Count == 4)
                    {
                        /* 格式
                         * download 长度 文件名
                         */

                        long totalLen = long.Parse(m[1]);
                        using (FileStream w = new FileStream(m[3], FileMode.Create, FileAccess.Write))
                        {
                            while (totalLen > 0)
                            {
                                byte[] file = new byte[4 * 1024]; //每次接收4MB
                                int recvLen = accept.Receive(file, file.Length, 0);
                                if (recvLen <= 0) break;
                                Task t = w.WriteAsync(file, 0, file.Length);
                                while (!t.IsCompleted) ;

                                totalLen -= recvLen;
                            }
                        }

                        if (!Send(accept, "$ result = ok"))
                            break;
                    }
                    else
                    {
                        if (!Send(accept, "$ error = unrecognized command"))
                            break;
                    }

                    recv = Recv(accept);
                }
            }
            catch { }
        }

        static void Request()
        {
            Socket query = null;
            try
            {
                query = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                query.Bind(new IPEndPoint(IPAddress.Any, 5800)); //端口
                query.Listen(10); //最多被10个人控制

                while (true)
                    RequestConnect(query.Accept());
                //query.Close();
            }
            catch (Exception) //只有出现异常清空才停止监听，退出进程
            {
                if (query != null)
                    query.Close();
            }
        }

        static void Main(string[] args)
        {
            Request();

            return;
        }
    }
}