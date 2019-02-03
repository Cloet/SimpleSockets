using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncClientServer.Helper;
using AsyncClientServer.Model;

namespace AsyncClientServer.Example.Server
{
	class Server
	{

		private static Boolean _started;
		static void Main(string[] args)
		{
			Console.Title = "Server";
			BindEvents();
			Thread t = new Thread(StartServer);
			t.Start();

			while (true)
			{

				if (_started)
				{
					Console.Write("Choose client");
					string id = Console.ReadLine();
					int idd = Int32.Parse(id);

					Console.WriteLine("Write a message");
					string msg = Console.ReadLine();


					AsyncSocketListener.Instance.SendMessage(idd, msg, false);
				}

			}

		}

		private static void StartServer()
		{
			Console.WriteLine("Server has started...");
			Console.WriteLine();
			AsyncSocketListener.Instance.StartListening(13000);

		}

		private static void BindEvents()
		{
			AsyncSocketListener.Instance.MessageReceived += new MessageReceivedHandler(MessageReceived);
			AsyncSocketListener.Instance.MessageSubmitted += new MessageSubmittedHandler(MessageSubmitted);
			AsyncSocketListener.Instance.ClientDisconnected += new ClientDisconnectedHandler(ClientDisconnected);
			AsyncSocketListener.Instance.FileReceived += new FileFromClientReceivedHandler(FileReceived);
			AsyncSocketListener.Instance.ServerHasStarted += new ServerHasStartedHandler(ServerHasStarted);
		}

		/*Send messages*/
		private static void SendMessage(int id, string msg, Boolean close)
		{
			AsyncSocketListener.Instance.SendMessage(id, msg, close);
		}

		private static void SendFile(int id, string fileLocation, string remoteLocation,Boolean close)
		{
			AsyncSocketListener.Instance.SendFile(id, fileLocation, remoteLocation, close);
		}

		private static void SendObject(int id, SerializableObject anyObj, Boolean close)
		{
			AsyncSocketListener.Instance.SendObject(id, anyObj, close);
		}

		private static void requestfileTransfer(int id, string location, string remoteloc)
		{
			SendMessage(id, "transfer" + "/" + "D:\\stay.mp3" + "/" + "D:\\copy-stay.mp3", false);
		}

		/*Events*/
		private static void MessageReceived(int id, string header,string msg)
		{
			AsyncSocketListener.Instance.SendMessage(id, "Received message", false);
			Console.WriteLine("Server received message from client " + id + ": " + msg);
		}

		private static void MessageSubmitted(int id, bool close)
		{
			Console.WriteLine("Server sent a message to client " + id);
		}

		private static void ObjectReceived(int id, string obj)
		{
			AsyncSocketListener.Instance.SendMessage(id, "Recieved message", false);
			Console.WriteLine("Server received an object from client " + id);
		}

		private static void FileReceived(int id, string path)
		{
			AsyncSocketListener.Instance.SendMessage(id, "Received", false);
			Console.WriteLine("Server received a file from client "+ id + " and is stored at " + path);
		}

		private static void ServerHasStarted()
		{
			_started = true;
		}

		private static void ClientDisconnected(int id)
		{
			Console.WriteLine("Client "+ id + " has disconnected.");
		}


	}
}
