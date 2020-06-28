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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
	class Program
	{
		private static event EventHandler<DataReceivedEventArgs> PersonObjectReceived;
		private static event EventHandler<DataReceivedEventArgs> CustomMessageReceived;

		private static IList<SimpleClient> Clients = null;

		static void Main(string[] args)
		{
			BootstrapClient();
		}

		private static void BootstrapClient() {
			Console.WriteLine("Starting client.");

			Clients = new List<SimpleClient>();

			var input = EnableSsl();
			var no_clients = Amount_Of_Clients();
			var context = new SslContext(new X509Certificate2(new SocketHelper().GetCertFileContents(), "Password"));	
			
			PersonObjectReceived += Program_PersonObjectReceived;
			CustomMessageReceived += Program_CustomMessageReceived;

			for (var i = 0; i < no_clients; i++) {
				StartClient(input == "y", context);
			}
			
			// Messaging
			while (true)
			{
				Process(Choice());
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
			
			client.LoggerLevel = LogLevel.Debug;
			

			client.ConnectTo("127.0.0.1", 13000, new TimeSpan(0,0,5));
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

		private static void Process(string input) {

			input = input.Trim().ToLower();

			if (input == "msgmd" || input == "msg")
			{

				Console.Write("Enter a message: ");
				var msg = Console.ReadLine();
				IDictionary<object, object> md = null;

				if (input == "msgmd")
					md = Metadata();
				
				foreach( var client in Clients) {
					client.SendMessage(msg,md);
				}
			}
			else if (input == "stats") {
				foreach (var client in Clients) {
					Console.WriteLine(client.Statistics.ToString());
				}
			}
			else if (input == "obj" || input == "objmd")
			{

				Console.WriteLine("Creating new 'Person' object");
				Console.Write("Enter a firstname: ");
				var fname = Console.ReadLine();
				Console.Write("Enter a lastname: ");
				var lname = Console.ReadLine();

				IDictionary<object, object> md = null;

				if (input == "objmd")
					md = Metadata();

				foreach( var client in Clients) {
					client.SendObject(new Person(fname, lname), md);
				}

			}
			else if (input == "h" || input == "?")
			{
				var stb = new StringBuilder();
				stb.Append("Possible commands:" + Environment.NewLine);
				stb.Append("\tmsg\t\tSend a message to the server." + Environment.NewLine);
				stb.Append("\tmsgmd\t\tSend a message with metadata to the server." + Environment.NewLine);
				stb.Append("\tobj\t\tSend a test object to the server." + Environment.NewLine);
				stb.Append("\tobjmd\t\tSend a test object with metadata to the server." + Environment.NewLine);
				stb.Append("\tclear\t\tClears the terminal." + Environment.NewLine);
				stb.Append("\trestart\t\tRestarts the server." + Environment.NewLine);
				stb.Append("\tstats\t\tStatistics of the server." + Environment.NewLine);
				stb.Append("\tquit\t\tClose the server and terminal." + Environment.NewLine);
				Console.WriteLine(stb.ToString());
			}
			else if (input == "clear")
				Console.Clear();
			else if (input == "quit")
			{
				foreach(var client in Clients) {
					client.Dispose();
				}
				Environment.Exit(0);
			}
			else if (input == "restart") {
				foreach(var client in Clients) {
					client.Dispose();
				}
				BootstrapClient();			
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
