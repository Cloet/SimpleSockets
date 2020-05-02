using System;
using System.Collections.Generic;
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
		protected virtual void OnConnectedToServer() => ConnectedToServer?.Invoke(this, null);

		/// <summary>
		/// Event fired when a client is disconnected from the server
		/// </summary>
		public event EventHandler DisconnectedFromServer;
		protected virtual void OnDisconnectedFromServer() => DisconnectedFromServer?.Invoke(this, null);

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

		#endregion


		private Action<string> _logger;

		protected readonly ManualResetEventSlim Connected = new ManualResetEventSlim(false);

		protected readonly ManualResetEventSlim Sent = new ManualResetEventSlim(false);

		public override Action<string> Logger { 
            get => _logger;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SocketLogger = LogHelper.InitializeLogger(true, SslEncryption , SocketProtocolType.Tcp == this.SocketProtocol, value, this.LoggerLevel);
                _logger = value;
            }
        }

		public string ServerIp { get; protected set; }

		public int ServerPort { get; protected set; }

		public TimeSpan AutoReconnect { get; protected set; }

		public IPEndPoint EndPoint { get; protected set; }

		protected Socket Listener { get; set; }

		public SimpleClient(bool useSsl, SocketProtocolType protocol) : base(useSsl, protocol) {
			
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

		public virtual bool IsConnected()
		{
			try
			{
				if (Listener == null)
					return false;

				return !((Listener.Poll(1000, SelectMode.SelectRead) && (Listener.Available == 0)) || !Listener.Connected);
			}
			catch (Exception)
			{
				return false;
			}
		}

		internal virtual void OnMessageReceivedHandler(SimpleMessage message) {
			if (message.MessageType == MessageType.Message)
				OnMessageReceived(new MessageReceivedEventArgs(message.BuildDataToString(), message.BuildMetadataFromBytes()));

			if (message.MessageType == MessageType.Object)
			{
				var obj = message.BuildObjectFromBytes(out var type);

				if (!(obj == null || type == null))
					OnObjectReceived(new ObjectReceivedEventArgs(obj, type, message.BuildMetadataFromBytes()));
				else
					SocketLogger?.Log("Error receiving an object.", LogLevel.Error);
			}

		}

		#region Sending Data

		protected abstract void SendToServer(byte[] payload);

		protected abstract Task<bool> SendToServerAsync(byte[] payload);

		protected bool SendInternal(MessageType msgType, byte[] data, IDictionary<object, object> metadata, IDictionary<object, object> extraInfo, EncryptionType encryption, CompressionType compression)
		{

			try
			{
				if (EncryptionMethod != EncryptionType.None && (EncryptionPassphrase == null || EncryptionPassphrase.Length == 0))
					SocketLogger?.Log($"Please set a valid encryptionmethod when trying to encrypt a message.{Environment.NewLine}This message will not be encrypted.", LogLevel.Warning);

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

		protected async Task<bool> SendInternalAsync(MessageType msgType, byte[] data, IDictionary<object, object> metadata, IDictionary<object, object> extraInfo, EncryptionType encryption, CompressionType compression)
		{
			try
			{
				if (EncryptionMethod != EncryptionType.None && (EncryptionPassphrase == null || EncryptionPassphrase.Length > 0))
					SocketLogger?.Log($"Please set a valid encryptionmethod when trying to encrypt a message.{Environment.NewLine}This message will not be encrypted.", LogLevel.Warning);

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

		#region Messages
		public bool SendMessage(string message, IDictionary<object,object> metadata, EncryptionType encryption, CompressionType compression)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			return SendInternal(MessageType.Message, bytes, metadata,null, encryption, compression);
		}

		public bool SendMessage(string message, IDictionary<object,object> metadata)
		{
			return SendMessage(message,metadata, EncryptionMethod, CompressionMethod);
		}

		public bool SendMessage(string msg) {
			return SendMessage(msg, null);
		}

		public async Task<bool> SendMessageAsync(string message, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			var bytes = Encoding.UTF8.GetBytes(message);
			return await SendInternalAsync(MessageType.Message,bytes, metadata, null, encryption, compression);
		}

		public async Task<bool> SendMessageAsync(string message, IDictionary<object, object> metadata) {
			return await SendMessageAsync(message, metadata, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendMessageAsync(string message) {
			return await SendMessageAsync(message, null);
		}

		#endregion

		#region Bytes
		public bool SendBytes(byte[] bytes, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return SendInternal(MessageType.Bytes, bytes, metadata, null, encryption, compression);
		}

		public bool SendBytes(byte[] bytes, IDictionary<object, object> metadata) {
			return SendBytes(bytes, metadata, EncryptionMethod, CompressionMethod);
		}

		public bool SendBytes(byte[] bytes) {
			return SendBytes(bytes, null);
		}

		public async Task<bool> SendBytesAsync(byte[] bytes, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return await SendInternalAsync(MessageType.Bytes, bytes, metadata, null, encryption, compression);
		}

		public async Task<bool> SendBytesAsync(byte[] bytes, IDictionary<object, object> metadata) {
			return await SendBytesAsync(bytes, metadata, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendBytesAsync(byte[] bytes) {
			return await SendBytesAsync(bytes, null);
		}

		#endregion

		#region Object

		public bool SendObject(object obj, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);

			var info = new Dictionary<object, object>();
			info.Add("Type", obj.GetType());

			return SendInternal(MessageType.Object, bytes, metadata, info, encryption, compression);
		}

		public bool SendObject(object obj, IDictionary<object, object> metadata) {
			return SendObject(obj, metadata, EncryptionMethod, CompressionMethod);
		}

		public bool SendObject(object obj) {
			return SendObject(obj, null);
		}

		public async Task<bool> SendObjectAsync(object obj, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);

			var info = new Dictionary<object, object>();
			info.Add("Type", obj.GetType());

			return await SendInternalAsync(MessageType.Object, bytes, metadata, info, encryption, compression);
		}

		public async Task<bool> SendObjectAsync(object obj, IDictionary<object, object> metadata) {
			return await SendObjectAsync(obj, metadata, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendObjectAsync(object obj) {
			return await SendObjectAsync(obj, null);
		}

		#endregion

		#endregion

	}

}