using JCServer;
using System;
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
            Console.WriteLine("$ error = " + e.Message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void OutputHostName(string name)
        {
            ConsoleColor c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(name);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(":~$ ");
            Console.ForegroundColor = c;
        }

        static void OutputHeader()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Welcome to ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("JCClient");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\nIf you want to know the ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("JCServer");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\'s ip, you'd input ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("\"./Host\"");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("(Linux) or ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("\"Host.exe\"");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("(Windows).\nYou'd better in the same local area network as the ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("JCServer\n");
        }

        static void ConnectServer()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Please input the ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("JCServer");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\'s ip : ");
            Console.ForegroundColor = ConsoleColor.Gray;

            string address = Console.ReadLine();
            if (address == "clear")
            {
                Console.Clear();
                OutputHeader();
                return;
            }

            Socket s = null;
            try
            {
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(address), 5800);
                s.Connect(ipe);//尝试建立连接
            }
            catch (Exception e)
            {
                OutputErrorMessage(e);
                return;
            }

            try
            {
                string send = "";
                while (send != "quit")
                {

                    Console.ForegroundColor = ConsoleColor.White;
                    OutputHostName(address);
                    send = Console.ReadLine();

                    InforProc m = new InforProc(send);
                    if (m[0] == "filelen" && m.Count == 2)
                    {
                        using (FileStream r = new FileStream(m[1], FileMode.Open, FileAccess.Read))
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(r.Length.ToString() + "\n");
                        }
                    }
                    else
                    {
                        Send(s, send);

                        if (m[0] == "download" && m.Count == 4)
                        {
                            using (FileStream r = new FileStream(m[2], FileMode.Open, FileAccess.Read))
                            {
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
                        if (send != "quit")
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(Recv(s) + "\n");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OutputErrorMessage(e);
            }

        }
        
        static void Main(string[] args)
        {
            Console.Title = "JCClient by Return";
            OutputHeader();

            while(true)
                ConnectServer();
        }
    }
}
