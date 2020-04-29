using System;
using SimpleSockets;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Server;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting TCP Server.");
            var server = new SimpleTcpServer(false);
            server.Listen(13000);

            server.LoggerLevel = LogLevel.Debug;

            server.ClientConnected += ClientConnected;
            server.Logger += Logger;
			server.MessageReceived += Server_MessageReceived;
			server.PreSharedKey = new byte[16];
			server.PreSharedKey[0] = 5;
			server.CompressionMethod = CompressionType.Deflate;


			var client = new SimpleTcpClient(false);
			client.Connect("127.0.0.1", 13000, 5);
			client.Logger += Logger2;
			client.CompressionMethod = CompressionType.Deflate;
			client.LoggerLevel = LogLevel.Debug;
			client.PreSharedKey = new byte[16];
			client.PreSharedKey[0] = 5;

			//while (true)
			//{
			//	Console.WriteLine("Press any key to send a message.");
			//	Console.Read();
			//	client.SendMessage("Test message");
			//	Console.ReadLine();
			//}

			for (int i = 0; i < 1000; i++)
			{
				client.SendMessage("Test message " + (i+1).ToString());
			}

			Console.ReadLine();
        }

		private static void Server_MessageReceived(object sender, MessageReceivedEventArgs e)
		{
			Console.WriteLine("Received a message:" + e.Message);
		}

		private static void Logger(string obj)
        {
            // Console.WriteLine(obj);
        }

		private static void Logger2(string obj) {
			// Console.WriteLine(obj);
		}

        private static void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine("Client has connected:" + e.ClientInfo.Id);
        }
    }
}
