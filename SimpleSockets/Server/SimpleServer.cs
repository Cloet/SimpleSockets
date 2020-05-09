using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Helpers.Serialization;
using SimpleSockets.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSockets.Server
{

	public abstract class SimpleServer : SimpleSocket
	{

		#region Events

		/// <summary>
		/// Event invoked when a client connects to the server.
		/// </summary>
		public event EventHandler<ClientInfoEventArgs> ClientConnected;
		protected virtual void OnClientConnected(ClientInfoEventArgs eventArgs) => ClientConnected?.Invoke(this, eventArgs);

		/// <summary>
		/// Event invoked when a client disconnects from the server.
		/// </summary>
		public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;
		protected virtual void OnClientDisconnected(ClientDisconnectedEventArgs eventArgs) => ClientDisconnected?.Invoke(this, eventArgs);

		/// <summary>
		/// Event invoked when a server starts listening for connections.
		/// </summary>
		public event EventHandler ServerStartedListening;
		protected virtual void OnServerStartedListening() => ServerStartedListening?.Invoke(this, null);

		/// <summary>
		/// Event invoked when the server received a message from a client.
		/// </summary>
		public event EventHandler<ClientMessageReceivedEventArgs> MessageReceived;
		protected virtual void OnClientMessageReceived(ClientMessageReceivedEventArgs eventArgs) => MessageReceived?.Invoke(this, eventArgs);

		/// <summary>
		/// Event invoked when the server receives an object from a client.
		/// </summary>
		public event EventHandler<ClientObjectReceivedEventArgs> ObjectReceived;
		protected virtual void OnClientObjectReceived(ClientObjectReceivedEventArgs eventArgs) => ObjectReceived?.Invoke(this, eventArgs);

		/// <summary>
		/// Event invoked when the server receives bytes from a client.
		/// </summary>
		public event EventHandler<ClientBytesReceivedEventArgs> BytesReceived;
		protected virtual void OnClientBytesReceived(ClientBytesReceivedEventArgs eventArgs) => BytesReceived?.Invoke(this, eventArgs);

		#endregion

		/// <summary>
		/// Handles all logs made by the socket.
		/// </summary>
		public override Action<string> Logger
		{
			get => _logger;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				SocketLogger = LogHelper.InitializeLogger(false, SocketProtocolType.Tcp == this.SocketProtocol, value, this.LoggerLevel);
				_logger = value;
			}
		}

		#region Variables

		private Action<string> _logger;

		private TimeSpan _timeout = new TimeSpan(0, 0, 0);

		protected readonly ManualResetEventSlim CanAcceptConnections = new ManualResetEventSlim(false);

		protected Socket Listener { get; set; }

		/// <summary>
		/// Indicates if the server is listening and accepting potential clients.
		/// </summary>
		public bool Listening { get; set; }

		/// <summary>
		/// Port the server listens to.
		/// </summary>
		public int ListenerPort { get; set; }

		/// <summary>
		/// Ip the server listens to.
		/// </summary>
		public string ListenerIp { get; set; }

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

		internal IDictionary<int, ISessionMetadata> ConnectedClients { get; set; }

		/// <summary>
		/// Potential eventhandlers for received messages.
		/// </summary>
		public IDictionary<string, EventHandler<ClientDataReceivedEventArgs>> DynamicCallbacks { get; protected set; }

		#endregion

		protected SimpleServer(SocketProtocolType protocol) : base(protocol)
		{
			Listening = false;
			ConnectedClients = new Dictionary<int, ISessionMetadata>();
			DynamicCallbacks = new Dictionary<string, EventHandler<ClientDataReceivedEventArgs>>();
			BlackList = new List<IPAddress>();
			WhiteList = new List<IPAddress>();
		}

		/// <summary>
		/// Listen for clients on the IP:Port
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public abstract void Listen(string ip, int port);

		/// <summary>
		/// Listen for clients on all local ports for a given port.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public void Listen(int port) => Listen("*", port);

		/// <summary>
		/// Returns true if a client is connected.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool IsClientConnected(int id)
		{
			try
			{
				if (GetClientMetadataById(id) is ISessionMetadata state && state.Listener is Socket socket)
				{
					return !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
				}
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Something went wrong trying to check if a client is connected.", ex, LogLevel.Error);
			}

			return false;
		}

		/// <summary>
		/// Returns true if a client is conntected.
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		public bool IsClientConnected(Guid guid)
		{
			try
			{
				if (GetClientInfoByGuid(guid) is ISessionMetadata state && state.Listener is Socket socket)
				{
					return !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
				}
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Something went wrong trying to check if a client is connected.", ex, LogLevel.Error);
			}

			return false;
		}

		/// <summary>
		/// Shutdown a client.
		/// </summary>
		/// <param name="id"></param>
		public void ShutDownClient(int id)
		{
			ShutDownClient(id, DisconnectReason.Normal);
		}

		/// <summary>
		/// Get ClientInfo by id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ISessionInfo GetClientInfoById(int id)
		{
			return GetClientMetadataById(id);
		}

		/// <summary>
		/// Get ClientInfo by guid.
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		public ISessionInfo GetClientInfoByGuid(Guid guid)
		{
			return ConnectedClients?.Values.Single(x => x.Guid == guid);
		}

		/// <summary>
		/// Get clientinfo of all connected clients
		/// </summary>
		/// <returns></returns>
		public IList<ISessionInfo> GetAllClients()
		{
			var clients = ConnectedClients.Values.Cast<ISessionInfo>().ToList();
			if (clients == null)
				return new List<ISessionInfo>();
			return clients;
		}

		#region Data-Invokers

		protected override void ByteDecoder(ISessionMetadata session, byte[] array)
		{
			try {
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == PacketHelper.ESCAPE || session.DataReceiver.LastByteEscape == true)
					{
						var lastEscape = session.DataReceiver.LastByteEscape;
						if (lastEscape)
							session.DataReceiver.LastByteEscape = false;

						if (i == (array.Length - 1))
							session.DataReceiver.LastByteEscape = true;
						else {
							if (!lastEscape)
								i++;
							if (array[i] == PacketHelper.EOF)
							{
								var msg = session.DataReceiver.BuildMessageFromPayload(EncryptionPassphrase, PreSharedKey);
								if (msg != null)
									OnMessageReceivedHandler(session, msg);
								session.ResetDataReceiver();
							}
							else
								session.DataReceiver.AppendByteToReceived(array[i]);
						}
					}
					else
						session.DataReceiver.AppendByteToReceived(array[i]);
				}
			} catch (Exception ex) {
				throw new Exception(ex.ToString(), ex);
			}

		}

		protected virtual void RequestHandler(ISessionMetadata client, Request request) {

			if (request.Req == Requests.FileTransfer) {
				var filename = request.Data;
				Responses res = Responses.Error;
				string errormsg = "";
				if (!FileTransferEnabled) {
					res = Responses.Error;
					errormsg = "File transfer is not allowed.";
				}
				else
				{
					if (File.Exists(Path.GetFullPath(filename)))
						res = Responses.FileExists;
					else
						res = Responses.ReqFilePathOk;
				}
				SendPacket(client.Id, Response.CreateResponse(request.RequestGuid, res, errormsg, null).BuildResponseToPacket());
			} else if (request.Req == Requests.FileDelete) {
				Responses res = Responses.Error;
				string errormsg = "";
				var filename = request.Data;

				if (!FileTransferEnabled)
				{
					res = Responses.Error;
					errormsg = "File transfer is not allowed.";
				}
				else {
					try
					{
						File.Delete(Path.GetFullPath(filename));
						res = Responses.FileDeleted;
					}
					catch (Exception ex) {
						res = Responses.Error;
						errormsg = ex.ToString();
					}
				}

				SendPacket(client.Id, Response.CreateResponse(request.RequestGuid, res, errormsg, null).BuildResponseToPacket());
			}
		}

		protected virtual void OnMessageReceivedHandler(ISessionMetadata client, Packet message)
		{

			Statistics?.AddReceivedMessages(1);

			var extraInfo = message.AdditionalInternalInfo;
			var eventHandler = message.GetDynamicCallbackServer(extraInfo, DynamicCallbacks);

			Guid clientGuid;

			if (SocketProtocol == SocketProtocolType.Udp)
			{
				clientGuid = message.GetGuidFromMessage(extraInfo);
				SocketLogger?.Log($"Message contains guid:{clientGuid} trying to link to an existing client.", LogLevel.Trace);
			}

			if (message.MessageType == PacketType.Auth)
			{
				var data = Encoding.UTF8.GetString(message.Data);
				var split = data.Split('|');

				client.ClientName = split[0];
				client.Guid = Guid.Parse(split[1]);
				client.UserDomainName = split[2];
				client.OsVersion = split[3];
			}

			SocketLogger?.Log($"Received a completed message from a client of type {Enum.GetName(typeof(PacketType), message.MessageType)}. {client.Info()}", LogLevel.Trace);

			if (message.MessageType == PacketType.Message)
			{
				var ev = new ClientMessageReceivedEventArgs(message.BuildDataToString(), client, message.MessageMetadata);

				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnClientMessageReceived(ev);
			}

			if (message.MessageType == PacketType.Object)
			{
				var obj = message.BuildObjectFromBytes(extraInfo, out var type);

				if (obj != null && type != null)
				{
					var ev = new ClientObjectReceivedEventArgs(obj, type, client, message.MessageMetadata);
					if (eventHandler != null)
						eventHandler?.Invoke(this, ev);
					else
						OnClientObjectReceived(ev);
				}
				else
					SocketLogger?.Log("Error receiving an object.", LogLevel.Error);
			}

			if (message.MessageType == PacketType.Bytes)
			{
				var ev = new ClientBytesReceivedEventArgs(client, message.Data, message.MessageMetadata);
				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnClientBytesReceived(ev);
			}

			if (message.MessageType == PacketType.File) {
				if (extraInfo == null)
					return;

				var partex = extraInfo.TryGetValue(PacketHelper.PACKETPART, out var part);
				var totex = extraInfo.TryGetValue(PacketHelper.TOTALPACKET, out var total);
				var destex = extraInfo.TryGetValue(PacketHelper.DESTPATH, out var path);

				if (destex) {
					var file = Path.GetFullPath(path.ToString());

					if ((long)part == 0) {
						FileInfo finfo = new FileInfo(file);
						finfo.Directory?.Create();
					}

					using (BinaryWriter writer = new BinaryWriter(File.Open(file, FileMode.Append)))
					{
						writer.Write(message.Data, 0, message.Data.Length);
						writer.Close();
					}
				}
			}

			if (message.MessageType == PacketType.Request) {
				var req = SerializationHelper.DeserializeJson<Request>(message.Data);
				RequestHandler(client, req);
			}

			if (message.MessageType == PacketType.Response) {
				var resp = SerializationHelper.DeserializeJson<Response>(message.Data);
				lock (_responsePackets) {
					_responsePackets.Add(resp.ResponseGuid, resp);
				}
			}

		}

		#endregion

		#region Sending Data

		protected abstract void SendToSocket(int clientId, byte[] payload);

		protected abstract Task<bool> SendToSocketAsync(int clientId, byte[] payload);

		protected bool SendInternal(int clientid, PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionMethod eType, CompressionMethod cType, Type objType = null)
		{

			var packet = PacketBuilder.NewPacket
				.SetBytes(data)
				.SetPacketType(msgType)
				.SetMetadata(metadata)
				.SetCompression(cType)
				.SetEncryption(eType)
				.SetDynamicCallback(eventKey);

			if (msgType == PacketType.Object)
				packet.SetObjectType(objType);

			return SendPacket(clientid, packet.Build());
		}

		protected async Task<bool> SendInternalAsync(int clientId, PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionMethod eType, CompressionMethod cType, Type objType = null)
		{

			var packet = PacketBuilder.NewPacket
				.SetBytes(data)
				.SetPacketType(msgType)
				.SetMetadata(metadata)
				.SetCompression(cType)
				.SetEncryption(eType)
				.SetDynamicCallback(eventKey);

			if (msgType == PacketType.Object)
				packet.SetObjectType(objType);

			return await SendPacketAsync(clientId, packet.Build());
		}

		// Add some extra data to a packet that will be sent.
		private Packet AddDataOntoPacket(Packet packet)
		{
			packet.PreSharedKey = PreSharedKey;
			packet.Logger = SocketLogger;
			packet.EncryptionKey = EncryptionPassphrase;

			if (packet.addDefaultEncryption)
			{
				packet.Encrypt = (EncryptionMethod != EncryptionMethod.None);
				packet.EncryptMode = EncryptionMethod;
			}

			if (packet.addDefaultCompression)
			{
				packet.Compress = (CompressionMethod != CompressionMethod.None);
				packet.CompressMode = CompressionMethod;
			}

			return packet;
		}

		/// <summary>
		/// Send a packet build with <seealso cref="PacketBuilder"/>.
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool SendPacket(int clientId, Packet packet)
		{

			try
			{
				var p = AddDataOntoPacket(packet);
				SendToSocket(clientId, p.BuildPayload());
				return true;
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error sending a packet.", ex, LogLevel.Error);
				return false;
			}
		}

		/// <summary>
		/// Sends a packet build with <seealso cref="PacketBuilder"/>
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		public async Task<bool> SendPacketAsync(int clientId, Packet packet)
		{
			try
			{
				var p = AddDataOntoPacket(packet);
				return await SendToSocketAsync(clientId, p.BuildPayload());
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error sending a packet.", ex, LogLevel.Error);
				return false;
			}
		}

		#region Message

		public bool SendMessage(int clientId, string message)
		{
			return SendInternal(clientId, PacketType.Message, Encoding.UTF8.GetBytes(message), null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		public bool SendMessage(int clientId, string message, IDictionary<object, object> metadata)
		{
			return SendInternal(clientId, PacketType.Message, Encoding.UTF8.GetBytes(message), null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendMessageAsync(int clientId, string message)
		{
			return await SendInternalAsync(clientId, PacketType.Message, Encoding.UTF8.GetBytes(message), null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendMessageAsync(int clientId, string message, IDictionary<object, object> metadata)
		{
			return await SendInternalAsync(clientId, PacketType.Message, Encoding.UTF8.GetBytes(message), metadata, string.Empty, EncryptionMethod, CompressionMethod);
		}

		#endregion

		#region Bytes

		public bool SendBytes(int clientId, byte[] bytes)
		{
			return SendInternal(clientId, PacketType.Bytes, bytes, null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		public bool SendBytes(int clientId, byte[] bytes, IDictionary<object, object> metadata)
		{
			return SendInternal(clientId, PacketType.Bytes, bytes, null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendBytesAsync(int clientId, byte[] bytes)
		{
			return await SendInternalAsync(clientId, PacketType.Bytes, bytes, null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendMessageAsync(int clientId, byte[] bytes, IDictionary<object, object> metadata)
		{
			return await SendInternalAsync(clientId, PacketType.Message, bytes, metadata, string.Empty, EncryptionMethod, CompressionMethod);
		}


		#endregion

		#region Object

		public bool SendObject(int clientId, object obj)
		{
			return SendObject(clientId, obj, null);
		}

		public bool SendObject(int clientId, object obj, IDictionary<object, object> metadata)
		{
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);
			return SendInternal(clientId, PacketType.Object, bytes, metadata, string.Empty, EncryptionMethod, CompressionMethod, obj.GetType());
		}

		public async Task<bool> SendObjectAsync(int clientId, object obj)
		{
			return await SendObjectAsync(clientId, obj, null);
		}

		public async Task<bool> SendObjectAsync(int clientId, object obj, IDictionary<object, object> metadata)
		{
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);
			return await SendInternalAsync(clientId, PacketType.Object, bytes, metadata, string.Empty, EncryptionMethod, CompressionMethod, obj.GetType());
		}


		#endregion

		#endregion

		/// <summary>
		/// Disposes of the server.
		/// </summary>
		public override void Dispose()
		{
			try
			{
				if (!Disposed)
				{
					TokenSource.Cancel();
					TokenSource.Dispose();
					Listening = false;
					Listener.Dispose();
					CanAcceptConnections.Dispose();

					foreach (var id in ConnectedClients.Keys.ToList())
					{
						ShutDownClient(id, DisconnectReason.Kicked);
					}

					ConnectedClients = new Dictionary<int, ISessionMetadata>();
					TokenSource.Dispose();
					Disposed = true;
					GC.SuppressFinalize(this);
				}
				else
				{
					throw new ObjectDisposedException(nameof(SimpleTcpServer), "This object is already disposed.");
				}

			}
			catch (Exception ex)
			{
				SocketLogger?.Log(ex, LogLevel.Error);
			}
		}


		#region Helper Methods

		internal void ShutDownClient(int id, DisconnectReason reason)
		{
			var client = GetClientMetadataById(id);

			if (client == null)
				SocketLogger?.Log("Cannot shutdown client " + id + ", does not exist.", LogLevel.Warning);

			try
			{
				if (client?.Listener != null)
				{
					client.Listener.Shutdown(SocketShutdown.Both);
					client.Listener.Close();
					client.Listener = null;
					SocketLogger?.Log($"client has been closed. {client.Info()}", LogLevel.Trace);
				}
			}
			catch (ObjectDisposedException de)
			{
				SocketLogger?.Log("Cannot shutdown client " + id + ", has already been disposed.", de, LogLevel.Warning);
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("An error occurred when shutting down client " + id, ex, LogLevel.Warning);
			}
			finally
			{
				lock (ConnectedClients)
				{
					ConnectedClients.Remove(id);
					OnClientDisconnected(new ClientDisconnectedEventArgs(client, reason));
				}
			}
		}

		internal ISessionMetadata GetClientMetadataById(int id)
		{
			return ConnectedClients.TryGetValue(id, out var state) ? state : null;
		}

		internal ISessionMetadata GetClientMetadataByGuid(Guid guid)
		{
			return ConnectedClients?.Values.Single(x => x.Guid == guid);
		}

		protected IPAddress ListenerIPFromString(string ip)
		{
			try
			{

				if (string.IsNullOrEmpty(ip) || ip.Trim() == "*")
				{
					var ipAdr = IPAddress.Any;
					ListenerIp = ipAdr.ToString();
					return ipAdr;
				}

				return IPAddress.Parse(ip);
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Invalid Server IP", ex, LogLevel.Fatal);
				return null;
			}
		}

		//Check if the server should allow the client that is attempting to connect.
		internal bool IsConnectionAllowed(ISessionMetadata state)
		{
			if (WhiteList.Count > 0)
			{
				return CheckWhitelist(state.IPv4) || CheckWhitelist(state.IPv6);
			}

			if (BlackList.Count > 0)
			{
				return !CheckBlacklist(state.IPv4) && !CheckBlacklist(state.IPv6);
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

		#endregion

	}

}