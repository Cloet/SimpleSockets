using System;
using System.Collections.Generic;
using System.Text;
using Shared;
using SimpleSockets;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Server;

namespace Server
{
    class Program
    {
		private static SimpleTcpServer _server;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting TCP Server.");
            _server = new SimpleTcpServer(false);

            _server.LoggerLevel = LogLevel.Debug;
			BindEvents(_server);
			_server.CompressionMethod = CompressionType.Deflate;

			_server.Listen(13000);

			while (true) {
				Process(Choice());
			}
        }

		private static void Process(string input) {

			input = input.Trim().ToLower();

			if (input == "msgmd" || input == "msg")
			{
				var clientid = GetClient();

				if (clientid < 0)
					return;

				Console.Write("Enter a message: ");
				var msg = Console.ReadLine();
				IDictionary<object, object> md = null;

				if (input == "msgmd")
					md = Metadata();

				_server.SendMessage(clientid, msg, md);
			}
			else if (input == "obj" || input == "objmd") {
				var clientid = GetClient();

				if (clientid < 0)
					return;

				Console.WriteLine("Creating new 'Person' object");
				Console.Write("Enter a firstname:");
				var fname = Console.ReadLine();
				Console.Write("Enter a lastname:");
				var lname = Console.ReadLine();

				IDictionary<object, object> md = null;

				if (input == "objmd")
					md = Metadata();

				_server.SendObject(clientid, new Person(fname, lname), md);

			}
			else if (input == "h" || input == "?")
			{
				var stb = new StringBuilder();
				stb.Append("Possible commands:" + Environment.NewLine);
				stb.Append("\tmsg\t\tSend a message to the server." + Environment.NewLine);
				stb.Append("\tmsgmd\t\tSend a message with metadata to the server." + Environment.NewLine);
				stb.Append("\tobj\t\tSends a test object to the server." + Environment.NewLine);
				stb.Append("\tobjmd\t\tSend a test object with metadata to the server." + Environment.NewLine);
				stb.Append("\tclear\t\tClears the terminal." + Environment.NewLine);
				stb.Append("\trestart\t\tRestarts the server." + Environment.NewLine);
				stb.Append("\tquit\t\tClose the server and terminal." + Environment.NewLine);
				Console.WriteLine(stb.ToString());
			}
			else if (input == "clear")
				Console.Clear();
			else if (input == "quit")
			{
				_server.Dispose();
				Environment.Exit(0);
			}
			else if (input.Trim() == "")
				Console.WriteLine("");
			else
				Console.WriteLine("Invalid input try again.");			
		}

		private static IDictionary<object, object> Metadata() {
			IDictionary<object, object> md = null;

			Console.WriteLine("Enter metadata (q to quit)");

			while (true) {
				if (md != null) {
					Console.WriteLine(md.Count + " pieces of metadata.");
					Console.WriteLine("quit - To quit adding metadata.");
					Console.Write(">");
					var resp = Console.ReadLine();

					if (resp.ToLower().Trim() == "quit")
						return md;
				}

				Console.Write("key: ");
				var key = Console.ReadLine();

				if (key.ToLower().Trim() == "q")
					return md;

				Console.Write("value: ");
				var value = Console.ReadLine();

				if (value.ToLower().Trim() == "q")
					return md;

				if (md == null)
					md = new Dictionary<object, object>();

				md.Add(key, value);
			}
		}

		private static string Choice() {
			Console.WriteLine("Command [h|? for help]");
			Console.Write("> ");
			return Console.ReadLine();
		}

		private static void BindEvents(SimpleTcpServer server) {
			server.MessageReceived += Server_MessageReceived;
			server.ClientConnected += ClientConnected;
			server.ObjectReceived += Server_ObjectReceived;
			server.Logger += Logger;
		}

		private static void Server_ObjectReceived(object sender, ClientObjectReceivedEventArgs e)
		{
			if (e.GetType() == typeof(Person)) {
				Console.WriteLine("Person object received from client " + e.ClientInfo.Id);
				var per = (Person) e.ReceivedObject;
				Console.WriteLine("Firstname: " + per.FirstName);
				Console.WriteLine("Lastname : " + per.LastName);
			}
			WriteMetadata(e.Metadata);
		}

		private static int GetClient() {
			var clients = _server.GetAllClients();

			if (clients == null || clients.Count == 0) {
				Console.WriteLine("No connected clients.");
				return -1;
			}

			Console.WriteLine("All clients:");
			foreach (var client in clients)
				Console.WriteLine("\t ID:"+client.Id + " IPv4:" + client.IPv4);

			Console.Write("select a client: ");
			var c = Console.ReadLine();

			var worked = int.TryParse(c, out var result);

			if (!worked) {
				Console.WriteLine("Please enter a valid number.");
				return GetClient();
			}

			var cl = _server.GetClientInfoById(result);

			if (cl == null) {
				Console.WriteLine("Invalid client id.");
				return GetClient();
			}

			return result;
		}

		private static void Server_MessageReceived(object sender, ClientMessageReceivedEventArgs e)
		{
			Console.WriteLine();
			Console.WriteLine("Client " + e.ClientInfo.Id + ": " + e.Message);
			WriteMetadata(e.Metadata);
		}

		private static void Logger(string obj)
        {
			Console.WriteLine();
            Console.WriteLine(obj);
        }

        private static void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
			Console.WriteLine();
            Console.WriteLine("Client has connected:" + e.ClientInfo.Id);
        }

		private static void WriteMetadata(IDictionary<object, object> metadata)
		{
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
