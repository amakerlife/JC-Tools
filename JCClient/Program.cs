using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JCClient
{
    class Program
    {
        static void OutputErrorMessage(Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("$ error : " + e.Message + "\n");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void OutputSuffix(string key, string value, ConsoleColor keyColor = ConsoleColor.Yellow, ConsoleColor valueColor = ConsoleColor.Green)
        {
            ConsoleColor c = Console.ForegroundColor;
            Console.ForegroundColor = keyColor;
            Console.Write(key);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(":");
            Console.ForegroundColor = valueColor;
            Console.Write(value);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("~$ ");
            Console.ForegroundColor = c;
        }

        static void OutputHeader()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("JC-Tools 2.0");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" by Return");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Report ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("bugs ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("to ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("https://gitee.com/graph-lc/jc-tools/issues");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(".\n");
        }

        static public Dictionary<string, Socket> connectList = new Dictionary<string, Socket>();

        private class UploadFileToSocket : IDisposable
        {
            private FileStream file = null;

            public UploadFileToSocket(string fileName) => file = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            ~UploadFileToSocket() => file.Close();

            public void Dispose() => file.Close();

            public byte[] Handle()
            {
                byte[] bytes = new byte[4 * 1024 * 1024]; //每次发送4MB
                long position = file.Position;
                Task t = file.ReadAsync(bytes, 0, bytes.Length);
                t.Wait();
                return bytes.Take(file.Length - position > 4 * 1024l * 1024l ? 4 * 1024 * 1024 : (int)(file.Length - position)).ToArray();
            }
            
            public long Length
            {
                get
                {
                    return file.Length;
                }
            }
        }

        private class DownloadFileFromSocket :IDisposable
        {
            private FileStream file = null;
            
            public DownloadFileFromSocket(string filename) => file = new FileStream(filename, FileMode.Create, FileAccess.Write);
            ~DownloadFileFromSocket() => file.Close();

            public void Dispose() => file.Close();

            public void Handle(byte[] message)
            {
                Task t = file.WriteAsync(message, 0, message.Length);
                t.Wait();
            }
        }

        /// <summary>
        /// 将文件从本地上传到Server
        /// </summary>
        /// <param name="s">Server的Socket</param>
        /// <param name="srcPath">本地文件路径</param>
        /// <param name="upPath">上传到Server的路径</param>
        static void Upload(Socket s, string srcPath, string upPath)
        {
            TCPSocket.Send(s, "upload \"" + upPath + "\"");
            TCPSocket.Recv(s); //回调，防止粘包

            UploadFileToSocket up = new UploadFileToSocket(srcPath);
            TCPSocket.Send(s, up.Length, up.Handle);
            up.Dispose();
        }

        /// <summary>
        /// 从Server下载文件
        /// </summary>
        /// <param name="s">Server的Socket</param>
        /// <param name="srcPath">Server端的文件路径</param>
        /// <param name="downPath">本地文件路径</param>
        static void Download(Socket s, string srcPath, string downPath)
        {
            TCPSocket.Send(s, "download \"" + srcPath + "\"");
            if (TCPSocket.Recv(s) != "ok") return;
            /*using (FileStream w = new FileStream(downPath, FileMode.Create, FileAccess.Write))
            {
                while (totalLen > 0)
                {
                    byte[] file = new byte[16 * 1024 * 1024]; //每次接收4MB
                    int recvLen = s.Receive(file, totalLen < 16 * 1024 * 1024 ? (int)totalLen : 16 * 1024 * 1024, 0);
                    if (recvLen <= 0) break;
                    Task t = w.WriteAsync(file, 0, recvLen);
                    while (!t.IsCompleted) ;

                    totalLen -= recvLen;
                }
            }*/

            DownloadFileFromSocket down = new DownloadFileFromSocket(downPath);
            TCPSocket.Recv(s, down.Handle);
            
            TCPSocket.Send(s, "ok");
            down.Dispose();
        }

        /// <summary>
        /// 截取JCServer的屏幕，并下载到JClient
        /// </summary>
        /// <param name="s">JCServer的Socket</param>
        /// <param name="mode">截图的格式(jpg/png)</param>
        /// <param name="downPath">本地文件的路径</param>
        static void Screen(Socket s, string mode, string downPath)
        {
            TCPSocket.Send(s, "screen \"" + mode + "\"");
            DownloadFileFromSocket down = new DownloadFileFromSocket(downPath);
            TCPSocket.Recv(s, down.Handle);
            TCPSocket.Send(s, "ok");
            down.Dispose();
        }

        /// <summary>
        /// 匹配命令缩写
        /// </summary>
        /// <param name="input">输入的命令</param>
        /// <param name="command">匹配的命令</param>
        /// <param name="command2">命令缩写</param>
        /// <param name="splitLength">命令缩写长度</param>
        /// <returns></returns>
        static bool SimpleCommand(string input, string command,string command2="", int splitLength = 1)
        {
            return input == command || (command2 != "" && input==command2) || (splitLength > 0 && input == command.Substring(0, splitLength));
        }
        
        /// <summary>
        /// 处理命令
        /// </summary>
        static void ProcCommand()
        {
            while (true)
            {
                string send = "";
                Console.ForegroundColor = ConsoleColor.White;
                OutputSuffix(Dns.GetHostName(), connectList.Count.ToString());
                send = Console.ReadLine();

                try
                {
                    InforProc m = new InforProc(send);

                    if (SimpleCommand(m[0], "quit") && m.Count == 1) //退出
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Are you sure to exit ? It will kill all links. [Y/N]");
                        var result = Console.ReadKey();
                        Console.WriteLine();

                        if (result.KeyChar == 'Y')
                            return;
                    }
                    else if (SimpleCommand(m[0], "clear") && m.Count == 1)
                    {
                        Console.Clear();
                        OutputHeader();
                        continue;
                    }
                    else if (SimpleCommand(m[0], "help") && m.Count == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("\nJC-Tool 1.4\n");
                        Console.WriteLine("命令\t\t\t\t解释\t\t\t\t\t\t\t示例");
                        for (int iter = 0; iter < 100; ++iter)
                            Console.Write("-");
                        Console.WriteLine();
                        Console.WriteLine("quit\t\t\t\t退出");
                        Console.WriteLine("clear\t\t\t\t清空屏幕");
                        Console.WriteLine("list\t\t\t\t列出已经连接的 JCServer");
                        Console.WriteLine("list add [ip]\t\t\t向列表添加并连接此 ip 中的 JCServer\t\t\tlist add 127.0.0.1");
                        Console.WriteLine("list add-ip [ip]\t\t向列表添加并连接此 ip 中的 JCServer\t\t\tlist add 127.0.0.1");
                        Console.WriteLine("list add-host [name]\t\t向列表添加并解析 主机名 到 JCServer\t\t\tlist add XUE001");
                        Console.WriteLine("list remove [name]\t\t从列表中移除 name\t\t\t\t\tlist remove 127.0.0.1 或 list remove XUE001");
                        Console.WriteLine("list clear\t\t\t清空列表");
                        Console.WriteLine("send [command]\t\t\t向连接列表中的所有 JCServer 发送 [command]\t\tsend mouse move 1 1");
                        Console.WriteLine("send-server [name] [command]\t从列表中寻找名为 name 的连接，并向其发送 [command]\tsend-server 127.0.0.1 mouse move 1 1 或 send-server XUE001 mouse move 1 1");
                        Console.WriteLine();
                        Console.WriteLine("有关 [command] 的写法，请参阅 https://gitee.com/graph-lc/jc-tools\n");
                    }
                    else if (SimpleCommand(m[0], "list"))
                    {
                        if (m.Count == 1)
                        {
                            int i = 0;
                            Console.WriteLine();
                            foreach (var iter in connectList)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write(iter.Key);
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(":");
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.Write(((IPEndPoint)iter.Value.RemoteEndPoint).Port.ToString());
                                Console.WriteLine();
                            }
                            Console.WriteLine();
                        }
                        else if (SimpleCommand(m[1], "add") && (m.Count == 3 || m.Count == 4))
                        {
                            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(m[2]), m.Count == 3 ? 5800 : int.Parse(m[3]));
                            s.Connect(ipe);//尝试建立连接
                            connectList.Add(m[2], s);
                        }
                        else if (SimpleCommand(m[1], "add-host", "ah", 0) && (m.Count == 3 || m.Count == 4))
                        {
                            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            var hostList = Dns.GetHostAddresses(m[2]);
                            IPEndPoint ipe = new IPEndPoint(hostList[hostList.Length - 1], m.Count == 3 ? 5800 : int.Parse(m[3]));
                            s.Connect(ipe);//尝试建立连接
                            connectList.Add(m[2], s);
                        }
                        else if (SimpleCommand(m[1], "remove") && m.Count == 3)
                        {
                            if (connectList[m[2]] != null)
                                connectList[m[2]].Close();
                            connectList.Remove(m[2]);
                        }
                        else if (SimpleCommand(m[1], "clear") && m.Count == 2)
                        {
                            foreach (var item in connectList)
                            {
                                if (item.Value != null)
                                    item.Value.Close();
                            }
                            connectList.Clear();
                        }
                        else throw new Exception("Unrecognized command");
                    }
                    else if (SimpleCommand(m[0], "send") && m.Count >= 2)
                    {
                        Console.WriteLine();
                        string sendString = "";
                        for (int iter = 1; iter < m.Count; ++iter)
                            sendString += "\"" + m[iter] + "\" ";

                        foreach (var iter in connectList)
                        {
                            if (SimpleCommand(m[1], "upload") || SimpleCommand(m[1], "download") || SimpleCommand(m[1], "screen", "", 2))
                            {
                                if (m.Count == 4)
                                {
                                    if (SimpleCommand(m[1], "upload"))
                                    {
                                        Upload(iter.Value, m[2], m[3]);
                                    }
                                    else if (SimpleCommand(m[1], "download"))
                                    {
                                        Download(iter.Value, m[2], m[3]);
                                    }
                                    else if (SimpleCommand(m[1], "screen", "", 2))
                                    {
                                        // 格式
                                        // send screen png/jpg [ScreenFileName]
                                        Screen(iter.Value, m[2], m[3]);
                                    }
                                }
                                else throw new Exception("Wrong parameter length");
                            }
                            else
                            {
                                TCPSocket.Send(iter.Value, sendString);
                            }

                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("  Answer from ");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write(iter.Key);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write(" : [Handling...]");

                            string recvString = TCPSocket.Recv(iter.Value);
                            Console.Write(new string('\b', 13));
                            Console.Write(new string(' ', 13));
                            Console.Write(new string('\b', 13));
                            Console.WriteLine(recvString);
                        }
                        Console.WriteLine();
                    }
                    else if (SimpleCommand(m[0], "send-server", "sh", 0) && m.Count >= 3)
                    {
                        Console.WriteLine();
                        string sendString = "";
                        for (int iter = 2; iter < m.Count; ++iter)
                            sendString += "\"" + m[iter] + "\" ";

                        if (SimpleCommand(m[2], "upload") || SimpleCommand(m[2], "download") || SimpleCommand(m[2], "screen", "", 2))
                        {
                            if (m.Count == 5)
                            {
                                if (SimpleCommand(m[2], "upload"))
                                {
                                    Upload(connectList[m[1]], m[3], m[4]);
                                }
                                else if (SimpleCommand(m[2], "download"))
                                {
                                    Download(connectList[m[1]], m[3], m[4]);
                                }
                                else if (SimpleCommand(m[2], "screen", "", 2))
                                {
                                    // 格式
                                    // send-server [host-name] screen png/jpg [ScreenFileName]
                                    Screen(connectList[m[1]], m[3], m[4]);
                                }
                            }
                            else throw new Exception("Wrong parameter length");
                        }
                        else
                        {
                            TCPSocket.Send(connectList[m[1]], sendString);
                        }

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("  Answer from ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(m[1]);
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(" : [Handling...]");

                        Console.ForegroundColor = ConsoleColor.White;
                        string recvString = TCPSocket.Recv(connectList[m[1]]);
                        Console.Write(new string('\b', 13));
                        Console.Write(new string(' ', 13));
                        Console.Write(new string('\b', 13));
                        Console.WriteLine(recvString);
                        Console.WriteLine();

                    }
                    else
                    {
                        OutputErrorMessage(new Exception("Unrecognized command"));
                    }
                }
                catch (Exception e)
                {
                    OutputErrorMessage(e);
                }
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "JCClient by Return";
            OutputHeader();
            ProcCommand();
        }
    }
}
