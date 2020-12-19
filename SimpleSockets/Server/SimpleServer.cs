using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Helpers.Serialization;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;
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

		/// <summary>
		/// Event fired when a part of a file has been sent
		/// </summary>
		public event EventHandler<ClientFileTransferUpdateEventArgs> FileTransferUpdate;
		protected virtual void OnClientFileTransferUpdate(ClientFileTransferUpdateEventArgs eventArgs) => FileTransferUpdate?.Invoke(this, eventArgs);

		/// <summary>
		/// Event fired when a part of a file has been received.
		/// </summary>
		public event EventHandler<ClientFileTransferUpdateEventArgs> FileTransferReceivingUpdate;
		protected virtual void OnClientFileTransferReceivingUpdate(ClientFileTransferUpdateEventArgs eventArgs) => FileTransferReceivingUpdate?.Invoke(this, eventArgs);

		/// <summary>
		/// Fired when the server receives a request.
		/// The return value will be send back to the corresponding client.
		/// </summary>
		public Func<ISessionInfo,string, object, Type, object> RequestHandler = null;

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

		protected readonly ManualResetEvent CanAcceptConnections = new ManualResetEvent(false);

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
					if (SocketProtocolType.Udp == SocketProtocol)
						return (socket.Poll(1000, SelectMode.SelectRead));

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

		protected virtual void OnRequestReceived(ISessionMetadata client, Request request) {
			ResponseType res = ResponseType.Error;
			string errormsg = "";

			try {

				// if filetransfer not allowed throw an error
				if ( (int)request.Req < 2 && !FileTransferEnabled) {
					res = ResponseType.Error;
					errormsg = "Filetransfer is not allowed.";	
					SendPacket(client.Id, Response.CreateResponse(request.RequestGuid, res, errormsg, null).BuildResponseToPacket());
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
					var data = request.Data.ToString();
					client.ResetDataReceiver();
					ByteDecoder(client, PacketHelper.StringToByteArray(data));
					res = ResponseType.UdpResponse;
				}
				else if (request.Req == RequestType.CustomReq) {
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
					responseObject = RequestHandler(client, request.Header, content, request.DataType);
				}

				SendPacket(client.Id, Response.CreateResponse(request.RequestGuid, res, errormsg, null, responseObject).BuildResponseToPacket());
			} catch (Exception ex) {
				res = ResponseType.Error;
				errormsg = ex.ToString();
				SendPacket(client.Id, Response.CreateResponse(request.RequestGuid, res, errormsg, ex).BuildResponseToPacket());
			}
		}

		protected virtual void OnMessageReceivedHandler(ISessionMetadata client, Packet message)
		{

			Statistics?.AddReceivedMessages(1);

			var extraInfo = message.AdditionalInternalInfo;
			var eventHandler = message.GetDynamicCallbackServer(extraInfo, DynamicCallbacks);

			if (message.MessageType == PacketType.Auth)
			{
				var data = Encoding.UTF8.GetString(message.Data);
				var split = data.Split('|');

				client.ClientName = split[0].Trim();
				client.Guid = Guid.Parse(split[1]);
				client.UserDomainName = split[2].Trim();
				client.OsVersion = split[3].Trim();
			}

			if (SocketProtocol == SocketProtocolType.Udp)
			{
				Guid clientGuid;
				if (message.MessageType != PacketType.Auth) {
					clientGuid = message.GetGuidFromMessage(extraInfo);
					client.Guid = clientGuid;
				} else 
					clientGuid = client.Guid;
				SocketLogger?.Log($"Message contains guid:{clientGuid} trying to link to an existing client.", LogLevel.Trace);
				
				lock(ConnectedClients) {
					var metadata = ConnectedClients.FirstOrDefault(x => x.Value.Guid == clientGuid).Value;
					if (metadata != null) {
						var temp = client;
						client = metadata;
						client.ChangeDataReceiver(temp.DataReceiver);
					} else {
						var id = !ConnectedClients.Any() ? 1 : ConnectedClients.Keys.Max() + 1;
						var cloned = client.Clone(id); // clone to assign a new id to the connected client.
						cloned.WritingData.Set();
						ConnectedClients.Add(id, cloned);
						client = cloned;
					}
				}

			}

			SocketLogger?.Log($"Received a completed message from a client of type {Enum.GetName(typeof(PacketType), message.MessageType)}. {client.Info()}", LogLevel.Trace);

			if (message.MessageType == PacketType.Message)
			{
				var ev = new ClientMessageReceivedEventArgs(message.BuildDataToString(), client, message.MessageMetadata);

				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnClientMessageReceived(ev);
			} else if (message.MessageType == PacketType.Object)
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
			} else if (message.MessageType == PacketType.Bytes)
			{
				var ev = new ClientBytesReceivedEventArgs(client, message.Data, message.MessageMetadata);
				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnClientBytesReceived(ev);
			} else if (message.MessageType == PacketType.File) {
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
						writer.Write(message.Data, 0, message.Data.Length);
						writer.Close();
					}

					OnClientFileTransferReceivingUpdate(new ClientFileTransferUpdateEventArgs(client, (long) part, (long) total, fileInfo, file));
				}
			} else if (message.MessageType == PacketType.Request) {
				var req = SerializationHelper.DeserializeJson<Request>(message.Data);
				OnRequestReceived(client, req);
			} else if (message.MessageType == PacketType.Response) {
				var resp = SerializationHelper.DeserializeJson<Response>(message.Data);
				lock (_responsePackets) {
					_responsePackets.Add(resp.ResponseGuid, resp);
				}
			}

		}

		#endregion

		#region Sending Data

		protected abstract bool SendToSocket(int clientId, byte[] payload);

		protected abstract Task<bool> SendToSocketAsync(int clientId, byte[] payload);

		protected bool SendInternal(int clientId, PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionMethod eType, CompressionMethod cType, Type objType = null)
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

			return SendPacket(clientId, packet.Build());
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
		protected Packet AddDataOntoPacket(Packet packet)
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
		public virtual bool SendPacket(int clientId, Packet packet)
		{
			try
			{
				var p = AddDataOntoPacket(packet);
				return SendToSocket(clientId, p.BuildPayload());
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
		public virtual async Task<bool> SendPacketAsync(int clientId, Packet packet)
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
			return SendInternal(clientId, PacketType.Message, Encoding.UTF8.GetBytes(message), metadata, string.Empty, EncryptionMethod, CompressionMethod);
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
			return SendInternal(clientId, PacketType.Bytes, bytes, metadata, string.Empty, EncryptionMethod, CompressionMethod);
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

		#region File

		private bool SendFileRequests(int clientId, string file , string remoteloc, bool overwrite) {
			file = Path.GetFullPath(file);

			if (!File.Exists(file))
				throw new ArgumentException("No file found at path.", nameof(file));

			var response = RequestFileTransfer(clientId, 10000, remoteloc);

			if (response.Resp == ResponseType.Error) {
				throw new InvalidOperationException(response.ExceptionMessage,response.Exception);
			}

			if (response.Resp == ResponseType.FileExists) {
				if (overwrite)
				{
					response = RequestFileDelete(clientId, 10000, remoteloc);
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

		public bool SendFile(int clientId, string file, string remoteloc, bool overwrite) => SendFile(clientId, file, remoteloc, overwrite, null);

		public bool SendFile(int clientId, string file, string remoteloc, bool overwrite, IDictionary<object,object> metadata) {

			try {
				SendFileRequests(clientId, file, remoteloc, overwrite);

				var start = DateTime.Now;
				var bufferLength = 2048;
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

						var send = SendPacket(clientId, packet);

						if (!send)
						{
							SocketLogger?.Log($"Part {currentPart} of {totalParts} failed to be sent.", LogLevel.Error);
							return false;
						}
						OnClientFileTransferUpdate(new ClientFileTransferUpdateEventArgs(GetClientInfoById(clientId), currentPart, totalParts, new FileInfo(file), remoteloc));

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

		public async Task<bool> SendFileAsync(int clientId, string file, string remoteloc, bool overwrite) => await SendFileAsync(clientId, file, remoteloc, overwrite, null);

		public async Task<bool> SendFileAsync(int clientId, string file, string remoteloc, bool overwrite, IDictionary<object,object> metadata) {

			try
			{
				SendFileRequests(clientId, file, remoteloc, overwrite);

				var start = DateTime.Now;
				var bufLength = 2048;
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
						var send = await SendPacketAsync(clientId, packet);

						if (!send)
						{
							SocketLogger?.Log($"Part {currentPart} or {totalParts} failed to be sent.", LogLevel.Error);
							return false;
						}
						OnClientFileTransferUpdate(new ClientFileTransferUpdateEventArgs(GetClientInfoById(clientId), currentPart, totalParts, new FileInfo(file), remoteloc));

						buffer = new byte[bufLength];
					}
				}

				//Console.WriteLine("Took: " + (start.ToUniversalTime() - DateTime.Now.ToUniversalTime()).ToString());
				SocketLogger?.Log($"Filetransfer [{file}] -> [{remoteloc}] took  {(DateTime.Now.ToUniversalTime() - start.ToUniversalTime())}.", LogLevel.Trace);
				return true;
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error sending a file.", ex, LogLevel.Error);
				return false;
			}
		}

		private Response RequestFileTransfer(int clientId, int responseTimeInMs, string filename) {
			var req = Request.FileTransferRequest(filename, responseTimeInMs);
			SendPacket(clientId, req.BuildRequestToPacket());
			return GetResponse(req.RequestGuid, req.Expiration);
		}

		private Response RequestFileDelete(int clientId, int responseTimeInMs, string filename) {
				var req = Request.FileDeletionRequest(filename, responseTimeInMs);
				SendPacket(clientId, req.BuildRequestToPacket());
				return GetResponse(req.RequestGuid, req.Expiration);
			}

		#endregion

		#region Requests

		public object SendRequest(int clientId, int responseTimeInMs, string header, object data) {
			return SendRequest<object>(clientId,responseTimeInMs,header,  data);
		}

		public T SendRequest<T>(int clientId, int responseTimeInMs, string header, object data) {
			var req = Request.CustomRequest(responseTimeInMs,header, data);
			SendPacket(clientId, req.BuildRequestToPacket());
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

		/// <summary>
		/// Disposes of the server.
		/// </summary>
		public override void Dispose()
		{
			try
			{
				if (!Disposed)
				{
					Disposed = true;
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

			if (!Disposed && client == null)
				SocketLogger?.Log("Cannot shutdown client " + id + ", does not exist.", LogLevel.Warning);

			try
			{
				client?.Dispose();
				SocketLogger?.Log($"client has been closed. {client.Info()}", LogLevel.Trace);
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