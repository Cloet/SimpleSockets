using Shared;
using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Helpers.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Client
{
	class Program
	{
		private static event EventHandler<DataReceivedEventArgs> PersonObjectReceived;
		private static event EventHandler<DataReceivedEventArgs> CustomMessageReceived;

		static void Main(string[] args)
		{
			Console.WriteLine("Hello.");

			var cert = new X509Certificate2(new SocketHelper().GetCertFileContents(), "Password");

			// var client = new SimpleTcpClient();
			var clients = new List<SimpleTcpClient>();
			//var client = new SimpleTcpClient(cert);
			// BindEvents(client);
			// client.CompressionMethod = CompressionType.Deflate;

			for (int i = 0; i < 1; i++)
			{
				var client = new SimpleTcpClient(cert);
				BindEvents(client);
				client.ConnectTo("127.0.0.1", 13000, 5);
				clients.Add(client);
			}



			PersonObjectReceived += Program_PersonObjectReceived;
			CustomMessageReceived += Program_CustomMessageReceived;
			// client.DynamicCallbacks.Add("PersonObject", PersonObjectReceived);
			// client.DynamicCallbacks.Add("CustomMessage", CustomMessageReceived);

			while (true) {
				Console.Write("Enter a message: ");
				var msg = Console.ReadLine();
				foreach (var c in clients)
				{
					c.SendMessage(msg);
				}
				// client.SendMessage(msg);
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

		private static void BindEvents(SimpleTcpClient client) {
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
			Console.WriteLine("Connected to the server.");
		}

		private static void Logger(string obj)
		{
			Console.WriteLine();
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
