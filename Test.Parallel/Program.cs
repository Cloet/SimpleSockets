using System;
using System.Collections;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Client;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;

namespace Test.Parallel
{
	class Program
	{
		private static SimpleSocketListener _server;
		private static Random _random = new Random((int) DateTime.Now.Ticks);
		private static int _clientId;
		private static int _numMsg = 10000;
		private static int _clientThreads = 500;
		private static int _totalToReceive = _numMsg * _clientThreads;
		private static Counter _received = new Counter();
		private static Counter _receivedSubmitted = new Counter();
		private static Counter _receivedEmpty = new Counter();
		private static Counter _receivedError = new Counter();

		static void Main(string[] args)
		{
			Console.WriteLine("Starting Test...");
			StartServer();
			Thread.Sleep(1000);
			Task task = null;

			for (var i = 0; i < _clientThreads; i++)
			{
				Task.Run(() => ClientTask());
			}

			//Console.WriteLine("Received " + _received + " messages.");
			//Console.WriteLine("Received " + _received2 + " messages.");
			Console.ReadLine();

			Console.WriteLine("==================================");
			Console.WriteLine("Received : " + _received.GetCount);
			Console.WriteLine("Empty message: " + _receivedEmpty.GetCount);
			Console.WriteLine("Errors : " + _receivedError.GetCount);
			Console.WriteLine("Submitted : " + _receivedSubmitted.GetCount);
			Console.WriteLine("==================================");

			Console.ReadLine();

		}

		private static void StartServer()
		{
			_server = new SimpleSocketTcpListener();
			_server.ServerHasStarted += ServerOnServerHasStarted;
			_server.MessageReceived += ServerOnMessageReceived;
			_server.ServerErrorThrown += ServerOnServerErrorThrown;
			_server.StartListening(13000);
		}

		private static void ServerOnServerErrorThrown(Exception ex)
		{
			_receivedError.Count();
			Console.WriteLine("=================================");
			Console.WriteLine("Server Error.");
			Console.WriteLine(ex.Message);
			Console.WriteLine("Stacktrace: " + ex.StackTrace);
			Console.WriteLine("=================================");
		}

		private static void ClientTask()
		{
			//using (var client = new SimpleSocketTcpClient())
			//{
				var client = new SimpleSocketTcpClient();
				_clientId++;
				client.MessageReceived += ClientOnMessageReceived;
				client.ConnectedToServer += ClientOnConnectedToServer;
				client.ClientErrorThrown += ClientOnClientErrorThrown;
				client.MessageSubmitted += ClientOnMessageSubmitted;
				client.StartClient("127.0.0.1", 13000);

				//Thread.Sleep(1000);

				for (int i = 0; i < _numMsg; i++)
				{
					Task.Delay(_random.Next(0, 25)).Wait();
					client.SendMessage("Test Message " + (i + 1));
				}

			//}

			Console.WriteLine("[CLIENT] has finished.");
		}

		private static void ClientOnMessageSubmitted(SimpleSocketClient client, bool close)
		{
			_receivedSubmitted.Count();
		}

		private static void ClientOnClientErrorThrown(SimpleSocketClient client, Exception ex)
		{
			_receivedError.Count();
			Console.WriteLine("=================================");
			Console.WriteLine("Client Error.");
			Console.WriteLine(ex.Message);
			Console.WriteLine("Stacktrace: " + ex.StackTrace);
			Console.WriteLine("=================================");
		}

		private static void ServerOnMessageReceived(IClientInfo client, string message)
		{
			_received.Count();
			Console.WriteLine("Server has received a message from client " + client.Id + "|" + client.Guid + ", " + message);

			if (string.IsNullOrEmpty(message))
				_receivedEmpty.Count();

			if (_received.GetCount == _totalToReceive)
			{
				Console.WriteLine("==================================");
				Console.WriteLine("Received : " + _received.GetCount);
				Console.WriteLine("All messages have been received...");
				Console.WriteLine("==================================");
			}


		}

		private static void ClientOnConnectedToServer(SimpleSocketClient client)
		{
			Console.WriteLine("Client has connected to the server.");
		}

		private static void ClientOnMessageReceived(SimpleSocketClient client, string msg)
		{
			Console.WriteLine("Message from client received.");
		}

		private static void ServerOnServerHasStarted()
		{
			Console.WriteLine("Server has started.");
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
