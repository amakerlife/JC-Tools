using System;
using System.Net;

namespace Host
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("$ HostName = ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Dns.GetHostName());
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("$ Address = ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
                Console.WriteLine("\t" + address.ToString());
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Press any key to continue.");
            Console.ReadKey();
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
