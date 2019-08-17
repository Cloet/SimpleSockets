using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Compression.File;
using SimpleSockets.Messaging.Compression.Folder;
using SimpleSockets.Messaging.Compression.Stream;
using SimpleSockets.Messaging.Cryptography;
using SimpleSockets.Messaging.MessageContract;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets.Server
{
	public abstract class SimpleSocketListener: SimpleSocket
	{

		private static System.Timers.Timer _keepAliveTimer;
		internal IDictionary<int, ISocketState> ConnectedClients = new Dictionary<int, ISocketState>();

		protected int Limit = 500;
		protected readonly ManualResetEvent CanAcceptConnections = new ManualResetEvent(false);
		protected Socket Listener { get; set; }
		protected bool Disposed { get; set; }

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

		#region Events

		/// <summary>
		/// Event that is triggered when the server receives a Message.
		/// Format is ID:MESSAGE
		/// </summary>
		public event Action<int, string> MessageReceived;

		/// <summary>
		/// Event that is triggered when the server receives a Message with a custom header.
		/// Format is ID:MESSAGE:HEADER
		/// </summary>
		public event Action<int, string, string> CustomHeaderReceived;

		/// <summary>
		/// Event that is triggered when the server receives bytes from a client.
		/// Format = Id:Bytes
		/// </summary>
		public event Action<int, byte[]> BytesReceived;

		/// <summary>
		/// Event that is triggered when the server receives an object from a client
		/// Format = Id:Object:ObjectType
		/// </summary>
		public event Action<int, object, Type> ObjectReceived;

		/// <summary>
		/// Event that is triggered when the server receives an update on a filetransfer.
		/// This is invoked when sending a File/Folder.
		/// Format = Id:Origin:RemoteLoc:PercentDone:State
		/// </summary>
		public event Action<int, string, string, double, MessageState> MessageUpdateFileTransfer;

		/// <summary>
		/// Event that is triggered when sending a message, giving an update of the state.
		/// Format = Id:message:header:MessageType:State
		/// </summary>
		public event Action<int, string, string, MessageType, MessageState> MessageUpdate;

		/// <summary>
		/// Gives progress of current filetransfer.
		/// Format: Id:CurrentPart,TotalParts,location,State
		/// </summary>
		public event Action<int, int, int, string, MessageState> FileReceiver;

		/// <summary>
		/// Gives progress of current filetransfer.
		/// Format: Id:CurrentPart,TotalParts,location,State
		/// </summary>
		public event Action<int, int, int, string, MessageState> FolderReceiver;

		/// <summary>
		/// Event that is triggered when the server successfully has submitted a transmission of data.
		/// Format is ID:CLOSE
		/// The bool represents if the server has terminated after the message.
		/// </summary>
		public event Action<int, bool> MessageSubmitted;

		/// <summary>
		/// Event that is triggered when a client disconnects from the server.
		/// Format is ID
		/// </summary>
		public event Action<int> ClientDisconnected;

		/// <summary>
		/// Event that is triggered when a client connects to the server
		/// ID:ClientInfo
		/// </summary>
		public event Action<int, ISocketInfo> ClientConnected;
		
		/// <summary>
		/// Event that triggers when the server has successfully started
		/// </summary>
		public event Action ServerHasStarted;

		/// <summary>
		/// Event that is triggered when the server has failed to sent a sequence of bytes.
		/// </summary>
		public event Action<int, byte[], Exception> MessageFailed;

		/// <summary>
		/// Event that is triggered when exceptions are thrown within the server
		/// </summary>
		public event Action<Exception> ServerErrorThrown;

		public event Action<string> ServerLogs;

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
		internal bool IsConnectionAllowed(ISocketState state)
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
		internal ISocketState GetClient(int id)
		{
			return ConnectedClients.TryGetValue(id, out var state) ? state : null;
		}

		/// <summary>
		/// Get dictionary of clients
		/// </summary>
		/// <returns></returns>
		internal IDictionary<int, ISocketState> GetClients()
		{
			return ConnectedClients;
		}

		/// <summary>
		/// Returns all currently connected clients
		/// </summary>
		/// <returns></returns>
		public IDictionary<int, ISocketInfo> GetConnectedClients()
		{
			return ConnectedClients.ToDictionary(x => x.Key, x => (ISocketInfo)x.Value);
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Check if a client with given id is connected, remove if inactive.
		/// </summary>
		/// <param name="id"></param>
		public void CheckClient(int id)
		{
			if (!IsConnected(id))
			{
				ClientDisconnected?.Invoke(id);
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
		/// Stops the server from listening
		/// </summary>
		public void StopListening()
		{
			TokenSource.Cancel();
			IsRunning = false;

			foreach (var id in ConnectedClients.Keys.ToList())
			{
				Close(id);
			}

			Listener.Close();
		}

		/// <summary>
		/// Resumes listening
		/// </summary>
		public void ResumeListening()
		{
			if (IsRunning)
				throw new Exception("The server is already running.");

			if (string.IsNullOrEmpty(Ip))
				throw new ArgumentException("This method should only be used after using 'StopListening()'");
			if (Port == 0)
				throw new ArgumentException("This method should only be used after using 'StopListening()'");

			StartListening(Ip, Port, Limit);
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
				if (this.GetClient(id) is ISocketState state && state.Listener is Socket socket)
				{
					return !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
				}
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

					ConnectedClients = new Dictionary<int, ISocketState>();
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
				}
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			finally
			{
				lock (ConnectedClients)
				{
					ConnectedClients.Remove(id);
					ClientDisconnected?.Invoke(id);
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

		//Invoke Message Received
		protected internal override void RaiseMessageReceived(int id, string message)
		{
			MessageReceived?.Invoke(id, message);
		}

		protected internal override void RaiseMessageContractReceived(int id, IMessageContract contract, byte[] data)
		{
			contract.RaiseOnMessageReceived(this, id, contract.DeserializeToObject(data), contract.MessageHeader);
		}

		protected internal override void RaiseCustomHeaderReceived(int id, string header, string message)
		{
			CustomHeaderReceived?.Invoke(id, message, header);
		}

		protected internal override void RaiseBytesReceived(int id, byte[] data)
		{
			BytesReceived?.Invoke(id, data);
		}

		protected internal override void RaiseFileReceiver(int id, int currentPart, int totalParts, string partPath, MessageState status)
		{
			FileReceiver?.Invoke(id, currentPart, totalParts, partPath, status);
		}

		protected internal override void RaiseFolderReceiver(int id, int currentPart, int totalParts, string partPath, MessageState status)
		{
			FolderReceiver?.Invoke(id, currentPart, totalParts, partPath, status);
		}

		protected internal override void RaiseObjectReceived(int id, object obj, Type objectType)
		{
			ObjectReceived?.Invoke(id, obj, objectType);
		}

		protected internal override void RaiseMessageUpdateStateFileTransfer(int id,string origin, string remoteSaveLoc,double percentageDone, MessageState state)
		{
			MessageUpdateFileTransfer?.Invoke(id, origin, remoteSaveLoc, percentageDone, state);
		}

		protected internal override void RaiseMessageUpdate(int id,string msg, string header, MessageType msgType,MessageState state)
		{
			MessageUpdate?.Invoke(id, msg, header, msgType, state);
		}

		protected internal override void RaiseMessageFailed(int id, byte[] payLoad, Exception ex)
		{
			MessageFailed?.Invoke(id, payLoad, ex);
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

		protected void RaiseMessageSubmitted(int id,bool close)
		{
			MessageSubmitted?.Invoke(id, close);
		}

		protected void RaiseServerHasStarted()
		{
			IsRunning = true;
			ServerHasStarted?.Invoke();
		}

		protected void RaiseClientConnected(int id, ISocketInfo clientInfo)
		{
			ClientConnected?.Invoke(id, clientInfo);
		}

		protected void RaiseClientDisconnected(int id)
		{
			ClientDisconnected?.Invoke(id);
		}

		#endregion

		#region Send-Methods

		#region Message

		public void SendMessage(int id, string message, bool compress = false, bool encrypt = false, bool close = false)
		{
			var messageBuilder = new SimpleMessage(MessageType.Message, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMessage(message);

			messageBuilder.Build();
			SendToSocket(messageBuilder.PayLoad, close, false, id);
		}

		public async Task SendMessageAsync(int id, string message, bool compress = false, bool encrypt = false,bool close = false)
		{
			var builder = new SimpleMessage(MessageType.Message, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetMessage(message);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#region Bytes

		public void SendBytes(int id, byte[] data, bool compress = false, bool encrypt = false, bool close = false)
		{
			var messageBuilder = new SimpleMessage(MessageType.Bytes, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(data);

			messageBuilder.Build();
			SendToSocket(messageBuilder.PayLoad, close, false, id);
		}

		public async Task SendBytesAsync(int id, byte[] data, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.Bytes, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(data);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#region MessageContract

		public void SendMessageContract(int id, IMessageContract contract, bool compress = false, bool encrypt = false,bool close = false)
		{
			var builder = new SimpleMessage(MessageType.MessageContract, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(contract.MessageHeader)
				.SetBytes(contract.SerializeToBytes());

			builder.Build();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		public async Task SendMessageContractAsync(int id, IMessageContract contract, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.MessageContract, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(contract.MessageHeader)
				.SetBytes(contract.SerializeToBytes());

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#region Message-CustomHeader

		public void SendCustomHeader(int id, string message, string header, bool compress = false, bool encrypt = false,bool close = false)
		{
			var builder = new SimpleMessage(MessageType.CustomHeader, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(header)
				.SetMessage(message);

			builder.Build();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		public void SendCustomHeader(int id, byte[] data, byte[] header, bool compress = false, bool encrypt = false,bool close = false)
		{
			var builder = new SimpleMessage(MessageType.CustomHeader, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeader(header)
				.SetBytes(data);

			builder.Build();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		public async Task SendCustomHeaderAsync(int id, string message, string header, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.CustomHeader, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeaderString(header)
				.SetMessage(message);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		public async Task SendCustomHeaderAsync(int id, byte[] data, byte[] header, bool compress = false, bool encrypt = false, bool close = false)
		{
			var builder = new SimpleMessage(MessageType.CustomHeader, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetHeader(header)
				.SetBytes(data);

			await builder.BuildAsync();
			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#region File

		public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation,bool compress = true, bool encrypt = false, bool close = false)
		{
			await StreamFileFolderAsync(fileLocation, remoteFileLocation, compress, encrypt, close, id, MessageType.File);
		}

		public void SendFile(int id, string fileLocation, string remoteFileLocation, bool compress = true, bool encrypt = false, bool close = false)
		{
			StreamFileFolder(fileLocation, remoteFileLocation, encrypt, compress, close, id, MessageType.File);
		}

		#endregion

		#region Folder

		public async Task SendFolderAsync(int id, string folderLocation, string remoteSaveLocation, bool encrypt = false, bool close = false)
		{
			await StreamFileFolderAsync(folderLocation, remoteSaveLocation, encrypt, true, close, id,MessageType.Folder);
		}

		public void SendFolder(int id, string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false)
		{
			StreamFileFolder(folderLocation, remoteSaveLocation, encrypt, true, close, id, MessageType.Folder);
		}

		#endregion

		#region Object

		public async Task SendObjectAsync(int id, object obj, bool compress = false, bool encrypt = false,bool close = false)
		{
			if (ObjectSerializer == null)
				throw new Exception("No ObjectSerializer is currently set.");

			var builder = new SimpleMessage(MessageType.Object, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(ObjectSerializer.SerializeObjectToBytes(obj));

			await builder.BuildAsync();

			SendToSocket(builder.PayLoad, close, false, id);
		}

		public void SendObject(int id, object obj, bool compress = false, bool encrypt = false, bool close = false)
		{
			if (ObjectSerializer == null)
				throw new Exception("No ObjectSerializer is currently set.");

			var builder = new SimpleMessage(MessageType.Object, this, Debug)
				.CompressMessage(compress)
				.EncryptMessage(encrypt)
				.SetBytes(ObjectSerializer.SerializeObjectToBytes(obj));

			builder.Build();

			SendToSocket(builder.PayLoad, close, false, id);
		}

		#endregion

		#endregion

	}
}
