using SimpleSockets;
using System;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
			Console.WriteLine("Hello.");
			var client = new SimpleTcpClient(false);
			client.Connect("127.0.0.1", 13000,5);


			while (true) {
				Console.WriteLine("Press any key to send a message.");
				Console.Read();
				client.SendMessage("Test message");
			}

        }
    }
}
