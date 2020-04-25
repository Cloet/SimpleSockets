using System;
using SimpleSockets;
using SimpleSockets.Helpers;
using SimpleSockets.Server;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting TCP Server.");
            var server = new SimpleTcpServer();
            server.Listen(13000);

            server.LoggerLevel = LogLevel.Debug;

            server.ClientConnected += ClientConnected;
            server.Logger += Logger;

            Console.ReadLine();
        }

        private static void Logger(string obj)
        {
            Console.WriteLine(obj);
        }

        private static void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine("Client has connected:" + e.ClientInfo.Id);
        }
    }
}
