using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using AsyncClientServer.Client;

namespace NetCore.Console.Client
{
	public class Client
	{

		private static SocketClient _client;

		private static void Main(string[] args)
		{
			_client = new AsyncSocketClient();
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

			_client.SendMessage(message, false);
		}

		private static void SendCustom()
		{
			System.Console.Clear();
			Write("Enter the header you want to use for the transmission...  ");
			var header = System.Console.ReadLine();

			Write("Enter the message you want to send...  ");
			var message = System.Console.ReadLine();

			_client.SendCustomHeaderMessage(message, header, false);
		}

		private static void SendFile()
		{
			System.Console.Clear();
			Write("Enter the path to the file you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the file should be stored... ");
			var targetPath = System.Console.ReadLine();
			_client.SendFile(path, targetPath, false);
		}

		private static void SendFolder()
		{
			System.Console.Clear();
			Write("Enter the path to the folder you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the folder should be stored... ");
			var targetPath = System.Console.ReadLine();
			_client.SendFolder(path, targetPath, false);
		}


		#region Events

		private static void BindEvents()
		{
			_client.ProgressFileReceived += new ProgressFileTransferHandler(Progress);
			_client.Connected += new ConnectedHandler(ConnectedToServer);
			_client.ClientErrorThrown += new ClientErrorThrownHandler(ErrorThrown);
			_client.MessageReceived += new ClientMessageReceivedHandler(ServerMessageReceived);
			_client.MessageSubmitted += new ClientMessageSubmittedHandler(ClientMessageSubmitted);
			_client.FileReceived += new FileFromServerReceivedHandler(FileReceived);
			_client.Disconnected += new DisconnectedFromServerHandler(Disconnected);
			_client.MessageFailed += new DataTransferFailedHandler(MessageFailed);
			_client.CustomHeaderReceived += new ClientCustomHeaderReceivedHandler(CustomHeader);
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

		private static void MessageFailed(SocketClient tcpClient, byte[] messageData, string exceptionMessage)
		{
			WriteLine("The client has failed to send a message.");
			WriteLine("Error: " + exceptionMessage);
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
