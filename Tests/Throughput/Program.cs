using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Throughput
{
	class Program
	{
		static int clients = 25;
		static int amount = 100;
		static string errors = "";
		static Counter counter = new Counter();
		static Random _random = new Random((int)DateTime.Now.Ticks);

		static long expected => clients * amount;

		private static SimpleServer _server;

		static void Main(string[] args)
		{

			Console.Write("TCP (1) or UDP (2) Test ? ");
			var choice = Console.ReadLine();

			var useUDP = (choice == "2");

			if (useUDP)
				_server = new SimpleUdpServer();
			else
				_server = new SimpleTcpServer();

			_server.LoggerLevel = SimpleSockets.Helpers.LogLevel.Debug;
			BindServerEvents(_server);
			_server.Listen(13000);

			for (int i = 0; i < clients; i++) {
				new Thread(() => WriteMessages(useUDP)).Start();
			}

			while (true) {
				Console.ReadLine();
				Console.WriteLine("Amount of messages received: " + counter.GetCount + " expected: "+(expected));
				Console.WriteLine(_server.Statistics.ToString());
				Console.WriteLine(errors.ToString());
			}
		}

		private static void WriteMessages(bool useUDP) {

			SimpleClient client;
			if (useUDP)
				client = new SimpleUdpClient();
			else
				client = new SimpleTcpClient();

			client.LoggerLevel = SimpleSockets.Helpers.LogLevel.Debug;
			client.ConnectTo("127.0.0.1", 13000);
			client.Logger += ClientLogger;

			while (!client.IsConnected()) {
				Task.Delay(50).Wait();
			}

			for (int i = 0; i < amount; i++) {
				Task.Delay(_random.Next(0, 10)).Wait();
				client.SendMessage($"This is a test message {i+1}");
			}

		}

		private static void BindServerEvents(SimpleServer server) {
			server.MessageReceived += Server_MessageReceived;
			server.Logger += Logger;
		}

		private static void Logger(string obj) {
			Console.WriteLine(obj.ToString());
			errors += obj.ToString();
		}

		private static void ClientLogger(string obj) {
			Console.WriteLine(obj.ToString());
			errors += obj.ToString();
		}

		private static void Server_MessageReceived(object sender, ClientMessageReceivedEventArgs e)
		{
			counter.Count();
			if ( (counter.GetCount- clients) == expected) {
				Console.WriteLine("All messages have been received:" + _server.Statistics.UpTime);
			}
			// Console.WriteLine($"{e.ClientInfo.Id}|{e.ClientInfo.Guid}:{e.Message}");
		}
	}

	public sealed class Counter
	{
		private int _current = 0;

		public int GetCount => _current;

		public void Count()
		{
			Interlocked.Increment(ref _current);
		}

		public void reset()
		{
			_current = 0;
		}
	}
}
