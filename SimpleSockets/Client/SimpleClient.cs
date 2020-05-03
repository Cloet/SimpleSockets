using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
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
		/// Event fired when the client successfully validates the ssl certificate.
		/// </summary>
		public event EventHandler SslAuthSuccess;
		protected virtual void OnSslAuthSuccess() => SslAuthSuccess?.Invoke(this, null);

		/// <summary>
		/// Event fired when client is unable to validate the ssl certificate
		/// </summary>
		public event EventHandler SslAuthFailed;
		protected virtual void OnSslAutFailed() => SslAuthFailed?.Invoke(this, null);

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

		protected SslStream _sslStream;

		protected X509Certificate2 _sslCertificate;

		protected X509Certificate2Collection _sslCertificateCollection;

		protected TlsProtocol _tlsProtocol;

		public bool MutualAuthentication { get; set; }

		public bool AcceptInvalidCertificates { get; set; }

		/// <summary>
		/// Used to log exceptions/messages.
		/// </summary>
		public override Action<string> Logger { 
            get => _logger;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SocketLogger = LogHelper.InitializeLogger(true, SslEncryption , SocketProtocolType.Tcp == this.SocketProtocol, value, this.LoggerLevel);
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
		public SimpleClient(bool useSsl, SocketProtocolType protocol) : base(useSsl, protocol) {
			_connected = false;
			DynamicCallbacks = new Dictionary<string, EventHandler<DataReceivedEventArgs>>(); ;
		}

		protected string ClientGuid { get; set; }

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

		internal virtual void OnMessageReceivedHandler(SimpleMessage message) {

			Statistics?.AddReceivedMessages(1);

			var extraInfo = message.BuildInternalInfoFromBytes();
			var eventHandler = message.GetDynamicCallbackClient(extraInfo, DynamicCallbacks);

			if (message.MessageType == MessageType.Message) {
				var ev = new MessageReceivedEventArgs(message.BuildDataToString(), message.BuildMetadataFromBytes());

				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnMessageReceived(ev);
			}


			if (message.MessageType == MessageType.Object)
			{
				var obj = message.BuildObjectFromBytes(extraInfo, out var type);
				var ev = new ObjectReceivedEventArgs(obj, type, message.BuildMetadataFromBytes());

				if (!(obj == null || type == null)) {
					if (eventHandler != null)
						eventHandler?.Invoke(this, ev);
					else
						OnObjectReceived(ev);
				} else
					SocketLogger?.Log("Error receiving an object.", LogLevel.Error);
			}

			if (message.MessageType == MessageType.Bytes) {
				var ev = new BytesReceivedEventArgs(message.Data, message.BuildMetadataFromBytes());
				if (eventHandler != null)
					eventHandler?.Invoke(this, ev);
				else
					OnBytesReceived(ev);
			}

		}

		#region Sending Data

		protected abstract void SendToServer(byte[] payload);

		protected abstract Task<bool> SendToServerAsync(byte[] payload);

		protected bool SendInternal(MessageType msgType, byte[] data, IDictionary<object, object> metadata, IDictionary<object, object> extraInfo, string eventKey, EncryptionType encryption, CompressionType compression)
		{

			try
			{
				if (EncryptionMethod != EncryptionType.None && (EncryptionPassphrase == null || EncryptionPassphrase.Length == 0))
					SocketLogger?.Log($"Please set a valid encryptionmethod when trying to encrypt a message.{Environment.NewLine}This message will not be encrypted.", LogLevel.Warning);

				if (eventKey != string.Empty && eventKey != null) {
					if (extraInfo == null)
						extraInfo = new Dictionary<object, object>();
					extraInfo.Add("DynamicCallback", eventKey);
				}

				var payload = MessageBuilder.Initialize(msgType, SocketLogger)
					.AddCompression(compression)
					.AddEncryption(EncryptionPassphrase,encryption)
					.AddMessageBytes(data)
					.AddMetadata(metadata)
					.AddAdditionalInternalInfo(extraInfo)
					.BuildMessage();

				SendToServer(payload);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		protected async Task<bool> SendInternalAsync(MessageType msgType, byte[] data, IDictionary<object, object> metadata, IDictionary<object, object> extraInfo, string eventKey, EncryptionType encryption, CompressionType compression)
		{
			try
			{
				if (EncryptionMethod != EncryptionType.None && (EncryptionPassphrase == null || EncryptionPassphrase.Length > 0))
					SocketLogger?.Log($"Please set a valid encryptionmethod when trying to encrypt a message.{Environment.NewLine}This message will not be encrypted.", LogLevel.Warning);

				if (eventKey != string.Empty && eventKey != null)
				{
					if (extraInfo == null)
						extraInfo = new Dictionary<object, object>();
					extraInfo.Add("DynamicCallback", eventKey);
				}

				var payload = MessageBuilder.Initialize(msgType, SocketLogger)
					.AddCompression(CompressionMethod)
					.AddEncryption(EncryptionPassphrase, EncryptionMethod)
					.AddMessageBytes(data)
					.AddMetadata(metadata)
					.AddAdditionalInternalInfo(extraInfo)
					.BuildMessage();

				return await SendToServerAsync(payload);
			}
			catch (Exception)
			{
				return false;
			}
		}

		protected bool SendAuthenticationMessage() {
			var username = Environment.UserName;
			var osVersion = Environment.OSVersion;
			var user = Environment.UserDomainName;

			//Keep existing GUID
			var guid = string.IsNullOrEmpty(ClientGuid) ? Guid.NewGuid().ToString() : ClientGuid;

			var msg = username + "|" + guid + "|" + user + "|" + osVersion;

			return SendInternal(MessageType.Auth, Encoding.UTF8.GetBytes(msg),null,null,string.Empty,EncryptionMethod,CompressionMethod);
		}

		#region Messages

		/// <summary>
		/// Sends a message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public bool SendMessage(string message, IDictionary<object, object> metadata, string dynamicEventKey, EncryptionType encryption, CompressionType compression)
		{
			return SendInternal(MessageType.Message, Encoding.UTF8.GetBytes(message), metadata, null, string.Empty, encryption, compression);
		}

		/// <summary>
		/// Sends a message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public bool SendMessage(string message, IDictionary<object,object> metadata, EncryptionType encryption, CompressionType compression)
		{
			return SendMessage(message, metadata, string.Empty, encryption, compression);
		}

		/// <summary>
		/// Sends a message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <returns></returns>
		public bool SendMessage(string message, IDictionary<object, object> metadata, string dynamicEventKey) {
			return SendMessage(message, metadata, dynamicEventKey, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends a message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public bool SendMessage(string message, IDictionary<object,object> metadata)
		{
			return SendMessage(message,metadata, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends a message to the server.
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		public bool SendMessage(string message) {
			return SendMessage(message, null);
		}

		/// <summary>
		/// Sends an async message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public async Task<bool> SendMessageAsync(string message, IDictionary<object, object> metadata, string dynamicEventKey, EncryptionType encryption, CompressionType compression)
		{
			return await SendInternalAsync(MessageType.Message, Encoding.UTF8.GetBytes(message), metadata, null, dynamicEventKey, encryption, compression);
		}

		/// <summary>
		/// Sends an async message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public async Task<bool> SendMessageAsync(string message, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return await SendMessageAsync(message, metadata, string.Empty, encryption, compression);
		}

		/// <summary>
		/// Sends an async message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <returns></returns>
		public async Task<bool> SendMessageAsync(string message, IDictionary<object, object> metadata, string dynamicEventKey) {
			return await SendMessageAsync(message, metadata, dynamicEventKey, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends an async message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public async Task<bool> SendMessageAsync(string message, IDictionary<object, object> metadata) {
			return await SendMessageAsync(message, metadata, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends an async message to the server.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task<bool> SendMessageAsync(string message) {
			return await SendMessageAsync(message, null);
		}

		#endregion

		#region Bytes

		/// <summary>
		/// Sends bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public bool SendBytes(byte[] bytes, IDictionary<object, object> metadata, string dynamicEventKey, EncryptionType encryption, CompressionType compression)
		{
			return SendInternal(MessageType.Bytes, bytes, metadata, null, dynamicEventKey, encryption, compression);
		}

		/// <summary>
		/// Sends bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public bool SendBytes(byte[] bytes, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return SendBytes(bytes, metadata, string.Empty, encryption, compression);
		}

		/// <summary>
		/// Sends bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <returns></returns>
		public bool SendBytes(byte[] bytes, IDictionary<object, object> metadata, string dynamicEventKey)
		{
			return SendBytes(bytes, metadata, dynamicEventKey, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public bool SendBytes(byte[] bytes, IDictionary<object, object> metadata) {
			return SendBytes(bytes, metadata, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public bool SendBytes(byte[] bytes) {
			return SendBytes(bytes, null);
		}

		/// <summary>
		/// Sends async bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public async Task<bool> SendBytesAsync(byte[] bytes, IDictionary<object, object> metadata, string dynamicEventKey, EncryptionType encryption, CompressionType compression)
		{
			return await SendInternalAsync(MessageType.Bytes, bytes, metadata, null, dynamicEventKey, encryption, compression);
		}

		/// <summary>
		/// Sends async bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public async Task<bool> SendBytesAsync(byte[] bytes, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return await SendBytesAsync(bytes, metadata, string.Empty, encryption, compression);
		}

		/// <summary>
		/// Sends async bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <returns></returns>
		public async Task<bool> SendBytesAsync(byte[] bytes, IDictionary<object, object> metadata, string dynamicEventKey) {
			return await SendBytesAsync(bytes, metadata, dynamicEventKey, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends async bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public async Task<bool> SendBytesAsync(byte[] bytes, IDictionary<object, object> metadata) {
			return await SendBytesAsync(bytes, metadata, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends async bytes to the server.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public async Task<bool> SendBytesAsync(byte[] bytes) {
			return await SendBytesAsync(bytes, null);
		}

		#endregion

		#region Object

		/// <summary>
		/// Sends an object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public bool SendObject(object obj, IDictionary<object, object> metadata, string dynamicEventKey, EncryptionType encryption, CompressionType compression) {
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);

			var info = new Dictionary<object, object>();
			info.Add("Type", obj.GetType());

			return SendInternal(MessageType.Object, bytes, metadata, info, dynamicEventKey, encryption, compression);
		}

		/// <summary>
		/// Sends an object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public bool SendObject(object obj, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return SendObject(obj, metadata, string.Empty, encryption, compression);
		}

		/// <summary>
		/// Sends an object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <returns></returns>
		public bool SendObject(object obj, IDictionary<object, object> metadata, string dynamicEventKey)
		{
			return SendObject(obj, metadata, dynamicEventKey, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends an object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public bool SendObject(object obj, IDictionary<object, object> metadata) {
			return SendObject(obj, metadata, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends an object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool SendObject(object obj) {
			return SendObject(obj, null);
		}

		/// <summary>
		/// Sends an async object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public async Task<bool> SendObjectAsync(object obj, IDictionary<object, object> metadata, string dynamicEventKey, EncryptionType encryption, CompressionType compression)
		{
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);

			var info = new Dictionary<object, object>();
			info.Add("Type", obj.GetType());

			return await SendInternalAsync(MessageType.Object, bytes, metadata, info, dynamicEventKey, encryption, compression);
		}

		/// <summary>
		/// Sends an async object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <param name="encryption"></param>
		/// <param name="compression"></param>
		/// <returns></returns>
		public async Task<bool> SendObjectAsync(object obj, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return await SendObjectAsync(obj, metadata, string.Empty, encryption, compression);
		}

		/// <summary>
		/// Sends an async object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <param name="dynamicEventKey"></param>
		/// <returns></returns>
		public async Task<bool> SendObjectAsync(object obj, IDictionary<object, object> metadata, string dynamicEventKey)
		{
			return await SendObjectAsync(obj, metadata, dynamicEventKey, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends an async object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public async Task<bool> SendObjectAsync(object obj, IDictionary<object, object> metadata) {
			return await SendObjectAsync(obj, metadata, EncryptionMethod, CompressionMethod);
		}

		/// <summary>
		/// Sends an async object to the server.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public async Task<bool> SendObjectAsync(object obj) {
			return await SendObjectAsync(obj, null);
		}

		#endregion

		#endregion

	}

}