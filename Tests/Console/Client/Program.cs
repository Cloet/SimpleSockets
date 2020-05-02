using Shared;
using SimpleSockets;
using SimpleSockets.Helpers.Compression;
using System;
using System.Collections.Generic;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
			Console.WriteLine("Hello.");
			var client = new SimpleTcpClient(false);
			BindEvents(client);
			client.CompressionMethod = CompressionType.Deflate;
			client.ConnectTo("127.0.0.1", 13000,5);
			

			while (true) {
				Console.Write("Enter a message: ");
				var msg = Console.ReadLine();
				client.SendMessage(msg);
			}

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
			if (e.ObjectType == typeof(Person))
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
