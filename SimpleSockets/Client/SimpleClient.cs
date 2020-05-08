using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Helpers.Serialization;
using SimpleSockets.Messaging;

namespace SimpleSockets.Client {

    public abstract class SimpleClient : SimpleSocket
    {

		#region Events

		/// <summary>
		/// Event fired when a client is connected to a server.
		/// </summary>
		public event EventHandler ConnectedToServer;
		protected virtual void OnConnectedToServer() {
			if (_connected == false) {
				_connected = true;
				ConnectedToServer?.Invoke(this, null);
			}
		} 

		/// <summary>
		/// Event fired when a client is disconnected from the server
		/// </summary>
		public event EventHandler DisconnectedFromServer;
		protected virtual void OnDisconnectedFromServer() {
			if (_connected) {
				DisconnectedFromServer?.Invoke(this, null);
				_connected = false;
			}
		}

		/// <summary>
		/// Event fired when the client received a message.
		/// </summary>
		public event EventHandler<MessageReceivedEventArgs> MessageReceived;
		protected virtual void OnMessageReceived(MessageReceivedEventArgs eventArgs) => MessageReceived?.Invoke(this, eventArgs);

		/// <summary>
		/// Event fired when the client received a message.
		/// </summary>
		public event EventHandler<ObjectReceivedEventArgs> ObjectReceived;
		protected virtual void OnObjectReceived(ObjectReceivedEventArgs eventArgs) => ObjectReceived?.Invoke(this, eventArgs);

		/// <summary>
		/// Event fired when the client received bytes.
		/// </summary>
		public event EventHandler<BytesReceivedEventArgs> BytesReceived;
		protected virtual void OnBytesReceived(BytesReceivedEventArgs eventArgs) => BytesReceived?.Invoke(this, eventArgs);

		/// <summary>
		/// Event fired when the client failed to send a message.
		/// </summary>
		public event EventHandler<MessageFailedEventArgs> MessageFailed;
		protected virtual void OnMessageFailed(MessageFailedEventArgs eventArgs) => MessageFailed?.Invoke(this, eventArgs);

		#endregion

		private bool _connected;

		private Action<string> _logger;

		protected readonly ManualResetEventSlim Connected = new ManualResetEventSlim(false);

		protected readonly ManualResetEventSlim Sent = new ManualResetEventSlim(false);

		private Guid _clientGuid;

		/// <summary>
		/// The guid of a client.
		/// This is an unique identifier that will be transmitted to the server.
		/// This makes sure that after a disconnect the server can still know what client this is.
		/// </summary>
		public Guid ClientGuid
		{
			get
			{
				if (_clientGuid == Guid.Empty)
					_clientGuid = Guid.NewGuid();
				return _clientGuid;
			}
		}

		/// <summary>
		/// Used to log exceptions/messages.
		/// </summary>
		public override Action<string> Logger { 
            get => _logger;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SocketLogger = LogHelper.InitializeLogger(true, SocketProtocolType.Tcp == this.SocketProtocol, value, this.LoggerLevel);
                _logger = value;
            }
        }

		/// <summary>
		/// The ip of the server.
		/// </summary>
		public string ServerIp { get; protected set; }

		/// <summary>
		/// The port of the server.
		/// </summary>
		public int ServerPort { get; protected set; }

		/// <summary>
		/// Time the client waits to autoreconnect.
		/// </summary>
		public TimeSpan AutoReconnect { get; protected set; }

		/// <summary>
		/// The endpoint of the server.
		/// </summary>
		public IPEndPoint EndPoint { get; protected set; }

		// The listener socket
		protected Socket Listener { get; set; }

		/// <summary>
		/// Dynamic events that can be added to the client.
		/// </summary>
		public IDictionary<string, EventHandler<DataReceivedEventArgs>> DynamicCallbacks { get; protected set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="useSsl"></param>
		/// <param name="protocol"></param>
		public SimpleClient(SocketProtocolType protocol) : base(protocol) {
			_connected = false;
			AutoReconnect = new TimeSpan(0, 0, 5);
			DynamicCallbacks = new Dictionary<string, EventHandler<DataReceivedEventArgs>>(); ;
		}

		/// <summary>
		/// Returns true if connected, false if not
		/// </summary>
		/// <returns></returns>
		public virtual bool IsConnected()
		{
			try
			{
				if (Listener == null) {
					OnDisconnectedFromServer();
					return false;
				}

				return !((Listener.Poll(1000, SelectMode.SelectRead) && (Listener.Available == 0)) || !Listener.Connected);
			}
			catch (Exception)
			{
				OnDisconnectedFromServer();
				return false;
			}
		}

		/// <summary>
		/// Connect the client to a given ip:port
		/// </summary>
		/// <param name="serverIp"></param>
		/// <param name="serverPort"></param>
		/// <param name="autoReconnect">Amount of seconds the client waits before trying to reconnect.</param>
		public abstract void ConnectTo(string serverIp, int serverPort, int autoReconnect);

		/// <summary>
		/// Connects the client to a given ip:port.
		/// By default the client will try to reconnect every 5 seconds if no connection is established.
		/// </summary>
		/// <param name="serverIp"></param>
		/// <param name="serverPort"></param>
		public void ConnectTo(string serverIp, int serverPort) => ConnectTo(serverIp, serverPort, AutoReconnect.Seconds);

		/// <summary>
		/// Shutdowns the client
		/// </summary>
		public virtual void ShutDown()
		{
			try
			{
				Connected.Reset();
				if (Listener != null)
				{
					Listener.Shutdown(SocketShutdown.Both);
					Listener.Close();
					Listener = null;
					OnDisconnectedFromServer();
				}
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error closing the client", ex, LogLevel.Error);
			}
		}

		/// <summary>
		/// Disposes of the client
		/// </summary>
		public override void Dispose()
		{
			try
			{
				if (!Disposed)
				{
					ShutDown();
					TokenSource.Cancel();
					Sent.Dispose();
					Connected.Dispose();
					Disposed = true;
				}
			}
			catch (Exception ex)
			{
				SocketLogger?.Log(ex, LogLevel.Error);
			}
		}

		#region Helper-Methods

		// Handles the received packets
		protected virtual void OnMessageReceivedHandler(Packet packet)
		{

			Statistics?.AddReceivedMessages(1);

			var extraInfo = packet.AdditionalInternalInfo;
			var eventHandler = packet.GetDynamicCallbackClient(extraInfo, DynamicCallbacks);

			SocketLogger?.Log($"Received a completed message from the server of type {Enum.GetName(typeof(PacketType), packet.MessageType)}.", LogLevel.Trace);

			if (packet.MessageType == PacketType.Message)
			{
				var ev = new MessageReceivedEventArgs(packet.BuildDataToString(), packet.MessageMetadata);

				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnMessageReceived(ev);
			}


			if (packet.MessageType == PacketType.Object)
			{
				var obj = packet.BuildObjectFromBytes(extraInfo, out var type);
				var ev = new ObjectReceivedEventArgs(obj, type, packet.MessageMetadata);

				if (obj != null && type != null)
				{
					if (eventHandler != null)
						eventHandler?.Invoke(this, ev);
					else
						OnObjectReceived(ev);
				}
				else
					SocketLogger?.Log("Error receiving an object.", LogLevel.Error);
			}

			if (packet.MessageType == PacketType.Bytes)
			{
				var ev = new BytesReceivedEventArgs(packet.Data, packet.MessageMetadata);
				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnBytesReceived(ev);
			}

			if (packet.MessageType == PacketType.Response) {
				var response = SerializationHelper.DeserializeJson<Response>(packet.Data);

				lock (_responsePackets) {
					_responsePackets.Add(response.ResponseGuid, response);
				}
			}

			if (packet.MessageType == PacketType.Request)
			{
				var req = SerializationHelper.DeserializeJson<Request>(packet.Data);
				RequestHandler(req);
			}
		}

		protected virtual void RequestHandler(Request req) {

		}

		protected override void ByteDecoder(ISessionMetadata session, byte[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == PacketHelper.ESCAPE)
				{
					if (i < array.Length)
					{
						i++;
						if (array[i] == PacketHelper.EOF)
						{
							var msg = session.DataReceiver.BuildMessageFromPayload(EncryptionPassphrase, PreSharedKey);
							if (msg != null)
								OnMessageReceivedHandler(msg);
							session.ResetDataReceiver();
						}
						else
							session.DataReceiver.AppendByteToReceived(array[i]);
					}
				}
				else
					session.DataReceiver.AppendByteToReceived(array[i]);
			}
		}

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

		#region Sending Data

		protected abstract void SendToServer(byte[] payload);

		protected abstract Task<bool> SendToServerAsync(byte[] payload);

		protected bool SendInternal(PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionMethod encryption, CompressionMethod compression, Type objType = null)
		{
			var packet = PacketBuilder.NewPacket
				.SetBytes(data)
				.SetPacketType(msgType)
				.SetMetadata(metadata)
				.SetCompression(compression)
				.SetEncryption(encryption)
				.SetDynamicCallback(eventKey);

			if (msgType == PacketType.Object)
				packet.SetObjectType(objType);

			return SendPacket(packet.Build());
		}

		protected async Task<bool> SendInternalAsync(PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionMethod encryption, CompressionMethod compression, Type objType = null)
		{
			var packet = PacketBuilder.NewPacket
				.SetBytes(data)
				.SetPacketType(msgType)
				.SetMetadata(metadata)
				.SetCompression(compression)
				.SetEncryption(encryption)
				.SetDynamicCallback(eventKey);

			if (msgType == PacketType.Object)
				packet.SetObjectType(objType);

			return await SendPacketAsync(packet.Build());
		}

		// Sends an authentication to the server. this way the server can always identify a client based on the guid.
		protected bool SendAuthenticationMessage() {
			var username = Environment.UserName;
			var osVersion = Environment.OSVersion;
			var user = Environment.UserDomainName;

			//Keep existing GUID
			var guid = ClientGuid;

			var msg = username + "|" + guid + "|" + user + "|" + osVersion;

			return SendInternal(PacketType.Auth, Encoding.UTF8.GetBytes(msg),null,string.Empty,EncryptionMethod,CompressionMethod);
		}

		// Add some extra data to a packet that will be sent.
		private Packet AddDataOntoPacket(Packet packet) {
			if (SocketProtocol == SocketProtocolType.Udp)
			{
				var info = packet.AdditionalInternalInfo;
				if (info == null)
					info = new Dictionary<object, object>();
				info.Add(PacketHelper.GUID, ClientGuid);
			}

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
		/// Send a packet to the server. The packet build with <seealso cref="PacketBuilder"/>.
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool SendPacket(Packet packet) {

			try
			{
				var p = AddDataOntoPacket(packet);
				SendToServer(p.BuildPayload());
				return true;
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error sending a packet.", ex, LogLevel.Error);
				return false;
			}
		}

		/// <summary>
		/// Sends a packet to the server asynchronous. The packet is build with <seealso cref="PacketBuilder"/>
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		public async Task<bool> SendPacketAsync(Packet packet) {
			try
			{
				var p = AddDataOntoPacket(packet);
				return await SendToServerAsync(p.BuildPayload());
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error sending a packet.", ex, LogLevel.Error);
				return false;
			}
		}

		#region Message

		/// <summary>
		/// Sends a message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public bool SendMessage(string message) {
			return SendInternal(PacketType.Message, Encoding.UTF8.GetBytes(message), null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends a message with metadata to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public bool SendMessage(string message, IDictionary<object, object> metadata) {
			return SendInternal(PacketType.Message, Encoding.UTF8.GetBytes(message), null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Send a message to the server asynchronous.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task<bool> SendMessageAsync(string message) {
			return await SendInternalAsync(PacketType.Message, Encoding.UTF8.GetBytes(message), null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Send a message to the server asynchronous.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public async Task<bool> SendMessageAsync(string message, IDictionary<object, object> metadata) {
			return await SendInternalAsync(PacketType.Message, Encoding.UTF8.GetBytes(message), metadata, string.Empty, EncryptionMethod, CompressionMethod);
		}

		#endregion

		#region Bytes

		/// <summary>
		/// Send bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public bool SendBytes(byte[] bytes)
		{
			return SendInternal(PacketType.Bytes, bytes, null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Send bytes to the server with metadata.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public bool SendBytes(byte[] bytes, IDictionary<object, object> metadata)
		{
			return SendInternal(PacketType.Bytes, bytes, null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Send bytes to the server asynchronous.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public async Task<bool> SendBytesAsync(byte[] bytes)
		{
			return await SendInternalAsync(PacketType.Bytes, bytes, null, string.Empty, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Send bytes with metadata to the server asynchronous.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public async Task<bool> SendMessageAsync(byte[] bytes, IDictionary<object, object> metadata)
		{
			return await SendInternalAsync(PacketType.Message, bytes, metadata, string.Empty, EncryptionMethod, CompressionMethod);
		}


		#endregion

		#region Object

		/// <summary>
		/// Send an object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool SendObject(object obj)
		{
			return SendObject(obj, null);
		}

		/// <summary>
		/// Send an object with metadata to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public bool SendObject(object obj, IDictionary<object, object> metadata)
		{
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);
			return SendInternal(PacketType.Object, bytes, metadata, string.Empty, EncryptionMethod, CompressionMethod, obj.GetType());
		}

		/// <summary>
		/// Send an object to the server asynchronous.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public async Task<bool> SendObjectAsync(object obj)
		{
			return await SendObjectAsync(obj, null);
		}

		/// <summary>
		/// Send an object with metadata to the server asynchronous.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public async Task<bool> SendObjectAsync(object obj, IDictionary<object, object> metadata)
		{
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);
			return await SendInternalAsync(PacketType.Object, bytes, metadata, string.Empty, EncryptionMethod, CompressionMethod, obj.GetType());
		}


		#endregion

		#region File

		private Response GetResponse(Guid guid, DateTime expiration) {
			Response packet = null;
			while (!Token.IsCancellationRequested)
			{
				lock (_responsePackets)
				{
					if (_responsePackets.ContainsKey(guid))
					{
						packet = _responsePackets[guid];
						_responsePackets.Remove(guid);
						return packet;
					}
				}
				if (DateTime.Now >= expiration)
					break;
				Task.Delay(50).Wait();
			}

			throw new TimeoutException("No response received within the expected time window.");
		}

		private Response RequestFileTransfer(int responseTimeInMs, string filename) {
			var req = Request.FileTransferRequest(filename, responseTimeInMs);
			SendPacket(req.BuildRequestToPacket());
			return GetResponse(req.RequestGuid, req.Expiration);
		}

		private Response RequestFileDelete(int responseTimeInMs, string filename) {
			var req = Request.FileDeletionRequest(filename, responseTimeInMs);
			SendPacket(req.BuildRequestToPacket());
			return GetResponse(req.RequestGuid, req.Expiration);
		}

		public async Task<bool> SendFileAsync(string file, string remoteloc, bool overwrite) {

			file = Path.GetFullPath(file);

			if (!File.Exists(file))
				throw new ArgumentException("No file found at path.", nameof(file));

			var response = RequestFileTransfer(5000, remoteloc);

			if (response.Resp == Responses.Error) {
				throw new InvalidOperationException(response.ExceptionMessage,response.Exception);
			}

			if (response.Resp == Responses.FileExists) {
				if (overwrite)
				{
					response = RequestFileDelete(10000, remoteloc);
					if (response.Resp == Responses.FileDeleted)
						SocketLogger?.Log($"File at {remoteloc} was removed from remote location.", LogLevel.Trace);
					else if (response.Resp == Responses.Error)
						throw new InvalidOperationException(response.ExceptionMessage, response.Exception);
					else
						throw new InvalidOperationException("Invalid response received.");
				}
				else
					throw new InvalidOperationException($"A file already exists on the remote location at {remoteloc}.");
			}

			try
			{
				var start = DateTime.Now;
				var bufLength = 4096;
				var buffer = new byte[bufLength]; //When this buffer exceeds 85000 bytes -> buffer will be stored in LOH -> bad for memory usage.			
				using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true))
				{
					var read = 0;
					var currentPart = 0;
					var totalLength = fileStream.Length;
					int totalParts = (int)Math.Ceiling((double)(totalLength / bufLength));

					while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length, Token)) > 0)
					{
						currentPart++;
						var data = new byte[read];
						if (read == buffer.Length)
							data = buffer;
						else
							Array.Copy(buffer, 0, data, 0, read);

						var packet = PacketBuilder.NewPacket
							.SetBytes(data)
							.SetPacketType(PacketType.File)
							.SetPartNumber(currentPart, totalParts)
							.SetEncryption(EncryptionMethod)
							.SetDestinationPath(remoteloc)
							.Build();


						var send = await SendToServerAsync(packet.BuildPayload());

						if (send == false)
						{
							SocketLogger?.Log($"Part {currentPart} or {totalParts} failed to be sent.", LogLevel.Error);
							return false;
						}

						buffer = new byte[bufLength];
					}
				}

				Console.WriteLine("Took: " + (start.ToUniversalTime() - DateTime.Now.ToUniversalTime()).ToString());
				return true;
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error sending a file.", ex, LogLevel.Error);
				return false;
			}
		}

		#endregion

		#endregion

	}

}