using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using MessageTesting;
using SimpleSockets;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;

namespace NetCore.Console.Server
{
	public class Server
	{
		private static SimpleSocketListener _listener;
		private static MessageA _messageAContract;
		private static bool _encrypt;
		private static bool _compress;
		private static ProgressBar progress;

		private static void Main(string[] args)
		{
			_encrypt = false;
			_compress = false;

			var xmlSer = new XmlSerialization();
			var binSer = new BinarySerializer();
			
			//_listener = new SimpleSocketUdpListener();
			//_listener = new SimpleSocketTcpListener();
			_listener = new SimpleSocketTcpSslListener(@"C:\Users\Cloet\Desktop\test.pfx", "Password");

			_listener.ObjectSerializer = binSer;
			_listener.AllowReceivingFiles = true;
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
		private static void MessageAContractOnOnMessageReceived(SimpleSocket socket, IClientInfo clientInfo, object message, string header)
		{
			WriteLine("Server received a MessageContract from the client with id " + clientInfo.Id + " the header is : " + header + " and the message reads: " + message.ToString());
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
				WriteLine("    - Object    (O)");
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
						case "O":
							SendObject();
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

		private static async void SendMessage()
		{
			System.Console.Clear();
			var id = ShowClients();


			Write("Enter your message you want to send to the server...  ");
			var message = System.Console.ReadLine();


			await _listener.SendMessageAsync(id, message, _compress, _encrypt, false);
		}

		private static async void SendMessageContract()
		{
			System.Console.Clear();
			var id = ShowClients();


			Write("Press enter to send a MessageContract...  ");
			System.Console.ReadLine();

			await _listener.SendMessageContractAsync(id, _messageAContract, _compress, _encrypt, false);
		}

		private static async void SendCustom()
		{
			System.Console.Clear();
			var id = ShowClients();

			Write("Enter the header you want to use for the transmission...  ");
			var header = System.Console.ReadLine();

			Write("Enter the message you want to send...  ");
			var message = System.Console.ReadLine();

			await _listener.SendCustomHeaderAsync(id, message, header, _compress, _encrypt, false);
		}

		private static async void SendFile()
		{
			System.Console.Clear();
			var id = ShowClients();

			Write("Enter the path to the file you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the file should be stored... ");
			var targetPath = System.Console.ReadLine();

			await _listener.SendFileAsync(id, path, targetPath, _compress, _encrypt, false);
			//_listener.SendFile(id,path, targetPath,_encrypt,true, false);
		}

		private static async void SendFolder()
		{
			System.Console.Clear();
			var id = ShowClients();

			Write("Enter the path to the folder you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the folder should be stored... ");
			var targetPath = System.Console.ReadLine();

			await _listener.SendFolderAsync(id,path, targetPath,true, false);
		}

		private static async void SendObject()
		{
			System.Console.Clear();
			var id = ShowClients();

			var person = new Person("Test", "FirstName", "5th Avenue");

			WriteLine("Press enter to send an object.");
			System.Console.ReadLine();

			await _listener.SendObjectAsync(id, person);
		}


		#region Events

		private static void BindEvents()
		{
			//Events
			_listener.AuthFailure += ListenerOnAuthFailure;
			_listener.AuthSuccess += ListenerOnAuthSuccess;
			_listener.FileReceiver += ListenerOnFileReceiver;
			_listener.FolderReceiver += ListenerOnFolderReceiver;
			_listener.MessageReceived += MessageReceived;
			_listener.MessageSubmitted += MessageSubmitted;
			_listener.CustomHeaderReceived += CustomHeaderReceived;
			_listener.ClientDisconnected += ClientDisconnected;
			_listener.ClientConnected += ClientConnected;
			_listener.ServerHasStarted += ServerHasStarted;
			_listener.MessageFailed += MessageFailed;
			_listener.ServerErrorThrown += ErrorThrown;
			_listener.ObjectReceived += ListenerOnObjectReceived;
			_listener.MessageUpdateFileTransfer += ListenerOnMessageUpdateFileTransfer;
			_listener.MessageUpdate += ListenerOnMessageUpdate;

		}

		private static void ListenerOnAuthFailure(IClientInfo client)
		{
			WriteLine("Server failed to authenticate certificate of client " + client.Id + " " + client.Guid + ".");
		}

		private static void ListenerOnAuthSuccess(IClientInfo client)
		{
			WriteLine("Server authenticate certificate of client " + client.Id + " " + client.Guid + ".");
		}

		//*****Begin Events************///

		private static void ListenerOnMessageUpdate(IClientInfo client, string msg, string header, MessageType msgType, MessageState state)
		{
			//WriteLine("Sending message to client: msg = " + msg + ", header = " + header);
		}

		private static void ListenerOnMessageUpdateFileTransfer(IClientInfo client, string origin, string loc, double percentageDone, MessageState state)
		{
			//WriteLine("Sending message to client: " + percentageDone);

		}

		private static void ListenerOnObjectReceived(IClientInfo client, object obj, Type objType)
		{
			WriteLine("Received an object of type = " + objType.FullName);

			if (obj.GetType() == typeof(Person))
			{
				var p = (Person) obj;
				WriteLine("Person: ");
				WriteLine("Name:" + p.Name);
				WriteLine("Firstname:" + p.FirstName);
				WriteLine("Street: " + p.Street);
			}

		}

		private static void ListenerOnFolderReceiver(IClientInfo client, int currentPart, int totalParts, string loc, MessageState state)
		{
			if (state == MessageState.ReceivingData)
			{
				if (progress == null)
				{
					progress = new ProgressBar();
					System.Console.Write("Receiving a Folder... ");
				}

				var progressDouble = ((double)currentPart / totalParts);

				progress.Report(progressDouble);

				if (progressDouble >= 1.00)
				{
					progress.Dispose();
					progress = null;
					Thread.Sleep(200);
					WriteLine("Folder Received.");
				}
			}



			if (state == MessageState.Decrypting)
				WriteLine("Decrypting Folder this might take a while.");
			if (state == MessageState.Decompressing)
				WriteLine("Decompressing the Folder this might take a while.");
			if (state == MessageState.DecompressingDone)
				WriteLine("Decompressing has finished.");
			if (state == MessageState.DecryptingDone)
				WriteLine("Decrypting has finished.");
			if (state == MessageState.Completed)
			{
				WriteLine("Folder received and stored at location: " + loc);
			}
		}

		private static void ListenerOnFileReceiver(IClientInfo client, int currentPart, int totalParts, string loc, MessageState state)
		{
			if (state == MessageState.ReceivingData)
			{
				if (progress == null)
				{
					progress = new ProgressBar();
					System.Console.Write("Receiving a File... ");
				}

				var progressDouble = ((double)currentPart / totalParts);

				progress.Report(progressDouble);

				if (progressDouble >= 1.00)
				{
					progress.Dispose();
					progress = null;
					Thread.Sleep(200);
					WriteLine("File Transfer Complete");
				}
			}

			if (state == MessageState.Decrypting)
				WriteLine("Decrypting File this might take a while.");
			if (state == MessageState.Decompressing)
				WriteLine("Decompressing the File this might take a while.");
			if (state == MessageState.DecompressingDone)
				WriteLine("Decompressing has finished.");
			if (state == MessageState.DecryptingDone)
				WriteLine("Decrypting has finished.");
			if (state == MessageState.Completed)
				WriteLine("File received and stored at location: " + loc);
		}

		private static void CustomHeaderReceived(IClientInfo client, string msg, string header)
		{
			WriteLine("The server received a message from the client with ID " + client.Id + " the header is : " + header + " and the message is : " + msg);
		}

		private static void MessageReceived(IClientInfo client, string msg)
		{
			WriteLine("The server has received a message from client " + client.Id + " with name : " + client.ClientName +" and guid : " + client.Guid);
			WriteLine("The client is running on " + client.OsVersion + " and UserDomainName = " + client.UserDomainName);

			WriteLine("The server has received a message from client " + client.Id + " the message reads: " + msg);
		}

		private static void MessageSubmitted(IClientInfo client, bool close)
		{
			WriteLine("A message has been sent to client " + client.Id);
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

		private static void MessageFailed(IClientInfo client, byte[] messageData, Exception exception)
		{
			WriteLine("The server has failed to deliver a message to client " + client.Id);
			WriteLine("Error message : " + exception.Message);
		}

		private static void ClientConnected(IClientInfo client)
		{
			WriteLine("Client " + client.Id + " with IPv4 " + client.RemoteIPv4 + " has connected to the server.");
		}

		private static void ClientDisconnected(IClientInfo client)
		{
			WriteLine("Client " + client.Id + " has disconnected from the server.");
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
