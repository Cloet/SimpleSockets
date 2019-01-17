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
		static void Main(string[] args)
		{
			Console.Title = "Server";
			BindEvents();
			Thread t = new Thread(StartServer);
			t.Start();

			Console.Read();
		}

		private static void StartServer()
		{
			AsyncSocketListener.Instance.StartListening(13000);
			Console.WriteLine("Server has started...");
			Console.WriteLine();
		}

		private static void BindEvents()
		{
			AsyncSocketListener.Instance.MessageReceived += new MessageReceivedHandler(MessageReceived);
			AsyncSocketListener.Instance.MessageSubmitted += new MessageSubmittedHandler(MessageSubmitted);
			AsyncSocketListener.Instance.ObjectReceived += new ObjectFromClientReceivedHandler(ObjectReceived);
			AsyncSocketListener.Instance.ClientDisconnected += new ClientDisconnectedHandler(ClientDisconnected);
			AsyncSocketListener.Instance.FileReceived += new FileFromClientReceivedHandler(FileReceived);
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

		/*Events*/
		private static void MessageReceived(int id, string msg)
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

		private static void ClientDisconnected(int id)
		{
			Console.WriteLine("Client "+ id + " has disconnected.");
		}


	}
}
