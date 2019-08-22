using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.MessageContract;
using SimpleSockets.Messaging.Metadata;
using Timer = System.Threading.Timer;

namespace SimpleSockets.Client
{

	#region Delegates

	public delegate void ConnectedToServerDelegate(SimpleSocketClient client);

	public delegate void MessageReceivedDelegate(SimpleSocketClient client, string msg);

	public delegate void CustomHeaderReceivedDelegate(SimpleSocketClient client, string message, string header);

	public delegate void BytesReceivedDelegate(SimpleSocketClient client, byte[] messageBytes);

	public delegate void ObjectReceivedDelegate(SimpleSocketClient client, object obj, Type objType);

	public delegate void MessageUpdateFileTransferDelegate(SimpleSocketClient client, string origin, string remoteSaveLoc, double percentageDone, MessageState state);

	public delegate void MessageUpdateDelegate(SimpleSocketClient client, string message, string header, MessageType messageType, MessageState state);

	public delegate void FileReceiverDelegate(SimpleSocketClient client, int currentPart, int totalPart, string location, MessageState state);

	public delegate void FolderReceiverDelegate(SimpleSocketClient client, int currentPart, int totalPart, string location, MessageState state);

	public delegate void MessageSubmittedDelegate(SimpleSocketClient client, bool close);

	public delegate void DisconnectedFromServerDelegate(SimpleSocketClient client);

	public delegate void MessageFailedDelegate(SimpleSocketClient client, byte[] payLoad, Exception ex);

	public delegate void ClientErrorThrownDelegate(SimpleSocketClient client, Exception ex);

	public delegate void ClientLogsDelegate(SimpleSocketClient client, string message);

	#endregion

	public abstract class SimpleSocketClient: SimpleSocket
	{

		#region Vars

		//--Protected
		protected Socket Listener;
		protected bool CloseClient;
		protected readonly ManualResetEvent ConnectedMre = new ManualResetEvent(false);
		protected readonly ManualResetEvent SentMre = new ManualResetEvent(false);
		protected IPEndPoint Endpoint;
		protected static System.Timers.Timer KeepAliveTimer;
		protected bool Disposed = false;

		//--Private
		private bool _disconnectedInvoked;
		private string _clientGuid = "";

		//--Public
		/// <summary>
		/// This is how many seconds te client waits to try and reconnect to the server
		/// </summary>
		public int ReconnectInSeconds { get; protected set; }

		/// <summary>
		/// When true the client will send additional info to the server.
		/// Additional info : Client username, domainName
		/// </summary>
		public bool EnableExtendedAuth { get; set; }

		#endregion

		#region Events

		/// <summary>
		/// Event that triggers when a client is connected to server
		/// </summary>
		public event ConnectedToServerDelegate ConnectedToServer;

		/// <summary>
		/// Event that is triggered when a client receives a message from a server
		/// Format = SimpleSocketClient:MESSAGE
		/// </summary>
		public event MessageReceivedDelegate MessageReceived;

		/// <summary>
		/// Event that is triggered when a client receives a custom message from a server
		/// Format = SimpleSocketClient:MESSAGE:HEADER
		/// </summary>
		public event CustomHeaderReceivedDelegate CustomHeaderReceived;

		/// <summary>
		/// Event that is triggered when a client receives bytes from the connected server.
		/// </summary>
		public event BytesReceivedDelegate BytesReceived;

		/// <summary>
		/// Event that is triggered when the client receives an object from the server.
		/// </summary>
		public event ObjectReceivedDelegate ObjectReceived;

		/// <summary>
		/// Gives insight in the state of the current FileTransfer to the server.
		/// Format = Socket,OriginFile,RemoteSaveLocFile,PercentageDone,MessageState
		/// </summary>
		public event MessageUpdateFileTransferDelegate MessageUpdateFileTransfer;

		/// <summary>
		/// Gives insight in the state of the current message.
		/// </summary>
		public event MessageUpdateDelegate MessageUpdate;

		/// <summary>
		/// Event that is triggered when a client receives a file or a part of a file.
		/// Format = Socket,CurrentPart,TotalParts,PathToOutput,MessageState
		/// </summary>
		public event FileReceiverDelegate FileReceiver;

		/// <summary>
		/// Event that is triggered when a client receives a folder or a part of a folder.
		/// Format = Socket,CurrentPart,TotalParts,PathToOutput,MessageState
		/// </summary>
		public event FolderReceiverDelegate FolderReceiver;

		/// <summary>
		/// Event that is triggered when the client successfully has submitted a transmission of data.
		/// Format is ID:CLOSE
		/// The bool represents if the client has terminated after the message.
		/// </summary>
		public event MessageSubmittedDelegate MessageSubmitted;

		/// <summary>
		/// Event that is triggered when the client has disconnected from the server.
		/// Format = SimpleSocketClient
		/// </summary>
		public event DisconnectedFromServerDelegate DisconnectedFromServer;

		/// <summary>
		/// Event that is triggered when a client fails to send a message to the server
		/// Format = SimpleSocketClient:MessageType:MessageBytes,Exception
		/// </summary>
		public event MessageFailedDelegate MessageFailed;

		/// <summary>
		/// Event that is triggered when a client gives an error.
		/// </summary>
		public event ClientErrorThrownDelegate ClientErrorThrown;

		/// <summary>
		/// Event that receives logs.
		/// </summary>
		public event ClientLogsDelegate ClientLogs;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		protected SimpleSocketClient() : base()
		{
			KeepAliveTimer = new System.Timers.Timer(15000);
			KeepAliveTimer.Elapsed += KeepAlive;
			KeepAliveTimer.AutoReset = true;
			KeepAliveTimer.Enabled = false;

			if (EnableExtendedAuth)
				SendAuthMessage();
			else
				SendBasicAuthMessage();
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Starts the client.
		/// <para>requires server ip, port number and how many seconds the client should wait to try to connect again. Default is 5 seconds</para>
		/// </summary>
		public abstract void StartClient(string ipServer, int port, int reconnectInSeconds = 5);

		/// <summary>
		/// Check if client is connected to server
		/// </summary>
		/// <returns>bool</returns>
		public bool IsConnected()
		{
			try
			{
				return !((Listener.Poll(1000, SelectMode.SelectRead) && (Listener.Available == 0)) || !Listener.Connected);
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Closes the client.
		/// </summary>
		public void Close()
		{
			try
			{
				if (Disposed)
					return;

				ConnectedMre.Reset();
				TokenSource.Cancel();
				IsRunning = false;

				if (!IsConnected())
				{
					return;
				}

				Listener.Shutdown(SocketShutdown.Both);
				Listener.Close();
				Listener = null;
				RaiseDisconnected();
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		/// <summary>
		/// Safely close client and break all connections to server.
		/// </summary>
		public override void Dispose()
		{
			if (!Disposed)
			{
				Disposed = true;
				Close();
				ConnectedMre.Dispose();
				SentMre.Dispose();
				KeepAliveTimer.Enabled = false;
				//KeepAliveTimer.Dispose();
				GC.SuppressFinalize(this);
			}
		}

		#endregion

		#region Protected

		//Convert string to IPAddress
		protected IPAddress GetIp(string ip)
		{
			try
			{
				return Dns.GetHostAddresses(ip).First();
			}
			catch (SocketException se)
			{
				throw new Exception("Invalid server IP", se);
			}
			catch (Exception ex)
			{
				throw new Exception("Error trying to get a valid IPAddress from string : " + ip, ex);
			}
		}

		#endregion

		#region Private

		//Timer that tries reconnecting every x seconds
		private void KeepAlive(object source, ElapsedEventArgs e)
		{
			if (Token.IsCancellationRequested)
			{
				Close();
				ConnectedMre.Reset();
			}
			else if (!IsConnected())
			{
				Close();
				ConnectedMre.Reset();
				StartClient(Ip, Port, ReconnectInSeconds);
			}
		}

		#endregion

		#endregion
		
		#region SendingData

		//Sends message from queue
		protected abstract void BeginSendFromQueue(MessageWrapper message);

		//Loops the queue for messages that have to be sent.
		protected void SendFromQueue()
		{
			if (Disposed)
				return;

			while (!Token.IsCancellationRequested)
			{

				ConnectedMre.WaitOne();

				if (IsConnected())
				{
					BlockingMessageQueue.TryDequeue(out var message);
					BeginSendFromQueue(message);
				}
				else
				{
					Close();
					ConnectedMre.Reset();
				}

			}
		}

		//Send message and invokes MessageSubmitted.
		protected abstract void SendCallback(IAsyncResult result);

		#endregion

		#region Receiving

		#endregion

		#region Global-Event-Invokers

		//Invoke MessageReceived.
		protected internal override void RaiseMessageReceived(IClientInfo client, string message)
		{
			MessageReceived?.Invoke(this, message);
		}

		protected internal override void RaiseMessageContractReceived(IClientInfo client, IMessageContract contract, byte[] data)
		{
			contract.RaiseOnMessageReceived(this, client, contract.DeserializeToObject(data), contract.MessageHeader);
		}

		protected internal override void RaiseCustomHeaderReceived(IClientInfo client, string header, string message)
		{
			CustomHeaderReceived?.Invoke(this, message, header);
		}

		protected internal override void RaiseBytesReceived(IClientInfo client, byte[] data)
		{
			BytesReceived?.Invoke(this, data);
		}

		protected internal override void RaiseFileReceiver(IClientInfo client, int currentPart, int totalParts, string partPath, MessageState status)
		{
			FileReceiver?.Invoke(this, currentPart, totalParts, partPath, status);
		}

		protected internal override void RaiseFolderReceiver(IClientInfo client, int currentPart, int totalParts, string partPath, MessageState status)
		{
			FolderReceiver?.Invoke(this, currentPart, totalParts, partPath, status);
		}

		protected internal override void RaiseObjectReceived(IClientInfo client, object obj, Type objectType)
		{
			ObjectReceived?.Invoke(this, obj, objectType);
		}

		protected internal override void RaiseMessageUpdateStateFileTransfer(IClientInfo client, string origin, string remoteSaveLoc,double percentageDone, MessageState state)
		{
			MessageUpdateFileTransfer?.Invoke(this, origin, remoteSaveLoc, percentageDone, state);
		}

		protected internal override void RaiseMessageUpdate(IClientInfo client, string msg, string header, MessageType msgType,MessageState state)
		{
			MessageUpdate?.Invoke(this, msg, header, msgType, state);
		}

		protected internal override void RaiseMessageFailed(IClientInfo client, byte[] payLoad,Exception ex)
		{
			MessageFailed?.Invoke(this,payLoad,ex);
		}

		protected internal override void RaiseLog(string message)
		{
			ClientLogs?.Invoke(this, message);
		}

		protected internal override void RaiseLog(Exception ex)
		{
			var str = ex.Message + Environment.NewLine + "stacktrace: " + ex.StackTrace;
			ClientLogs?.Invoke(this, str);
		}

		protected internal override void RaiseErrorThrown(Exception ex)
		{
			ClientErrorThrown?.Invoke(this, ex);
		}

		protected void RaiseMessageSubmitted(bool close)
		{
			MessageSubmitted?.Invoke(this, close);
		}

		protected void RaiseDisconnected()
		{
			if (_disconnectedInvoked == false)
			{
				DisconnectedFromServer?.Invoke(this);
				_disconnectedInvoked = true;
			}
		}

		protected void RaiseConnected()
		{
			IsRunning = true;
			ConnectedToServer?.Invoke(this);
			_disconnectedInvoked = false;
		}

		#endregion

		#region Send Methods

		#region AuthMessage

		protected void SendBasicAuthMessage()
		{
			var osVersion = Environment.OSVersion;

			var guid = string.IsNullOrEmpty(_clientGuid) ? Guid.NewGuid().ToString() : _clientGuid;

			var msg = guid + "|" + osVersion;

			var builder = new SimpleMessage(MessageType.BasicAuth, this, Debug)
				.CompressMessage(false)
				.EncryptMessage(true)
				.SetMessage(msg);
			builder.Build();
			SendToSocket(builder.PayLoad, false, false);
		}

		protected void SendAuthMessage()
		{
			var username = Environment.UserName;
			var osVersion = Environment.OSVersion;
			var user = Environment.UserDomainName;

			//Keep existing GUID
			var guid = string.IsNullOrEmpty(_clientGuid) ? Guid.NewGuid().ToString() : _clientGuid;

			var msg = username + "|" + guid + "|" + user + "|" + osVersion;

			var builder = new SimpleMessage(MessageType.Auth, this, Debug)
				.CompressMessage(false)
				.EncryptMessage(true)
				.SetMessage(msg);

			builder.Build();
			SendToSocket(builder.PayLoad, false, false);
		}

		#endregion

		#region Message

		public void SendMessage(string message, bool compress = false, bool encrypt = false, bool close = false)
		{
			var messageBuilder = new SimpleMessage(MessageType.Message, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMessage(message);

			messageBuilder.Build();
			SendToSocket(messageBuilder.PayLoad, close, false);
		}

		public async Task SendMessageAsync(string message, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.Message, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMessage(message);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false);
		}

		#endregion

		#region Bytes

		public void SendBytes(byte[] data, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.Bytes, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(data);

			builder.Build();
			SendToSocket(builder.PayLoad, close, false);
		}

		public async Task SendBytesAsync(byte[] data, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.Bytes, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(data);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false);
		}

		#endregion

		#region MessageContract

		public void SendMessageContract(IMessageContract contract, bool compress = false, bool encrypt = false,bool close = false)
		{
			var builder = new SimpleMessage(MessageType.MessageContract, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(contract.MessageHeader)
				.SetBytes(contract.SerializeToBytes());

			builder.Build();
			SendToSocket(builder.PayLoad, close, false);
		}

		public async Task SendMessageContractAsync(IMessageContract contract, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.MessageContract, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(contract.MessageHeader)
				.SetBytes(contract.SerializeToBytes());

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false);
		}

		#endregion

		#region Message-CustomHeader

		public void SendCustomHeader(string message, string header, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.CustomHeader, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(header)
				.SetMessage(message);

			builder.Build();
			SendToSocket(builder.PayLoad, close, false);
		}

		public void SendCustomHeader(byte[] data, byte[] header, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.CustomHeader, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeader(header)
				.SetBytes(data);

			builder.Build();
			SendToSocket(builder.PayLoad, close, false);
		}

		public async Task SendCustomHeaderAsync(string message, string header, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.CustomHeader, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(header)
				.SetMessage(message);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false);
		}

		public async Task SendCustomHeaderAsync(byte[] data, byte[] header, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.CustomHeader, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeader(header)
				.SetBytes(data);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false);
		}

		#endregion

		#region File

		public async Task SendFileAsync(string fileLocation, string remoteSaveLocation, bool compress = true, bool encrypt = false, bool close = false)
		{
			await StreamFileFolderAsync(fileLocation, remoteSaveLocation, encrypt, compress, close,MessageType.File);
		}

		public void SendFile(string fileLocation, string remoteSaveLocation, bool compress = true, bool encrypt = false, bool close = false)
		{
			StreamFileFolder(fileLocation, remoteSaveLocation, encrypt, compress, close,MessageType.File);
		}

		#endregion

		#region Folder

		public async Task SendFolderAsync(string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false)
		{
			await StreamFileFolderAsync(folderLocation, remoteSaveLocation, encrypt, true, close,MessageType.Folder);
		}

		public void SendFolder(string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false)
		{
			StreamFileFolder(folderLocation, remoteSaveLocation, encrypt, true, close, MessageType.Folder);
		}


		#endregion

		#region Object

		public async Task SendObjectAsync(object obj, bool compress = false, bool encrypt = false, bool close = false)
		{
			if (ObjectSerializer == null)
				throw new Exception("No ObjectSerializer is currently set.");

			var builder = new SimpleMessage(MessageType.Object, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(ObjectSerializer.SerializeObjectToBytes(obj));

			await builder.BuildAsync();

			SendToSocket(builder.PayLoad, close, false);
		}

		public void SendObject(object obj, bool compress = false, bool encrypt = false, bool close = false)
		{
			if (ObjectSerializer == null)
				throw new Exception("No ObjectSerializer is currently set.");

			var builder = new SimpleMessage(MessageType.Object, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(ObjectSerializer.SerializeObjectToBytes(obj));
			builder.Build();

			SendToSocket(builder.PayLoad, close, false);
		}


		#endregion


		#endregion



	}
}
