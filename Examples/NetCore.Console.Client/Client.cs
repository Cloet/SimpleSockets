using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using AsyncClientServer;
using AsyncClientServer.Client;
using NetCore.Console.Client.MessageContracts;

namespace NetCore.Console.Client
{
	public class Client
	{

		private static SocketClient _client;
		private static bool _encrypt;
		private static MessageA _messageAContract;

		private static void Main(string[] args)
		{
			_encrypt = false;
			_client = new AsyncSocketClient {AllowReceivingFiles = true};

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
		private static void MessageAContractOnOnMessageReceived(AsyncSocket client,int clientId, object message, string header)
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

			_client.SendMessage(message, _encrypt, false);
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

			_client.SendCustomHeaderMessage(message, header, _encrypt, false);
		}

		private static void SendFile()
		{
			System.Console.Clear();
			Write("Enter the path to the file you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the file should be stored... ");
			var targetPath = System.Console.ReadLine();
			_client.SendFile(path, targetPath,_encrypt,true, false);
		}

		private static void SendFolder()
		{
			System.Console.Clear();
			Write("Enter the path to the folder you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the folder should be stored... ");
			var targetPath = System.Console.ReadLine();
			_client.SendFolder(path, targetPath,_encrypt, false);
		}


		#region Events

		private static void BindEvents()
		{
			_client.ProgressFileReceived += Progress;
			_client.ConnectedToServer += ConnectedToServer;
			_client.ClientErrorThrown += ErrorThrown;
			_client.MessageReceived += ServerMessageReceived;
			_client.MessageSubmitted += ClientMessageSubmitted;
			_client.FileReceived += FileReceived;
			_client.DisconnectedFromServer += Disconnected;
			_client.MessageFailed += MessageFailed;
			_client.CustomHeaderReceived += CustomHeader;
		}

		private static void CustomHeader(SocketClient a, string msg, string header)
		{
			WriteLine("Bytes received from server with header = " + header + " and message = " + msg);
		}


		private static void ErrorThrown(SocketClient socketClient, Exception error)
		{
			WriteLine("The client has thrown an error: " + error.Message);
			WriteLine("Stacktrace: " + error.StackTrace);
		}

		private static void ConnectedToServer(SocketClient a)
		{
			WriteLine("The client has connected to the server on ip " + a.Ip);
		}

		private static void ServerMessageReceived(SocketClient a, string msg)
		{
			WriteLine("Message received from the server: " + msg);
		}

		private static void FileReceived(SocketClient a, string file)
		{
			WriteLine("The client has received a File/Folder and has saved this File/Folder at path :  " + file);
		}

		private static void Disconnected(SocketClient a, string ip, int port)
		{
			WriteLine("The client has disconnected from the server with ip " + a.Ip + "on port " + a.Port);
		}

		private static ProgressBar progress;
		private static void Progress(SocketClient client, int bytes, int messageSize)
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

		private static void ClientMessageSubmitted(SocketClient a, bool close)
		{
			WriteLine("The client has submitted a message to the server.");
		}

		private static void MessageFailed(SocketClient tcpClient, byte[] messageData, Exception exception)
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
