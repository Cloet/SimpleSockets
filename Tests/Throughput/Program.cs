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
		static int clients = 500;
		static int amount = 1000000;
		static string errors = "";
		static Counter counter = new Counter();
		static Random _random = new Random((int)DateTime.Now.Ticks);


		static void Main(string[] args)
		{
			var server = new SimpleTcpServer();
			server.LoggerLevel = SimpleSockets.Helpers.LogLevel.Debug;
			BindServerEvents(server);
			server.Listen(13000);

			for (int i = 0; i < clients; i++) {
				new Thread(() => WriteMessages()).Start();
			}

			while (true) {
				Console.ReadLine();
				Console.WriteLine("Amount of messages received: " + counter.GetCount + " expected: "+(clients*amount));
				Console.WriteLine(server.Statistics.ToString());
				Console.WriteLine(errors.ToString());
			}
		}

		private static void WriteMessages() {
			var client = new SimpleTcpClient();
			client.LoggerLevel = SimpleSockets.Helpers.LogLevel.Debug;
			client.ConnectTo("127.0.0.1", 13000);
			client.Logger += ClientLogger;

			while (!client.IsConnected()) {
				Task.Delay(50).Wait();
			}

			for (int i = 0; i < amount; i++) {
				Task.Delay(_random.Next(0, 25)).Wait();
				client.SendMessage($"This is a test message {i+1}");
			}

		}

		private static void BindServerEvents(SimpleTcpServer server) {
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
			Console.WriteLine($"{e.ClientInfo.Id}:{e.Message}");
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
