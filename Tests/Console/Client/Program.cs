using Shared;
using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
	class Program
	{
		private static event EventHandler<DataReceivedEventArgs> PersonObjectReceived;
		private static event EventHandler<DataReceivedEventArgs> CustomMessageReceived;

		private static IList<SimpleClient> Clients = new List<SimpleClient>();

		private static ManualResetEvent _connected = new ManualResetEvent(false);

		static void Main(string[] args)
		{
			Console.WriteLine("Starting client.");

			var input = EnableSsl();
			var no_clients = Amount_Of_Clients();
			var context = new SslContext(new X509Certificate2(new SocketHelper().GetCertFileContents(), "Password"));	
			
			PersonObjectReceived += Program_PersonObjectReceived;
			CustomMessageReceived += Program_CustomMessageReceived;

			for (var i = 0; i < no_clients; i++) {
				_connected.Reset();
				StartClient(input == "y", context);
				_connected.WaitOne();
			}
			

			// Messaging
			while (true)
			{
				Console.Write("Enter a message: ");
				var msg = Console.ReadLine();
				foreach(var client in Clients) {
					client.SendMessage(msg);
				}
			}
		}

		private static int Amount_Of_Clients() {
			while(true) {
				Console.Write("How much clients should be started? [1-10] ");
				var input = Console.ReadLine();
				var valid = Int32.TryParse(input, out var result);

				if (valid) {
					if (result > 0 && result <= 10) {
						return result;
					}
				}

				Console.WriteLine("invalid input.");

			}
		}

		private static void StartClient(bool ssl, SslContext context) {
			SimpleClient client;
			
			if (ssl)
				client = new SimpleTcpClient(context);
			else
				client = new SimpleTcpClient();
			
			client.LoggerLevel = LogLevel.Trace;
			
			client.ConnectTo("127.0.0.1", 13000, 5);
			client.CompressionMethod = CompressionMethod.None;
			BindEvents(client);
			Clients.Add(client);

			client.DynamicCallbacks.Add("Person", PersonObjectReceived);
			client.DynamicCallbacks.Add("CustomMessage", CustomMessageReceived);

		}

		private static string EnableSsl() {

			while (true) {
				Console.Write("Enable ssl ? [y/n] ");				
				var input = Console.ReadLine();
				if (input.Trim() == "y" || input.Trim() == "n")
					return input.Trim();
				else
					Console.WriteLine("invalid input.");
			}
		}


		private static void Program_CustomMessageReceived(object sender, DataReceivedEventArgs e)
		{
			Console.WriteLine("Received a custom message:" + e.ReceivedObject);
		}

		private static void Program_PersonObjectReceived(object sender, DataReceivedEventArgs e)
		{
			Console.WriteLine("Person object received from the server");
			var per = (Person)e.ReceivedObject;
			Console.WriteLine("Firstname: " + per.FirstName);
			Console.WriteLine("Lastname : " + per.LastName);
			WriteMetadata(e.Metadata);
		}

		private static void Cuscallback(string obj) {
			Console.WriteLine(obj);
		}

		private static void BindEvents(SimpleClient client) {
			client.MessageReceived += Client_MessageReceived;
			client.ConnectedToServer += Client_ConnectedToServer;
			client.DisconnectedFromServer += Client_DisconnectedFromServer;
			client.ObjectReceived += Client_ObjectReceived;
			client.Logger += Logger;
		}

		private static void Client_ObjectReceived(object sender, SimpleSockets.Client.ObjectReceivedEventArgs e)
		{
			if (e.ReceivedObjectType == typeof(Person))
			{
				Console.WriteLine("Person object received from the server");
				var per = (Person)e.ReceivedObject;
				Console.WriteLine("Firstname: " + per.FirstName);
				Console.WriteLine("Lastname : " + per.LastName);
			}
			WriteMetadata(e.Metadata);
		}

		private static void Client_DisconnectedFromServer(object sender, EventArgs e)
		{
			Console.WriteLine("Disconnected from the server.");
		}

		private static void Client_ConnectedToServer(object sender, EventArgs e)
		{
			_connected.Set();
			Console.WriteLine("Connected to the server.");
		}

		private static void Logger(string obj)
		{
			// Console.WriteLine();
			Console.WriteLine(obj);
		}

		private static void Client_MessageReceived(object sender, SimpleSockets.Client.MessageReceivedEventArgs e)
		{
			Console.WriteLine("Message: " + e.Message);
			WriteMetadata(e.Metadata);
		}

		private static void WriteMetadata(IDictionary<object, object> metadata) {
			if (metadata != null)
			{
				Console.WriteLine("Message contains metadata.");
				foreach (var item in metadata)
				{
					Console.WriteLine("Key  : " + item.Key);
					Console.WriteLine("Value: " + item.Value);
				}
			}
		}

	}
}
