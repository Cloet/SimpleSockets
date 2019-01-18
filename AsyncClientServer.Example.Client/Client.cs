using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncClientServer.Helper;
using AsyncClientServer.Model;

namespace AsyncClientServer.Example.Client
{
	class Client
	{
		private static Boolean _connected;
		private static AsyncClient _client;
		static void Main(string[] args)
		{

			Console.Title = "Client";
			

			_client = new AsyncClient();
			BindEvents();
			Thread t = new Thread(StartClient);
			t.Start();

			while(true){

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
			_client.ObjectReceived += new ObjectFromServerReceivedHandler(ObjectReceived);
		}

		/*Send messages*/
		private static void SendMessage(string msg, Boolean close)
		{
			_client.SendMessage(msg, close);
		}

		private static void SendFile(string fileLocation, string remoteLocation, Boolean close)
		{
			_client.SendFile(fileLocation, remoteLocation, close);
		}

		private static void SendObject(SerializableObject anyObj, Boolean close)
		{
			_client.SendObject(anyObj, close);
		}

		/*Events*/
		//Client Events
		private static void ConnectedToServer(AsyncClient a)
		{
			_connected = true;
			Console.WriteLine("Client has connected to server");
			a.SendMessage("Hello server, I'm the client.", false);
			a.Receive();
		}

		private static void ServerMessageReceived(AsyncClient a, String msg)
		{
			Console.WriteLine("Message received from the server: " + msg);
			a.Receive();
		}

		private static void ObjectReceived(string xml)
		{
			Console.WriteLine("Object received from the server: " + xml);
		}

		private static void FileReceived(string file)
		{
			Console.WriteLine("File received and saved at: " + file);
		}

		private static void ClientMessageSubmitted(AsyncClient a, bool close)
		{
			if (close)
			{
				a.Dispose();
			}
		}

	}
}
