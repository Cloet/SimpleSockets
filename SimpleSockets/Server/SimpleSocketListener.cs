using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Compression.File;
using SimpleSockets.Messaging.Compression.Folder;
using SimpleSockets.Messaging.Compression.Stream;
using SimpleSockets.Messaging.Cryptography;
using SimpleSockets.Messaging.MessageContracts;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets.Server
{

	#region Delegates

	public delegate void MessageReceivedDelegate(IClientInfo client, string message);

	public delegate void MessageWithMetadataReceivedDelegate(IClientInfo client, object message, IDictionary<object,object> metadata, Type objType);

	public delegate void BytesReceivedDelegate(IClientInfo client, byte[] messageData);

	public delegate void ObjectReceivedDelegate(IClientInfo client, object obj, Type type);

	public delegate void MessageUpdateFileTransferDelegate(IClientInfo client, string origin, string remoteLoc,double percentageDone, MessageState state);

	public delegate void MessageUpdateDelegate(IClientInfo client, string origin, string remoteLoc, MessageType messageType,MessageState messageState);

	public delegate void FileReceiverDelegate(IClientInfo client, int currentPart, int totalPart, string output,MessageState messageState);

	public delegate void FolderReceiverDelegate(IClientInfo client, int currentPart, int totalPart, string output,MessageState messageState);

	public delegate void MessageSubmittedDelegate(IClientInfo client, bool close);

	public delegate void ClientDisconnectedDelegate(IClientInfo client, DisconnectReason reason);

	public delegate void ClientConnectedDelegate(IClientInfo clientInfo);

	public delegate void ServerHasStartedDelegate();

	public delegate void MessageFailedDelegate(IClientInfo client, byte[] messageData, Exception ex);

	public delegate void ServerErrorThrownDelegate(Exception ex);

	public delegate void ServerLogsDelegate(string log);

	public delegate void AuthenticationStatus(IClientInfo client, AuthStatus status);

	#endregion

	public abstract class SimpleSocketListener: SimpleSocket
	{

		private static System.Timers.Timer _keepAliveTimer;
		private TimeSpan _timeout = new TimeSpan(0, 0, 0);
		internal IDictionary<int, IClientMetadata> ConnectedClients = new ConcurrentDictionary<int, IClientMetadata>();

		protected int Limit = 500;
		protected readonly ManualResetEvent CanAcceptConnections = new ManualResetEvent(false);
		protected Socket Listener { get; set; }
		protected bool Disposed { get; set; }

		protected ParallelQueue ParallelQueue { get; set; }

		/// <summary>
		/// Server will only accept IP Addresses that are in the whitelist.
		/// If the whitelist is empty all IPs will be accepted unless they are blacklisted.
		/// </summary>
		public IList<IPAddress> WhiteList { get; set; }

		/// <summary>
		/// The server will not accept connections from these IPAddresses.
		/// Whitelist has priority over Blacklist meaning that if an IPAddress is in the whitelist and blacklist
		/// It will still be added.
		/// </summary>
		public IList<IPAddress> BlackList { get; set; }

		/// <summary>
		/// Time until a client will timeout.
		/// Value cannot be less then 5 seconds
		/// Default value is 0 which means the server will wait indefinitely until it gets a response. 
		/// </summary>
		public TimeSpan Timeout
		{
			get => _timeout;
			set
			{
				if (value.TotalSeconds > 0 && value.TotalSeconds < 5)
					throw new ArgumentOutOfRangeException(nameof(Timeout));
				_timeout = value;
			}
		}

		#region Events

		/// <summary>
		/// Only thrown when using ssl
		/// </summary>
		public event AuthenticationStatus SslAuthStatus;

		/// <summary>
		/// Event that is triggered when the server receives a Message.
		/// Format is ID:MESSAGE
		/// </summary>
		public event MessageReceivedDelegate MessageReceived;

		/// <summary>
		/// Event that is triggered when the server receives a Message with a custom header.
		/// Format is ID:MESSAGE:HEADER
		/// </summary>
		public event MessageWithMetadataReceivedDelegate MessageWithMetaDataReceived;

		/// <summary>
		/// Event that is triggered when the server receives bytes from a client.
		/// Format = Id:Bytes
		/// </summary>
		public event BytesReceivedDelegate BytesReceived;

		/// <summary>
		/// Event that is triggered when the server receives an object from a client
		/// Format = Id:Object:ObjectType
		/// </summary>
		public event ObjectReceivedDelegate ObjectReceived;

		/// <summary>
		/// Event that is triggered when the server receives an update on a filetransfer.
		/// This is invoked when sending a File/Folder.
		/// Format = Id:Origin:RemoteLoc:PercentDone:State
		/// </summary>
		public event MessageUpdateFileTransferDelegate MessageUpdateFileTransfer;

		/// <summary>
		/// Event that is triggered when sending a message, giving an update of the state.
		/// Format = Id:message:header:MessageType:State
		/// </summary>
		public event MessageUpdateDelegate MessageUpdate;

		/// <summary>
		/// Gives progress of current filetransfer.
		/// Format: Id:CurrentPart,TotalParts,location,State
		/// </summary>
		public event FileReceiverDelegate FileReceiver;

		/// <summary>
		/// Gives progress of current filetransfer.
		/// Format: Id:CurrentPart,TotalParts,location,State
		/// </summary>
		public event FolderReceiverDelegate FolderReceiver;

		/// <summary>
		/// Event that is triggered when the server successfully has submitted a transmission of data.
		/// Format is ID:CLOSE
		/// The bool represents if the server has terminated after the message.
		/// </summary>
		public event MessageSubmittedDelegate MessageSubmitted;

		/// <summary>
		/// Event that is triggered when a client disconnects from the server.
		/// Format is ID
		/// </summary>
		public event ClientDisconnectedDelegate ClientDisconnected;

		/// <summary>
		/// Event that is triggered when a client connects to the server
		/// ID:ClientInfo
		/// </summary>
		public event ClientConnectedDelegate ClientConnected;
		
		/// <summary>
		/// Event that triggers when the server has successfully started
		/// </summary>
		public event ServerHasStartedDelegate ServerHasStarted;

		/// <summary>
		/// Event that is triggered when the server has failed to sent a sequence of bytes.
		/// </summary>
		public event MessageFailedDelegate MessageFailed;

		/// <summary>
		/// Event that is triggered when exceptions are thrown within the server
		/// </summary>
		public event ServerErrorThrownDelegate ServerErrorThrown;

		/// <summary>
		/// Event that Logs messages
		/// </summary>
		public event ServerLogsDelegate ServerLogs;

		#endregion

		/// <summary>
		/// Base constructor
		/// </summary>
		protected SimpleSocketListener()
		{
			//Set timer that checks all clients every 5 minutes
			_keepAliveTimer = new System.Timers.Timer(300000);
			_keepAliveTimer.Elapsed += KeepAlive;
			_keepAliveTimer.AutoReset = true;
			_keepAliveTimer.Enabled = true;
			WhiteList = new List<IPAddress>();
			BlackList = new List<IPAddress>();

			IsRunning = false;
			AllowReceivingFiles = false;

			ParallelQueue = new ParallelQueue(50);

			ByteCompressor = new DeflateByteCompression();
			MessageEncryption = new Aes256();
			FileCompressor = new GZipCompression();
			FolderCompressor = new ZipCompression();
		}

		#region Methods

		/// <summary>
		/// Add a socket to the clients dictionary.
		/// Lock clients temporary to handle multiple access.
		/// ReceiveCallback raise an event, after the message receiving is complete.
		/// </summary>
		/// <param name="result"></param>
		protected abstract void OnClientConnect(IAsyncResult result);

		//Converts string to IPAddress
		protected IPAddress DetermineListenerIp(string ip)
		{
			try
			{
				if (string.IsNullOrEmpty(ip))
				{
					var ipAdr = IPAddress.Any;
					Ip = ipAdr.ToString();
					return ipAdr;
				}

				//Try to parse the ip string to a valid IPAddress
				return IPAddress.Parse(ip);

			}
			catch (SocketException se)
			{
				throw new Exception("Invalid server IP", se);
			}
			catch (Exception ex)
			{
				throw new Exception("Error trying to get IPAddress from string : " + ip, ex);
			}
		}

		/// <summary>
		/// Export Connected Clients to a DSV (Delimiter separated values)  File
		/// </summary>
		/// <param name="path"></param>
		/// <param name="delimiter"></param>
		public void ExportConnectedClientsToDsv(string path, string delimiter = ";")
		{

			using (StreamWriter sw = new StreamWriter(path))
			{
				var last = GetConnectedClients().Last();
				foreach (var client in GetConnectedClients())
				{
					if (client.Key != last.Key)
						sw.Write(client.Value.RemoteIPv4 + delimiter);
					else
						sw.Write(client.Value.RemoteIPv4);

				}
			}

		}

		//Check if the server should allow the client that is attempting to connect.
		internal bool IsConnectionAllowed(IClientMetadata state)
		{
			if (WhiteList.Count > 0)
			{
				return CheckWhitelist(state.RemoteIPv4) || CheckWhitelist(state.RemoteIPv6);
			}

			if (BlackList.Count > 0)
			{
				return !CheckBlacklist(state.RemoteIPv4) && !CheckBlacklist(state.RemoteIPv6);
			}

			return true;
		}

		//Checks if an ip is in the whitelist
		protected bool CheckWhitelist(string ip)
		{
			var address = IPAddress.Parse(ip);
			return WhiteList.Any(x => Equals(x, address));
		}

		//Checks if an ip is in the blacklist
		protected bool CheckBlacklist(string ip)
		{
			var address = IPAddress.Parse(ip);
			return BlackList.Any(x => Equals(x, address));
		}

		//Timer that checks client every x seconds
		private void KeepAlive(object source, ElapsedEventArgs e)
		{
			CheckAllClients();
		}

		// Gets a socket from the clients dictionary by his Id.
		internal IClientMetadata GetClient(int id)
		{
			return ConnectedClients.TryGetValue(id, out var state) ? state : null;
		}

		/// <summary>
		/// Get dictionary of clients
		/// </summary>
		/// <returns></returns>
		internal IDictionary<int, IClientMetadata> GetClients()
		{
			return ConnectedClients;
		}

		/// <summary>
		/// Returns all currently connected clients
		/// </summary>
		/// <returns></returns>
		public IDictionary<int, IClientInfo> GetConnectedClients()
		{
			return ConnectedClients.ToDictionary(x => x.Key, x => (IClientInfo)x.Value);
		}

		/// <summary>
		/// Get metadata of a client with a particular ID
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public IClientInfo GetClientFromId(int id)
		{
			return ConnectedClients.TryGetValue(id, out var state) ? state : null;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Check if a client with given id is connected, remove if inactive.
		/// </summary>
		/// <param name="id"></param>
		public void CheckClient(int id)
		{
			if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
			if (!IsConnected(id))
			{
				var client = GetClient(id);
				ClientDisconnected?.Invoke(client, DisconnectReason.Unknown);
				ConnectedClients.Remove(id);
			}
		}

		/// <summary>
		/// Check all clients and show which are disconnected.
		/// </summary>
		public void CheckAllClients()
		{
			lock (ConnectedClients)
			{
				if (ConnectedClients.Keys.Count > 0)
				{
					foreach (var id in ConnectedClients.Keys)
					{
						CheckClient(id);
					}
				}
			}
		}

		/// <summary>
		/// Starts listening on the given port.
		/// </summary>
		///	<param name="ip"></param> 
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public abstract void StartListening(string ip, int port, int limit = 500);

		/// <summary>
		/// Starts listening on all possible interfaces.
		/// Safest option to start the server.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public void StartListening(int port, int limit = 500)
		{
			StartListening(null, port, limit);
		}
		
		/// <summary>
		/// returns if a certain client is connected
		/// </summary>
		/// <param name="id"></param>
		/// <returns>bool</returns>
		public bool IsConnected(int id)
		{
			try
			{
				if (this.GetClient(id) is IClientMetadata state && state.Listener is Socket socket)
				{
					return !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
				}
			} catch (ObjectDisposedException)
			{
				return false;
			}
			catch (Exception ex)
			{
				RaiseErrorThrown(ex);
			}

			return false;
		}

		/// <inheritdoc />
		/// <summary>
		/// Properly dispose the class.
		/// </summary>
		public override void Dispose()
		{
			try
			{
				if (!Disposed)
				{
					TokenSource.Cancel();
					TokenSource.Dispose();
					IsRunning = false;
					Listener.Dispose();
					CanAcceptConnections.Dispose();
					_keepAliveTimer.Enabled = false;
					_keepAliveTimer.Dispose();

					foreach (var id in ConnectedClients.Keys.ToList())
					{
						Close(id);
					}

					ConnectedClients = new Dictionary<int, IClientMetadata>();
					TokenSource.Dispose();
					Disposed = true;
					GC.SuppressFinalize(this);
				}
				else
				{
					throw new ObjectDisposedException(nameof(SimpleSocketListener), "This object is already disposed.");
				}

			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}


		/// <summary>
		/// Close a certain client
		/// </summary>
		/// <param name="id"></param>
		public void Close(int id)
		{
			var state = GetClient(id);

			if (state == null)
			{
				RaiseErrorThrown(new Exception("Client does not exist."));
			}

			try
			{
				if (state?.Listener != null)
				{
					state.Listener.Shutdown(SocketShutdown.Both);
					state.Listener.Close();
					state.Listener = null;
				}
			}
			catch (ObjectDisposedException) { 
				// Object has already been disposed, do nothing.
			}
			catch (Exception ex)
			{
				RaiseErrorThrown(ex);
			}
			finally
			{
				lock (ConnectedClients)
				{
					ConnectedClients.Remove(id);
					ClientDisconnected?.Invoke(state, DisconnectReason.Normal);
				}
			}
		}

		#endregion

		#region Message Sending

		protected abstract void BeginSendFromQueue(MessageWrapper message);

		protected void SendFromQueue()
		{
			while (!Token.IsCancellationRequested)
			{
				BlockingMessageQueue.TryDequeue(out var message);

				if (IsConnected(message.State.Id))
				{
					BeginSendFromQueue(message);
				}
				else
				{
					Close(message.State.Id);
				}

				message = null;
			}
		}

		#endregion

		#region Receiving Data


		#endregion

		#region Callbacks

		//End the send and invoke MessageSubmitted event.
		protected abstract void SendCallback(IAsyncResult result);

		#endregion
		
		#region Global-Event-Invokers

		protected void RaiseAuthFailed(IClientInfo client)
		{
			SslAuthStatus?.Invoke(client, AuthStatus.Failed);
		}

		protected void RaiseAuthSuccess(IClientInfo client)
		{
			SslAuthStatus?.Invoke(client, AuthStatus.Success);
		}

		//Invoke Message Received
		protected internal override void RaiseMessageReceived(IClientInfo client, string message)
		{
			MessageReceived?.Invoke(client, message);
		}

		protected internal override void RaiseMessageContractReceived(IClientInfo client, IMessageContract contract, byte[] data)
		{
			contract.RaiseOnMessageReceived(this,client, contract.DeserializeToObject(data), contract.MessageHeader);
		}

		protected internal override void RaiseMessageWithMetaDataReceived(IClientInfo client, object message, IDictionary<object,object> metadata, Type objType)
		{
			MessageWithMetaDataReceived?.Invoke(client, message, metadata, objType);
		}

		protected internal override void RaiseBytesReceived(IClientInfo client, byte[] data)
		{
			BytesReceived?.Invoke(client, data);
		}

		protected internal override void RaiseFileReceiver(IClientInfo client, int currentPart, int totalParts, string partPath, MessageState status)
		{
			FileReceiver?.Invoke(client, currentPart, totalParts, partPath, status);
		}

		protected internal override void RaiseFolderReceiver(IClientInfo client, int currentPart, int totalParts, string partPath, MessageState status)
		{
			FolderReceiver?.Invoke(client, currentPart, totalParts, partPath, status);
		}

		protected internal override void RaiseObjectReceived(IClientInfo client, object obj, Type objectType)
		{
			ObjectReceived?.Invoke(client, obj, objectType);
		}

		protected internal override void RaiseMessageUpdateStateFileTransfer(IClientInfo client, string origin, string remoteSaveLoc,double percentageDone, MessageState state)
		{
			MessageUpdateFileTransfer?.Invoke(client, origin, remoteSaveLoc, percentageDone, state);
		}

		protected internal override void RaiseMessageUpdate(IClientInfo client, string msg, string header, MessageType msgType,MessageState state)
		{
			MessageUpdate?.Invoke(client, msg, header, msgType, state);
		}

		protected internal override void RaiseMessageFailed(IClientInfo client, byte[] payLoad, Exception ex)
		{
			MessageFailed?.Invoke(client, payLoad, ex);
		}

		protected internal override void RaiseLog(string message)
		{
			ServerLogs?.Invoke(message);
		}

		protected internal override void RaiseLog(Exception ex)
		{
			var str = ex.Message + Environment.NewLine + "stacktrace: " + ex.StackTrace;
			ServerLogs?.Invoke(str);
		}

		protected internal override void RaiseErrorThrown(Exception ex)
		{
			ServerErrorThrown?.Invoke(ex);
		}

		protected void RaiseMessageSubmitted(IClientInfo client,bool close)
		{
			MessageSubmitted?.Invoke(client, close);
		}

		protected void RaiseServerHasStarted()
		{
			IsRunning = true;
			ServerHasStarted?.Invoke();
		}

		protected void RaiseClientConnected(IClientInfo clientInfo)
		{
			ClientConnected?.Invoke(clientInfo);
		}

		protected void RaiseClientDisconnected(IClientInfo client, DisconnectReason reason)
		{
			ClientDisconnected?.Invoke(client, reason);
		}

		#endregion

		#region Send-Methods

		#region Message

		public void SendMessage(int id, string message, bool compress = false, bool encrypt = false, bool close = false)
		{
			var client = GetClient(id);
			var messageBuilder = new SimpleMessage(MessageType.Message, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMessage(message)
				.SetSendClient(client);

			messageBuilder.Build();
			SendToSocket(messageBuilder.PayLoad, close, false, id);
		}

		public async Task SendMessageAsync(int id, string message, bool compress = false, bool encrypt = false,bool close = false)
		{
			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.Message, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMessage(message)
				.SetSendClient(client);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#region Bytes

		public void SendBytes(int id, byte[] data, bool compress = false, bool encrypt = false, bool close = false)
		{
			var client = GetClient(id);
			var messageBuilder = new SimpleMessage(MessageType.Bytes, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(data)
				.SetSendClient(client);

			messageBuilder.Build();
			SendToSocket(messageBuilder.PayLoad, close, false, id);
		}

		public async Task SendBytesAsync(int id, byte[] data, bool compress = false, bool encrypt = false, bool close = false)
		{
			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.Bytes, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(data)
				.SetSendClient(client);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#region MessageContract

		public void SendMessageContract(int id, IMessageContract contract, bool compress = false, bool encrypt = false,bool close = false)
		{
			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.MessageContract, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(contract.MessageHeader)
				.SetBytes(contract.SerializeToBytes())
				.SetSendClient(client);

			builder.Build();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		public async Task SendMessageContractAsync(int id, IMessageContract contract, bool compress = false, bool encrypt = false, bool close = false)
		{
			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.MessageContract, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(contract.MessageHeader)
				.SetBytes(contract.SerializeToBytes())
				.SetSendClient(client);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#region MessageWithMetadata

		public void SendMessageWithMetadata(int id, byte[] data, IDictionary<object, object> metadata, bool compress = false, bool encrypt = false, bool close = false) {
			if (ObjectSerializer == null)
				throw new ArgumentNullException(nameof(ObjectSerializer));

			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.MessageWithMetadata, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMetadata(metadata)
				.SetBytes(data)
				.SetHeaderString("ByteArray")
				.SetSendClient(client);

			builder.Build();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		public async Task SendMessageWithMetadataAsync(int id, byte[] data, IDictionary<object, object> metadata, bool compress = false, bool encrypt = false, bool close = false)
		{
			if (ObjectSerializer == null)
				throw new ArgumentNullException(nameof(ObjectSerializer));

			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.MessageWithMetadata, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMetadata(metadata)
				.SetBytes(data)
				.SetHeaderString("ByteArray")
				.SetSendClient(client);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		public void SendMessageWithMetadata(int id, object message, IDictionary<object,object> metadata, bool compress = false, bool encrypt = false,bool close = false)
		{
			if (ObjectSerializer == null)
				throw new ArgumentNullException(nameof(ObjectSerializer));

			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.MessageWithMetadata, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMetadata(metadata)
				.SetBytes(ObjectSerializer.SerializeObjectToBytes(message))
				.SetHeaderString(message.GetType().AssemblyQualifiedName)
				.SetSendClient(client);

			builder.Build();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		public async Task SendCustomHeaderAsync(int id, object message, IDictionary<object,object> metadata, bool compress = false, bool encrypt = false, bool close = false)
		{
			if (ObjectSerializer == null)
				throw new ArgumentNullException(nameof(ObjectSerializer));

			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.MessageWithMetadata, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMetadata(metadata)
				.SetBytes(ObjectSerializer.SerializeObjectToBytes(message))
				.SetHeaderString(message.GetType().AssemblyQualifiedName)
				.SetSendClient(client);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}


		#endregion

		#region File

		public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation,bool compress = true, bool encrypt = false, bool close = false)
		{
			var client = GetClient(id);
			await StreamFileFolderAsync(fileLocation, remoteFileLocation, compress, encrypt, close,MessageType.File, client);
		}

		public void SendFile(int id, string fileLocation, string remoteFileLocation, bool compress = true, bool encrypt = false, bool close = false)
		{
			var client = GetClient(id);
			StreamFileFolder(fileLocation, remoteFileLocation, encrypt, compress, close, MessageType.File, client);
		}

		#endregion

		#region Folder

		public async Task SendFolderAsync(int id, string folderLocation, string remoteSaveLocation, bool encrypt = false, bool close = false)
		{
			var client = GetClient(id);
			await StreamFileFolderAsync(folderLocation, remoteSaveLocation, encrypt, true, close,MessageType.Folder, client);
		}

		public void SendFolder(int id, string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false)
		{
			var client = GetClient(id);
			StreamFileFolder(folderLocation, remoteSaveLocation, encrypt, true, close, MessageType.Folder, client);
		}

		#endregion

		#region Object

		public async Task SendObjectAsync(int id, object obj, bool compress = false, bool encrypt = false,bool close = false)
		{
			if (ObjectSerializer == null)
				throw new ArgumentNullException(nameof(ObjectSerializer));

			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.Object, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(ObjectSerializer.SerializeObjectToBytes(obj))
				.SetSendClient(client)
				.SetHeaderString(obj.GetType().AssemblyQualifiedName);

			await builder.BuildAsync();

			SendToSocket(builder.PayLoad, close, false, id);
		}

		public void SendObject(int id, object obj, bool compress = false, bool encrypt = false, bool close = false)
		{
			if (ObjectSerializer == null)
				throw new ArgumentNullException(nameof(ObjectSerializer));

			var client = GetClient(id);
			var builder = new SimpleMessage(MessageType.Object, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(ObjectSerializer.SerializeObjectToBytes(obj))
				.SetSendClient(client)
				.SetHeaderString(obj.GetType().AssemblyQualifiedName);

			builder.Build();

			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#endregion

	}
}
