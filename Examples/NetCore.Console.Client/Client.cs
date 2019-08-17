using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using NetCore.Console.Client.MessageContracts;
using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Messaging;

namespace NetCore.Console.Client
{
	public class Client
	{

		private static SimpleSocketClient _client;
		private static bool _encrypt;
		private static MessageA _messageAContract;

		private static void Main(string[] args)
		{
			_encrypt = false;
			_client = new SimpleSocketTcpClient() {AllowReceivingFiles = true};
			_client.TempPath = @"D:\Torrents";

			//Create the MessageContract implementation and add to the client
			_messageAContract = new MessageA("MessageAHeader");
			_client.AddMessageContract(_messageAContract);
			//Bind MessageContract Event
			_messageAContract.OnMessageReceived += MessageAContractOnOnMessageReceived;

			BindEvents();
			_client.StartClient("127.0.0.1", 13000);
			

			while (true)
			{
				Options();


				WriteLine("Press any key to continue...");
				System.Console.Read();
				System.Console.Clear();
			}

		}

		//Event for MessageContract (MessageA)
		//The clientId is only used on the server side. Here it will return -1
		private static void MessageAContractOnOnMessageReceived(SimpleSocket client,int clientId, object message, string header)
		{
			WriteLine("MessageContract received with header: " + header + " and with message " + message.ToString());
		}


		private static void Options()
		{
			System.Console.Clear();
			if (_client.IsConnected())
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

		private static void SendMessage()
		{
			System.Console.Clear();
			Write("Enter your message you want to send to the server...  ");
			var message = System.Console.ReadLine();

			_client.SendMessage(message, _encrypt, false, false);
		}

		private static void SendMessageContract()
		{
			System.Console.Clear();
			Write("Press enter to send a MessageContract...  ");
			System.Console.ReadLine();

			_client.SendMessageContract(_messageAContract, _encrypt, false);
		}

		private static void SendCustom()
		{
			System.Console.Clear();
			Write("Enter the header you want to use for the transmission...  ");
			var header = System.Console.ReadLine();

			Write("Enter the message you want to send...  ");
			var message = System.Console.ReadLine();

			_client.SendCustomHeader(message, header, _encrypt, false);
		}

		private static void SendFile()
		{
			System.Console.Clear();
			Write("Enter the path to the file you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the file should be stored... ");
			var targetPath = System.Console.ReadLine();
			_client.SendFileAsync(path, targetPath, true,true,false);
			//_client.SendFile(path, targetPath,_encrypt,true, false);
		}

		private static void SendFolder()
		{
			System.Console.Clear();
			Write("Enter the path to the folder you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the folder should be stored... ");
			var targetPath = System.Console.ReadLine();
			_client.SendFolder(path, targetPath,true, false);
		}


		#region Events

		private static void BindEvents()
		{
			//_client.ProgressFileReceived += Progress;
			_client.FileReceiver += ClientOnFileReceiver;
			_client.FolderReceiver += ClientOnFolderReceiver;
			_client.DisconnectedFromServer += Disconnected;
			_client.MessageUpdateFileTransfer += ClientOnMessageUpdateFileTransfer;
			_client.MessageUpdate += ClientOnMessageUpdate;
			_client.ConnectedToServer += ConnectedToServer;
			_client.ClientErrorThrown += ErrorThrown;
			_client.MessageReceived += ServerMessageReceived;
			_client.MessageSubmitted += ClientMessageSubmitted;
			_client.MessageFailed += MessageFailed;
			_client.CustomHeaderReceived += CustomHeader;
			_client.ObjectReceived += ClientOnObjectReceived;
		}

		private static void ClientOnObjectReceived(SimpleSocketClient a, object obj, Type objType)
		{
			WriteLine("Received an object of type = " + objType.FullName);
		}

		private static void ClientOnMessageUpdate(SimpleSocketClient a, string msg, string header, MessageType msgType, MessageState state)
		{
			WriteLine("Sending message to client: msg = " + msg + ", header = " + header);
		}

		private static void ClientOnMessageUpdateFileTransfer(SimpleSocketClient a, string origin, string loc, double percentageDone, MessageState state)
		{
			WriteLine("Sending message to client: " + percentageDone);
		}

		private static void ClientOnFolderReceiver(SimpleSocketClient a, int currentPart, int totalParts, string loc, MessageState state)
		{
			if (state == MessageState.Decrypting)
				WriteLine("Decrypting Folder this might take a while.");
			if (state == MessageState.Decompressing)
				WriteLine("Decompressing the Folder this might take a while.");
			if (state == MessageState.DecompressingDone)
				WriteLine("Decompressing has finished.");
			if (state == MessageState.DecryptingDone)
				WriteLine("Decrypting has finished.");
			if (state == MessageState.Completed)
				WriteLine("Folder received and stored at location: " + loc);
		}

		private static void ClientOnFileReceiver(SimpleSocketClient a, int currentPart, int totalParts, string loc, MessageState state)
		{
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

		private static void CustomHeader(SimpleSocket a, string msg, string header)
		{
			WriteLine("Bytes received from server with header = " + header + " and message = " + msg);
		}


		private static void ErrorThrown(SimpleSocket socketClient, Exception error)
		{
			WriteLine("The client has thrown an error: " + error.Message);
			WriteLine("Stacktrace: " + error.StackTrace);
		}

		private static void ConnectedToServer(SimpleSocket a)
		{
			WriteLine("The client has connected to the server on ip " + a.Ip);
		}

		private static void ServerMessageReceived(SimpleSocket a, string msg)
		{
			WriteLine("Message received from the server: " + msg);
		}

		private static void FileReceived(SimpleSocket a, string file)
		{
			WriteLine("The client has received a File/Folder and has saved this File/Folder at path :  " + file);
		}

		private static void Disconnected(SimpleSocket a)
		{
			WriteLine("The client has disconnected from the server with ip " + a.Ip + "on port " + a.Port);
		}

		private static ProgressBar progress;
		private static void Progress(SimpleSocket client, int bytes, int messageSize)
		{
			if (progress == null)
			{
				progress = new ProgressBar();
				System.Console.Write("Receiving a file/folder... ");
			}

			var progressDouble = ((double)bytes / messageSize);

			progress.Report(progressDouble);

			if (progressDouble >= 1.00)
			{
				progress.Dispose();
				progress = null;
				Thread.Sleep(200);
				WriteLine(" Done.");
			}
		}

		private static void ClientMessageSubmitted(SimpleSocket a, bool close)
		{
			WriteLine("The client has submitted a message to the server.");
		}

		private static void MessageFailed(SimpleSocket tcpClient, byte[] messageData, Exception exception)
		{
			WriteLine("The client has failed to send a message.");
			WriteLine("Error: " + exception);
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
