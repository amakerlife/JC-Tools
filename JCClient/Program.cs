using JCServer;
using System;
using System.Collections.Generic;
using System.IO;
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

        static void Send(Socket s, string message)
        {
            s.Send(Encoding.UTF8.GetBytes(message), 0);
        }

        static void OutputErrorMessage(Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("$ error : " + e.Message);
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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Welcome to ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("JCClient");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\nIf you want to know the ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("JCServer");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\'s ip, you'd input ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\"./Host\"");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("(Linux) or ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\"Host.exe\"");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("(Windows).\nYou'd better in the same local area network as the ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("JCServer\n");
        }

        static public Dictionary<string, Socket> connectList = new Dictionary<string, Socket>();

        /// <summary>
        /// 将文件从本地上传到Server
        /// </summary>
        /// <param name="s">Server的Socket</param>
        /// <param name="srcPath">本地文件路径</param>
        /// <param name="upPath">上传到Server的路径</param>
        static void Upload(Socket s, string srcPath, string upPath)
        {
            using (FileStream r = new FileStream(srcPath, FileMode.Open, FileAccess.Read))
            {
                Send(s, "upload " + r.Length.ToString() + " " + upPath);
                long len = r.Length;
                while (len > 0)
                {
                    byte[] file = new byte[4 * 1024]; //每次发送4MB
                    Task t = r.ReadAsync(file, 0, file.Length);
                    while (!t.IsCompleted) ;

                    int recvLen = s.Send(file, file.Length, 0);
                    if (recvLen <= 0) break;

                    len -= recvLen;
                }
            }
        }

        /// <summary>
        /// 从Server下载文件
        /// </summary>
        /// <param name="s">Server的Socket</param>
        /// <param name="srcPath">Server端的文件路径</param>
        /// <param name="downPath">本地文件路径</param>
        static void Download(Socket s, string srcPath, string downPath)
        {
            Send(s, "download " + srcPath);

            long totalLen;
            try
            {
                totalLen = long.Parse(Recv(s));
            }
            catch
            {
                return;
            }

            using (FileStream w = new FileStream(downPath, FileMode.Create, FileAccess.Write))
            {
                while (totalLen > 0)
                {
                    byte[] file = new byte[4 * 1024]; //每次接收4MB
                    int recvLen = s.Receive(file, totalLen < 4 * 1024 ? (int)totalLen : 4 * 1024, 0);
                    if (recvLen <= 0) break;
                    Task t = w.WriteAsync(file, 0, recvLen);
                    while (!t.IsCompleted) ;

                    totalLen -= recvLen;
                }
            }

            Send(s, "ok");
        }

        static void ProcCommand()
        {
            string send = "";
            while (send != "quit")
            {

                Console.ForegroundColor = ConsoleColor.White;
                OutputSuffix(Dns.GetHostName(), connectList.Count.ToString());
                send = Console.ReadLine();
                try
                {
                    InforProc m = new InforProc(send);
                    
                    if (m[0] == "quit" && m.Count == 1) //退出
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Are you sure to exit ? It will kill all links. [Y/N]");
                        var result = Console.ReadKey();

                        if(result.KeyChar == 'Y')
                            return;
                    }
                    else if(m[0] == "clear")
                    {
                        Console.Clear();
                        OutputHeader();
                        continue;
                    }
                    else if(m[0] == "help")
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
                    else if (m[0] == "list")
                    {
                        if(m.Count == 1)
                        {
                            int i = 0;
                            foreach(var iter in connectList)
                            {
                                OutputSuffix("Item = " + i.ToString(), iter.Key, ConsoleColor.Red);
                            }
                        }
                        else if ((m[1] == "add" || m[1] == "add-ip") && m.Count == 3)
                        {
                            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(m[2]), 5800);
                            s.Connect(ipe);//尝试建立连接
                            connectList.Add(m[2], s);
                        }
                        else if (m[1] == "add-host" && m.Count == 3)
                        {
                            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            var hostList = Dns.GetHostAddresses(m[2]);
                            IPEndPoint ipe = new IPEndPoint(hostList[hostList.Length - 1], 5800);
                            s.Connect(ipe);//尝试建立连接
                            connectList.Add(m[2], s);
                        }
                        else if (m[1] == "remove" && m.Count == 3)
                        {
                            if (connectList[m[2]] != null)
                                connectList[m[2]].Close();
                            connectList.Remove(m[2]);
                        }
                        else if (m[1] == "clear" && m.Count == 2)
                        {
                            foreach (var item in connectList)
                            {
                                if (item.Value != null)
                                    item.Value.Close();
                            }
                            connectList.Clear();
                        }
                    }
                    else if (m[0] == "send")
                    {
                        string sendString = "";
                        for (int iter = 1; iter < m.Count; ++iter)
                            sendString += m[iter] + " ";

                        foreach (var iter in connectList)
                        {
                            if (m[1] == "upload" && m.Count == 4)
                            {
                                Upload(iter.Value, m[2], m[3]);
                            }
                            else if(m[1] == "download" && m.Count == 4)
                            {
                                Download(iter.Value, m[2], m[3]);
                            }
                            else
                            {
                                Send(iter.Value, sendString);
                            }

                            OutputSuffix("Recv", iter.Key, ConsoleColor.Red);
                            Console.WriteLine(Recv(iter.Value));
                        }
                    }
                    else if (m[0] == "send-server")
                    {
                        string sendString = "";
                        for (int iter = 2; iter < m.Count; ++iter)
                            sendString += m[iter] + " ";

                        if (m[2] == "upload" && m.Count == 5)
                        {
                            Upload(connectList[m[1]], m[3], m[4]);
                        }
                        else if(m[2] == "download" && m.Count == 5)
                        {
                            Download(connectList[m[1]], m[3], m[4]);
                        }
                        else
                        {
                            Send(connectList[m[1]], sendString);
                        }

                        OutputSuffix("Recv", m[1], ConsoleColor.Red);
                        Console.WriteLine(Recv(connectList[m[1]]));
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
