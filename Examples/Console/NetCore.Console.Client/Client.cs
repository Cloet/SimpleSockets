using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MessageTesting;
using Newtonsoft.Json;
using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;

namespace NetCore.Console.Client
{
	public class Client
	{

		private static SimpleSocketClient _client;
		private static bool _encrypt;
		private static bool _compress;
		private static MessageA _messageAContract;
		private static ProgressBar progress;

		private static void Main(string[] args)
		{
			_encrypt = true;
			_compress = true;

			var jsonSer = new MessageTesting.JsonSerialization();
			var xmlSer = new XmlSerialization();
			var binSer = new BinarySerializer();

			var cert = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(@"C:\Users\" + Environment.UserName + @"\Desktop\test.pfx")), "Password");

			_client = new SimpleSocketTcpClient();
			//_client = new SimpleSocketTcpSslClient(cert);

			_client.ObjectSerializer = jsonSer;
			_client.EnableExtendedAuth = true;
			_client.AllowReceivingFiles = true;


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
		private static void MessageAContractOnOnMessageReceived(SimpleSocket client,IClientInfo clientInfo, object message, string header)
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

		private static async void SendMessage()
		{
			System.Console.Clear();
			Write("Enter your message you want to send to the server...  ");
			var message = System.Console.ReadLine();

			await _client.SendMessageAsync(message, _compress, _encrypt, false);
		}

		private static async void SendMessageContract()
		{
			System.Console.Clear();
			Write("Press enter to send a MessageContract...  ");
			System.Console.ReadLine();

			await _client.SendMessageContractAsync(_messageAContract, _compress, _encrypt);
		}

		private static async void SendCustom()
		{
			System.Console.Clear();
			// Write("Enter the header you want to use for the transmission...  ");
			// var header = System.Console.ReadLine();

			Write("Enter the message you want to send...  ");
			var message = System.Console.ReadLine();

			var dictionary = new Dictionary<object, object>();

			dictionary.Add("Test", "This is a value");
			dictionary.Add("Test2", "This is a second value");

			await _client.SendMessageWithMetadataAsync("This is a message", dictionary);

			// await _client.SendCustomHeaderAsync(message, header, _compress, _encrypt);
		}

		private static async void SendFile()
		{
			System.Console.Clear();
			Write("Enter the path to the file you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the file should be stored... ");
			var targetPath = System.Console.ReadLine();
			await _client.SendFileAsync(path, targetPath, _compress,_encrypt,false);
		}

		private static async void SendFolder()
		{
			System.Console.Clear();
			Write("Enter the path to the folder you want to send to the server... ");
			var path = System.Console.ReadLine();

			Write("Enter the path on the server where the folder should be stored... ");
			var targetPath = System.Console.ReadLine();
			await _client.SendFolderAsync(path, targetPath,_encrypt, false);
		}

		private static async void SendObject()
		{
			System.Console.Clear();

			var person = new Person("TestFromClient", "FirstName", "5th Avenue");

			WriteLine("Press enter to send an object.");
			System.Console.ReadLine();

			await _client.SendObjectAsync(person);
		}

		#region Events

		private static void BindEvents()
		{
			//_client.ProgressFileReceived += Progress;
			_client.SslAuthStatus += ClientOnAuthStatus;
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
			_client.MessageWithMetadataReceived += CustomHeader;
			_client.ObjectReceived += ClientOnObjectReceived;
		}

		private static void ClientOnAuthStatus(AuthStatus status)
		{
			if (status == AuthStatus.Failed)
				WriteLine("Failed to authenticate.");
			if (status == AuthStatus.Success)
				WriteLine("Authenticated with success.");
		}

		private static void ClientOnObjectReceived(SimpleSocketClient a, object obj, Type objType)
		{
			WriteLine("Received an object of type = " + objType.FullName);
			if (obj.GetType() == typeof(Person))
			{
				var p = (Person)obj;
				WriteLine("Person: ");
				WriteLine("Name:" + p.Name);
				WriteLine("Firstname:" + p.FirstName);
				WriteLine("Street: " + p.Street);
			}
		}

		private static void ClientOnMessageUpdate(SimpleSocketClient a, string msg, string header, MessageType msgType, MessageState state)
		{
			// WriteLine("Sending message to client: msg = " + msg + ", header = " + header);
		}

		private static void ClientOnMessageUpdateFileTransfer(SimpleSocketClient a, string origin, string loc, double percentageDone, MessageState state)
		{
			WriteLine("Sending message to server: " + percentageDone + "%");
		}

		private static void ClientOnFolderReceiver(SimpleSocketClient a, int currentPart, int totalParts, string loc, MessageState state)
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

		private static void ClientOnFileReceiver(SimpleSocketClient a, int currentPart, int totalParts, string loc, MessageState state)
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

		private static void CustomHeader(SimpleSocket a, object msg, IDictionary<object, object> dict, Type objectType)
		{
			WriteLine("Test");
			// WriteLine("Bytes received from server with header = " + header + " and message = " + msg);
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

		private static void Disconnected(SimpleSocket a)
		{
			WriteLine("The client has disconnected from the server with ip " + a.Ip + "on port " + a.Port);
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
