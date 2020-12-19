using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Helpers.Serialization;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;

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

		/// <summary>
		/// Event fired when a part of a file has been sent
		/// </summary>
		public event EventHandler<FileTransferUpdateEventArgs> FileTransferUpdate;
		protected virtual void OnFileTransferUpdate(FileTransferUpdateEventArgs eventArgs) => FileTransferUpdate?.Invoke(this,eventArgs);

		public event EventHandler<FileTransferUpdateEventArgs> FileTransferReceivingUpdate;
		protected virtual void OnFileTransferReceivingUpdate(FileTransferUpdateEventArgs eventArgs) => FileTransferReceivingUpdate?.Invoke(this, eventArgs);

		/// <summary>
		/// Fired when the client receives a request.
		/// The return value will be send back to the server.
		/// </summary>
		public Func<string, object, Type, object> RequestHandler = null;

		#endregion

		protected int ReconnectAttempt = 0;

		private bool _connected;
		
		private Action<string> _logger;

		protected readonly ManualResetEvent Connected = new ManualResetEvent(false);

		protected readonly ManualResetEvent Sent = new ManualResetEvent(false);

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
		/// When this is set to 0 the client will never try ro reconnect automatically.
		/// The default value of this parameters is 5 seconds.
		/// </summary>
		public TimeSpan AutoReconnect { get; protected set; }

		/// <summary>
		/// Max amounts of automatic reconnection attempts the client will try.
		/// </summary>
		/// <value></value>
		public int MaxAttempts {get; protected set;}

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
			MaxAttempts = 20;
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
					ShutDownConnectionLost();
					return false;
				}

				return !((Listener.Poll(1000, SelectMode.SelectRead) && (Listener.Available == 0)) || !Listener.Connected);
			}
			catch (Exception)
			{
				ShutDownConnectionLost();
				return false;
			}
		}

		/// <summary>
		/// Connect the client to a given ip:port
		/// </summary>
		/// <param name="serverIp"></param>
		/// <param name="serverPort"></param>
		/// <param name="autoReconnect">Amount of seconds the client waits before trying to reconnect.</param>
		public abstract void ConnectTo(string serverIp, int serverPort, TimeSpan autoReconnect, int maxReconnectAttempts);

		/// <summary>
		/// Connect the client to a given ip:port
		/// </summary>
		/// <param name="serverIp"></param>
		/// <param name="serverPort"></param>
		/// <param name="autoReconnect">Amount of seconds the client waits before trying to reconnect.</param>
		public void ConnectTo(string serverIp, int serverPort, TimeSpan autoReconnect) => ConnectTo(serverIp, serverPort, autoReconnect, MaxAttempts);

		/// <summary>
		/// Connects the client to a given ip:port.
		/// By default the client will try to reconnect every 5 seconds if no connection is established.
		/// </summary>
		/// <param name="serverIp"></param>
		/// <param name="serverPort"></param>
		public void ConnectTo(string serverIp, int serverPort) => ConnectTo(serverIp, serverPort, AutoReconnect, MaxAttempts);

		// For internal use, if the client loses connection this method should be used.
		// If this method is called the client will try to reconnect to the server every X seconds.
		// When ShutDown() is called by a user this is not the case.
		protected virtual void ShutDownConnectionLost() {
			try  {
				if (Disposed || !_connected)
					return;

				SocketLogger?.Log("Connection to the server has been lost.", LogLevel.Trace);

				ShutDown();

				// Attempt to reconnect after losing connection.
				if (AutoReconnect.TotalMilliseconds > 0)
					ConnectTo(ServerIp, ServerPort, AutoReconnect);
			} catch (Exception ex) {
				SocketLogger?.Log("Error closing the client", ex, LogLevel.Error);
			}
		}

		/// <summary>
		/// Shutdowns the client
		/// </summary>
		public virtual void ShutDown()
		{
			try
			{
				if (Disposed)
					return;

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
		protected virtual void OnMessageReceivedHandler(ISessionMetadata session, Packet packet)
		{

			Statistics?.AddReceivedMessages(1);

			var extraInfo = packet.AdditionalInternalInfo;
			var eventHandler = packet.GetDynamicCallbackClient(extraInfo, DynamicCallbacks);

			SocketLogger?.Log($"Received a completed message from the server of type {nameof(packet.MessageType)}.", LogLevel.Trace);

			if (packet.MessageType == PacketType.Message)
			{
				var ev = new MessageReceivedEventArgs(packet.BuildDataToString(), packet.MessageMetadata);

				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnMessageReceived(ev);
			}else if (packet.MessageType == PacketType.File) {
				if (extraInfo == null)
					return;

				var partExists = extraInfo.TryGetValue(PacketHelper.PACKETPART, out var part);
				var totalExists = extraInfo.TryGetValue(PacketHelper.TOTALPACKET, out var total);
				var destExists = extraInfo.TryGetValue(PacketHelper.DESTPATH, out var path);

				if (destExists) {
					var file = Path.GetFullPath(path.ToString());
					var fileInfo = new FileInfo(file);

					if ((long)part == 0) {
						fileInfo.Directory?.Create();
					}

					using (BinaryWriter writer = new BinaryWriter(File.Open(file, FileMode.Append)))
					{
						writer.Write(packet.Data, 0, packet.Data.Length);
						writer.Close();
					}

					OnFileTransferReceivingUpdate(new FileTransferUpdateEventArgs((long)part, (long)total,fileInfo, file));
				}
			}else if (packet.MessageType == PacketType.Object)
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
			} else if (packet.MessageType == PacketType.Bytes)
			{
				var ev = new BytesReceivedEventArgs(packet.Data, packet.MessageMetadata);
				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnBytesReceived(ev);	
			} else if (packet.MessageType == PacketType.Response) {
				var response = SerializationHelper.DeserializeJson<Response>(packet.Data);

				lock (_responsePackets) {
					_responsePackets.Add(response.ResponseGuid, response);
				}
			} else if (packet.MessageType == PacketType.Request)
			{
				var req = SerializationHelper.DeserializeJson<Request>(packet.Data);
				OnRequestReceived(session, req);
			}
			
		}

		protected virtual void OnRequestReceived(ISessionMetadata session, Request request) {

			ResponseType res = ResponseType.Error;
			string errormsg = "";

			try {

				// if filetransfer not allowed throw an error
				if ( (int)request.Req < 2 && !FileTransferEnabled) {
					res = ResponseType.Error;
					errormsg = "Filetransfer is not allowed.";	
					SendPacket(Response.CreateResponse(request.RequestGuid, res, errormsg, null).BuildResponseToPacket());
					return;
				}

				object responseObject = null;
				if (request.Req == RequestType.FileTransfer) {
					var filename = request.Data.ToString();
					
					if (File.Exists(Path.GetFullPath(filename)))
						res = ResponseType.FileExists;
					else
						res = ResponseType.ReqFilePathOk;

				} else if (request.Req == RequestType.FileDelete) {
					var filename = request.Data.ToString();
					File.Delete(Path.GetFullPath(filename));
					res = ResponseType.FileDeleted;
				} else if (request.Req == RequestType.UdpMessage) {
					string data = request.Data.ToString();
					session.ResetDataReceiver();
					ByteDecoder(session, PacketHelper.StringToByteArray(data));
					
					// Return response to server that message was received.
					res = ResponseType.UdpResponse;
				} else if (request.Req == RequestType.CustomReq) {
					object content = null;
			
					if (request.Data.GetType() == typeof(JObject)) {
						content = ((JObject)request.Data).ToObject(request.DataType);
					}

					if (request.Data.GetType() == typeof(JArray)){
						content = ((JArray)request.Data).ToObject(request.DataType);
					}

					if (content == null)
						content = request.Data;

					res = ResponseType.CustomResponse;
					responseObject = RequestHandler(request.Header, content, request.DataType);
				}

				SendPacket(Response.CreateResponse(request.RequestGuid, res, errormsg, null, responseObject).BuildResponseToPacket());
			} catch (Exception ex) {
				res = ResponseType.Error;
				errormsg = ex.ToString();
				SendPacket(Response.CreateResponse(request.RequestGuid, res, errormsg, ex).BuildResponseToPacket());
			}

		}

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

		protected abstract bool SendToServer(byte[] payload);

		protected abstract Task<bool> SendToServerAsync(byte[] payload);

		protected virtual bool SendInternal(PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionMethod encryption, CompressionMethod compression, Type objType = null)
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

		protected virtual async Task<bool> SendInternalAsync(PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionMethod encryption, CompressionMethod compression, Type objType = null)
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
		protected Packet AddDataOntoPacket(Packet packet) {
			
			if (SocketProtocol == SocketProtocolType.Udp)
			{
				var info = packet.AdditionalInternalInfo;
				if (info == null)
					info = new Dictionary<object, object>();
				info.Add(PacketHelper.GUID, ClientGuid);
				packet.AdditionalInternalInfo = info;
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
		public virtual bool SendPacket(Packet packet) {

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
		public virtual async Task<bool> SendPacketAsync(Packet packet) {
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
			return SendInternal(PacketType.Message, Encoding.UTF8.GetBytes(message), metadata, string.Empty, EncryptionMethod, CompressionMethod);
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
			return SendInternal(PacketType.Bytes, bytes, metadata, string.Empty, EncryptionMethod, CompressionMethod);
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

		private bool SendFileRequests(string file , string remoteloc, bool overwrite) {
			file = Path.GetFullPath(file);

			if (!File.Exists(file))
				throw new ArgumentException("No file found at path.", nameof(file));

			var response = RequestFileTransfer(10000, remoteloc);

			if (response.Resp == ResponseType.Error) {
				throw new InvalidOperationException(response.ExceptionMessage,response.Exception);
			}

			if (response.Resp == ResponseType.FileExists) {
				if (overwrite)
				{
					response = RequestFileDelete(10000, remoteloc);
					if (response.Resp == ResponseType.FileDeleted)
						SocketLogger?.Log($"File at {remoteloc} was removed from remote location.", LogLevel.Trace);
					else if (response.Resp == ResponseType.Error)
						throw new InvalidOperationException(response.ExceptionMessage, response.Exception);
					else
						throw new InvalidOperationException("Invalid response received.");
				}
				else
					throw new InvalidOperationException($"A file already exists on the remote location at {remoteloc}.");
			}

			return true;
		}

		public bool SendFile(string file, string remoteloc, bool overwrite) => SendFile(file, remoteloc, overwrite, null);

		public bool SendFile(string file, string remoteloc, bool overwrite, IDictionary<object,object> metadata) {

			try {
				SendFileRequests(file, remoteloc, overwrite);

				var start = DateTime.Now;
				var bufferLength = 4096;
				var buffer = new byte[bufferLength];

				using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true)) {
					var read = 0;
					var currentPart = 0;
					var totalLength = fileStream.Length;
					double temp = ((double)totalLength/(double)bufferLength);
					int totalParts = (int)Math.Ceiling(temp);

					while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0) {
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
							.SetMetadata(metadata)
							.Build();

						SocketLogger?.Log($"Sending part {currentPart} of {totalParts} of {file}.", LogLevel.Trace);
						var send = SendPacket(packet);

						if (!send)
						{
							SocketLogger?.Log($"Part {currentPart} of {totalParts} failed to be sent.", LogLevel.Error);
							return false;
						}

						OnFileTransferUpdate(new FileTransferUpdateEventArgs(currentPart, totalParts, new FileInfo(file), remoteloc));
						buffer = new byte[bufferLength];
					}
				}

				SocketLogger?.Log($"Filetransfer [{file}] -> [{remoteloc}] took  {(DateTime.Now.ToUniversalTime() - start.ToUniversalTime())}.", LogLevel.Trace);
				return true;
			} catch (Exception ex) {
				SocketLogger?.Log("Error sending a file.", ex, LogLevel.Error);
				return false;
			}

		}

		public Task<bool> SendFileAsync(string file, string remoteloc, bool overwrite) => SendFileAsync(file, remoteloc, overwrite, null);

		public async Task<bool> SendFileAsync(string file, string remoteloc, bool overwrite, IDictionary<object,object> metadata) {

			try
			{
				SendFileRequests(file, remoteloc, overwrite);

				var start = DateTime.Now;
				var bufLength = 4096;
				var buffer = new byte[bufLength]; //When this buffer exceeds 85000 bytes -> buffer will be stored in LOH -> bad for memory usage.			
				using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true))
				{
					var read = 0;
					var currentPart = 0;
					var totalLength = fileStream.Length;
					double temp = ((double)totalLength/(double)bufLength);
					int totalParts = (int)Math.Ceiling(temp);

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
							.SetMetadata(metadata)
							.Build();

						SocketLogger?.Log($"Sending part {currentPart} of {totalParts} of {file}.", LogLevel.Trace);
						var send = await SendPacketAsync(packet);

						if (!send)
						{
							SocketLogger?.Log($"Part {currentPart} or {totalParts} failed to be sent.", LogLevel.Error);
							return false;
						}
						OnFileTransferUpdate(new FileTransferUpdateEventArgs(currentPart, totalParts, new FileInfo(file), remoteloc));

						buffer = new byte[bufLength];
					}
				}

				SocketLogger?.Log($"Filetransfer [{file}] -> [{remoteloc}] took  {(DateTime.Now.ToUniversalTime() - start.ToUniversalTime())}.", LogLevel.Trace);
				return true;
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error sending a file.", ex, LogLevel.Error);
				return false;
			}
		}


		#endregion

		#region Requests 
		
		public object SendRequest(int responseTimeInMs,string header, object data) {
			return SendRequest<object>(responseTimeInMs,header, data);
		}

		public T SendRequest<T>(int responseTimeInMs,string header, object data) {
			var req = Request.CustomRequest(responseTimeInMs,header, data);
			SendPacket(req.BuildRequestToPacket());
			var response = GetResponse(req.RequestGuid, req.Expiration);
			
			if (response.Resp == ResponseType.Error) {
				throw new InvalidOperationException(response.ExceptionMessage);
			}

			object content = null;

			if (response.DataType != null) {
				if (response.Data.GetType() == typeof(JObject)) {
					content = ((JObject)response.Data).ToObject(response.DataType);
				}

				if (response.Data.GetType() == typeof(JArray)){
					content = ((JArray)response.Data).ToObject(response.DataType);
				}
			}

			if (content == null)
				content = response.Data;

			return (T) content;
		}
		
		#endregion

		#endregion

	}

}
