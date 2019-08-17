using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using NetCore.Console.Server.MessageContracts;
using SimpleSockets;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;

namespace NetCore.Console.Server
{
	public class Server
	{
		private static SimpleSocketListener _listener;
		private static MessageA _messageAContract;
		private static bool _encrypt;

		private static void Main(string[] args)
		{
			_encrypt = false;
			_listener = new SimpleSocketTcpListener() {AllowReceivingFiles = true};
			_listener.TempPath = @"D:\Torrents\";

			_messageAContract = new MessageA("MessageAHeader");
			_listener.AddMessageContract(_messageAContract);
			_messageAContract.OnMessageReceived += MessageAContractOnOnMessageReceived;

			BindEvents();
			_listener.StartListening(13000);



			while (true)
			{
				Options();


				WriteLine("Press any key to continue...");
				System.Console.Read();
				System.Console.Clear();
			}

		}

		// Handles the MessageContractA
		private static void MessageAContractOnOnMessageReceived(SimpleSocket socket, int clientId, object message, string header)
		{
			WriteLine("Server received a MessageContract from the client with id " + clientId + " the header is : " + header + " and the message reads: " + message.ToString());
		}


		private static void Options()
		{
			System.Console.Clear();
			if (_listener.IsRunning)
			{
				WriteLine("Choose what type of message you want to send.");
				WriteLine("    - Message   (M)");
				WriteLine("    - Custom    (C)");
				WriteLine("    - File      (F)");
				WriteLine("    - Directory (D)");
				WriteLine("    - Contract  (B)");
				Write("Enter your chosen type: ");

				var option = System.Console.ReadLine();

				if (option != null)
					switch (option.ToUpper())
					{
						case "M":
							SendMessage();
							break;
						case "C":
							SendCustom();
							break;
						case "F":
							SendFile();
							break;
						case "D":
							SendFolder();
							break;
						case "B":
							SendMessageContract();
							break;
						default:
							Options();
							break;
					}
				else
				{
					Options();
				}

			}
		}

		private static int ShowClients()
		{
			var ids = new List<int>();
			var clients = _listener.GetConnectedClients();
			foreach (var client in clients)
			{
				ids.Add(client.Value.Id);
				WriteLine("Client ID: " + client.Value.Id + " with IPv4 : " + client.Value.RemoteIPv4);
			}

			Write("Enter the id of the client (E) to exit... ");
			var chosen = System.Console.ReadLine();

			if (chosen != null && chosen.ToUpper() == "E")
				return 0;

			if (!ids.Contains(int.Parse(chosen)))
			{
				chosen = ShowClients().ToString();
			}

			return int.Parse(chosen);

		}

		private static void SendMessage()
		{
			System.Console.Clear();
			var id = ShowClients();


			Write("Enter your message you want to send to the server...  ");
			var message = System.Console.ReadLine();


			_listener.SendMessage(id, message, _encrypt, false, false);
		}

		private static void SendMessageContract()
		{
			System.Console.Clear();
			var id = ShowClients();


			Write("Press enter to send a MessageContract...  ");
			System.Console.ReadLine();

			_listener.SendMessageContract(id, _messageAContract, _encrypt, false, false);
		}

		private static void SendCustom()
		{
			System.Console.Clear();
			var id = ShowClients();

			Write("Enter the header you want to use for the transmission...  ");
			var header = System.Console.ReadLine();

			Write("Enter the message you want to send...  ");
			var message = System.Console.ReadLine();

			_listener.SendCustomHeader(id, message, header, _encrypt, false, false);
		}

		private static void SendFile()
		{
			System.Console.Clear();
			var id = ShowClients();

			Write("Enter the path to the file you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the file should be stored... ");
			var targetPath = System.Console.ReadLine();

			_listener.SendFileAsync(id, path, targetPath, true, true, false);
			//_listener.SendFile(id,path, targetPath,_encrypt,true, false);
		}

		private static void SendFolder()
		{
			System.Console.Clear();
			var id = ShowClients();

			Write("Enter the path to the folder you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the folder should be stored... ");
			var targetPath = System.Console.ReadLine();

			_listener.SendFolder(id,path, targetPath,true, false);
		}

		#region Events

		private static void BindEvents()
		{
			//Events
			//_listener.ProgressFileTransfer += Progress;
			_listener.MessageReceived += MessageReceived;
			_listener.MessageSubmitted += MessageSubmitted;
			_listener.CustomHeaderReceived += CustomHeaderReceived;
			_listener.ClientDisconnected += ClientDisconnected;
			//_listener.ClientConnected += ClientConnected;
			//_listener.FileReceived += FileReceived;
			_listener.ServerHasStarted += ServerHasStarted;
			_listener.MessageFailed += MessageFailed;
			_listener.ServerErrorThrown += ErrorThrown;
		}

		//*****Begin Events************///

		private static void CustomHeaderReceived(int id, string msg, string header)
		{
			WriteLine("The server received a message from the client with ID " + id + " the header is : " + header + " and the message is : " + msg);
		}

		private static void MessageReceived(int id, string msg)
		{
			WriteLine("The server has received a message from client " + id + " the message reads: " + msg);
		}

		private static void MessageSubmitted(int id, bool close)
		{
			WriteLine("A message has been sent to client " + id);
		}

		private static void FileReceived(int id, string path)
		{
			WriteLine("The server has received a file from client " + id + " the file/folder is stored at : " + path);
		}

		private static ProgressBar progress;
		private static void Progress(int id, int bytes, int messageSize)
		{
			if (progress == null)
			{
				progress = new ProgressBar();
				System.Console.Write("Receiving a file/folder... ");
			}

			var progressDouble = ((double) bytes / messageSize);

			progress.Report(progressDouble);

			if (progressDouble >= 1.00)
			{
				progress.Dispose();
				progress = null;
				Thread.Sleep(200);
				WriteLine(" Done.");
			}
		}

		private static void ServerHasStarted()
		{
			WriteLine("The Server has started.");
		}

		private static void ErrorThrown(Exception exception)
		{
			WriteLine("The server has thrown an error. Message : " + exception.Message);
			WriteLine("Stacktrace: " + exception.StackTrace);
		}

		private static void MessageFailed(int id, byte[] messageData, Exception exception)
		{
			WriteLine("The server has failed to deliver a message to client " + id);
			WriteLine("Error message : " + exception.Message);
		}

		private static void ClientConnected(int id, ISocketInfo clientState)
		{
			WriteLine("Client " + id + " with IPv4 " + clientState.RemoteIPv4 + " has connected to the server.");
		}

		private static void ClientDisconnected(int id)
		{
			WriteLine("Client " + id + " has disconnected from the server.");
		}


		#endregion

		private static void Write(string text)
		{
			System.Console.Write(text);
		}

		private static void WriteLine(string text)
		{
			System.Console.WriteLine(text);
		}

	}
}
