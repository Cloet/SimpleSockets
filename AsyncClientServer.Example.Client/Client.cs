using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncClientServer.Client;
using Cryptography;

namespace AsyncClientServer.Example.Client
{
	class Client
	{
		private static bool _connected;
		private static AsyncClient _client;
		static void Main(string[] args)
		{

			Console.Title = "Client";
			

			_client = new AsyncClient();
			BindEvents();
			Thread t = new Thread(StartClient);
			t.Start();

			//SendFile("SourcePath", "TargetPath", false);

			while (true)
			{

				if (_connected)
				{
					Console.Write("Enter message:");
					string msg = Console.ReadLine();
					_client.SendMessage(msg, false);
				}

			}

			Console.ReadLine();

		}

		private static void StartClient()
		{

			_client.StartClient("127.0.0.1", 13000);

		}

		private static void BindEvents()
		{
			_client.Connected += new ConnectedHandler(ConnectedToServer);
			_client.MessageReceived += new ClientMessageReceivedHandler(ServerMessageReceived);
			_client.MessageSubmitted += new ClientMessageSubmittedHandler(ClientMessageSubmitted);
			_client.FileReceived += new FileFromServerReceivedHandler(FileReceived);
			_client.Disconnected += new DisconnectedFromServerHandler(Disconnected);
		}

		/*Send messages*/
		private static void SendMessage(string msg, bool close)
		{
			_client.SendMessage(msg, close);
		}

		private static void SendFile(string fileLocation, string remoteLocation, bool close)
		{
			_client.SendFile(fileLocation, remoteLocation, close);
		}

		private static void SendObject(object anyObj, bool close)
		{
			_client.SendObject(anyObj, close);
		}

		/*Events*/
		//Client Events
		private static void ConnectedToServer(IAsyncClient a)
		{
			_connected = true;
			Console.WriteLine("Client has connected to server");
			a.SendMessage("Hello server, I'm the client.", false);
		}

		private static void ServerMessageReceived(IAsyncClient a,string header, string msg)
		{
			Console.WriteLine("Message received from the server: " + msg);
		}

		private static void FileReceived(IAsyncClient a, string file)
		{
			Console.WriteLine("File received and saved at: " + file);
		}

		private static void Disconnected(string ip, int port)
		{
			Console.WriteLine("Client has disconnected from server with ip: " + ip + " and port " + port);
		}

		private static void ClientMessageSubmitted(IAsyncClient a, bool close)
		{
			//if (close)
			//{
			//	a.Dispose();
			//}
		}

	}
}
