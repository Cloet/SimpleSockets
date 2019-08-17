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

		//--Private
		private bool _disconnectedInvoked;

		//--Public
		/// <summary>
		/// This is how many seconds te client waits to try and reconnect to the server
		/// </summary>
		public int ReconnectInSeconds { get; protected set; }

		#endregion

		#region Events

		/// <summary>
		/// Event that triggers when a client is connected to server
		/// </summary>
		public event Action<SimpleSocketClient> ConnectedToServer;

		/// <summary>
		/// Event that is triggered when a client receives a message from a server
		/// Format = SimpleSocketClient:MESSAGE
		/// </summary>
		public event Action<SimpleSocketClient, string> MessageReceived;

		/// <summary>
		/// Event that is triggered when a client receives a custom message from a server
		/// Format = SimpleSocketClient:MESSAGE:HEADER
		/// </summary>
		public event Action<SimpleSocketClient, string, string> CustomHeaderReceived;

		/// <summary>
		/// Event that is triggered when a client receives bytes from the connected server.
		/// </summary>
		public event Action<SimpleSocketClient, byte[]> BytesReceived;

		/// <summary>
		/// Event that is triggered when the client receives an object from the server.
		/// </summary>
		public event Action<SimpleSocketClient, object, Type> ObjectReceived;

		/// <summary>
		/// Gives insight in the state of the current FileTransfer to the server.
		/// Format = Socket,OriginFile,RemoteSaveLocFile,PercentageDone,MessageState
		/// </summary>
		public event Action<SimpleSocketClient, string, string, double, MessageState> MessageUpdateFileTransfer;

		/// <summary>
		/// Gives insight in the state of the current message.
		/// </summary>
		public event Action<SimpleSocketClient, string, string, MessageType, MessageState> MessageUpdate;

		/// <summary>
		/// Event that is triggered when a client receives a file or a part of a file.
		/// Format = Socket,CurrentPart,TotalParts,PathToOutput,MessageState
		/// </summary>
		public event Action<SimpleSocketClient, int, int, string, MessageState> FileReceiver;

		/// <summary>
		/// Event that is triggered when a client receives a folder or a part of a folder.
		/// Format = Socket,CurrentPart,TotalParts,PathToOutput,MessageState
		/// </summary>
		public event Action<SimpleSocketClient, int, int, string, MessageState> FolderReceiver;

		/// <summary>
		/// Event that is triggered when the client successfully has submitted a transmission of data.
		/// Format is ID:CLOSE
		/// The bool represents if the client has terminated after the message.
		/// </summary>
		public event Action<SimpleSocketClient, bool> MessageSubmitted;

		/// <summary>
		/// Event that is triggered when the client has disconnected from the server.
		/// Format = SimpleSocketClient
		/// </summary>
		public event Action<SimpleSocketClient> DisconnectedFromServer;

		/// <summary>
		/// Event that is triggered when a client fails to send a message to the server
		/// Format = SimpleSocketClient:MessageType:MessageBytes,Exception
		/// </summary>
		public event Action<SimpleSocketClient,byte[],Exception> MessageFailed;

		/// <summary>
		/// Event that is triggered when a client gives an error.
		/// </summary>
		public event Action<SimpleSocketClient, Exception> ClientErrorThrown;

		/// <summary>
		/// Event that receives logs.
		/// </summary>
		public event Action<SimpleSocketClient, string> ClientLogs;

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
			Close();
			ConnectedMre.Dispose();
			SentMre.Dispose();
			KeepAliveTimer.Enabled = false;
			KeepAliveTimer.Dispose();
			TokenSource.Dispose();

			GC.SuppressFinalize(this);
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
		protected internal override void RaiseMessageReceived(int id, string message)
		{
			MessageReceived?.Invoke(this, message);
		}

		protected internal override void RaiseMessageContractReceived(int id, IMessageContract contract, byte[] data)
		{
			contract.RaiseOnMessageReceived(this, id, contract.DeserializeToObject(data), contract.MessageHeader);
		}

		protected internal override void RaiseCustomHeaderReceived(int id, string header, string message)
		{
			CustomHeaderReceived?.Invoke(this, message, header);
		}

		protected internal override void RaiseBytesReceived(int id, byte[] data)
		{
			BytesReceived?.Invoke(this, data);
		}

		protected internal override void RaiseFileReceiver(int id, int currentPart, int totalParts, string partPath, MessageState status)
		{
			FileReceiver?.Invoke(this, currentPart, totalParts, partPath, status);
		}

		protected internal override void RaiseFolderReceiver(int id, int currentPart, int totalParts, string partPath, MessageState status)
		{
			FolderReceiver?.Invoke(this, currentPart, totalParts, partPath, status);
		}

		protected internal override void RaiseObjectReceived(int id, object obj, Type objectType)
		{
			ObjectReceived?.Invoke(this, obj, objectType);
		}

		protected internal override void RaiseMessageUpdateStateFileTransfer(int id,string origin, string remoteSaveLoc,double percentageDone, MessageState state)
		{
			MessageUpdateFileTransfer?.Invoke(this, origin, remoteSaveLoc, percentageDone, state);
		}

		protected internal override void RaiseMessageUpdate(int id,string msg, string header, MessageType msgType,MessageState state)
		{
			MessageUpdate?.Invoke(this, msg, header, msgType, state);
		}

		protected internal override void RaiseMessageFailed(int id, byte[] payLoad,Exception ex)
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
			await StreamFileFolderAsync(fileLocation, remoteSaveLocation, encrypt, compress, close, -1, MessageType.File);
		}

		public void SendFile(string fileLocation, string remoteSaveLocation, bool compress = true, bool encrypt = false, bool close = false)
		{
			StreamFileFolder(fileLocation, remoteSaveLocation, encrypt, compress, close, -1, MessageType.File);
		}

		#endregion

		#region Folder

		public async Task SendFolderAsync(string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false)
		{
			await StreamFileFolderAsync(folderLocation, remoteSaveLocation, encrypt, true, close, -1,MessageType.Folder);
		}

		public void SendFolder(string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false)
		{
			StreamFileFolder(folderLocation, remoteSaveLocation, encrypt, true, close, -1, MessageType.Folder);
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
