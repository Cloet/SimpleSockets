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

namespace SimpleSockets.Server {

    public abstract class SimpleServer: SimpleSocket
    {

        #region Events
        
        /// <summary>
        /// Event invoked when a client connects to the server.
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        protected virtual void OnClientConnected(ClientConnectedEventArgs eventArgs) => ClientConnected?.Invoke(this, eventArgs);

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

        #endregion

        #region Variables

        private Action<string> _logger;

        public override Action<string> Logger { 
            get => _logger;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SocketLogger = LogHelper.InitializeLogger(false, SslEncryption , SocketProtocolType.Tcp == this.SocketProtocol, value, this.LoggerLevel);
                _logger = value;
            }
        }

        public int MaximumConnections { get; private set; } = 500;

        protected readonly ManualResetEventSlim CanAcceptConnections = new ManualResetEventSlim(false);

        protected Socket Listener { get; set; }

        public bool Listening { get; set; }

        public int ListenerPort { get; set; }

        public string ListenerIp { get; set; }

        internal IDictionary<int, IClientMetadata> ConnectedClients { get; set; }
		
        #endregion

        protected SimpleServer(bool useSsl, SocketProtocolType protocol): base(useSsl,protocol) {
            Listening = false;
            ConnectedClients = new Dictionary<int, IClientMetadata>();
        }

        public abstract void Listen(string ip, int port, int limit = 500);

        public void Listen(int port , int limit = 500) => Listen("*", port, limit);

        protected IPAddress ListenerIPFromString(string ip) {
            try {

                if (string.IsNullOrEmpty(ip) || ip.Trim() == "*"){
                    var ipAdr = IPAddress.Any;
                    ListenerIp = ipAdr.ToString();
                    return ipAdr;
                }

                return IPAddress.Parse(ip);
            } catch (Exception ex ){
                SocketLogger?.Log("Invalid Server IP",ex,LogLevel.Fatal);
                return null;
            }
        }

        public bool IsClientConnected(int id) {
            try {
                if (GetClientMetadataById(id) is IClientMetadata state && state.Listener is Socket socket) {
                    return !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
                }
            } catch (ObjectDisposedException) {
                return false;
            }
            catch (Exception ex) {
                SocketLogger?.Log("Something went wrong trying to check if a client is connected.", ex, LogLevel.Error);
            }

            return false;
        }

        public void ShutDownClient(int id) {
            ShutDownClient(id, DisconnectReason.Normal);
        }

        internal void ShutDownClient(int id, DisconnectReason reason) {
            var client = GetClientMetadataById(id);

            if (client == null)
                SocketLogger?.Log("Cannot shutdown client " + id + ", does not exist.", LogLevel.Warning);

            try {
                if (client?.Listener != null) {
                    client.Listener.Shutdown(SocketShutdown.Both);
                    client.Listener.Close();
                    client.Listener = null;
                }
            } catch (ObjectDisposedException de) {
                SocketLogger?.Log("Cannot shutdown client " + id + ", has already been disposed.",de, LogLevel.Warning);
            } catch (Exception ex) {
                SocketLogger?.Log("An error occurred when shutting down client " + id, ex, LogLevel.Warning);
            } finally {
                lock(ConnectedClients) {
                    ConnectedClients.Remove(id);
                    OnClientDisconnected(new ClientDisconnectedEventArgs(client, reason));
                }
            }
        }

        internal IClientMetadata GetClientMetadataById(int id) {
            return ConnectedClients.TryGetValue(id, out var state) ? state : null;
        }

        public IClientInfo GetClientInfoById(int id) {
            return GetClientMetadataById(id);
        }

		public IList<IClientInfo> GetAllClients() {
			var clients = ConnectedClients.Values.Cast<IClientInfo>().ToList();
			if (clients == null)
				return new List<IClientInfo>();
			return clients;
		}

		#region Data-Invokers

		internal virtual void OnMessageReceivedHandler(IClientMetadata client, SimpleMessage message) {

			if (message.MessageType == MessageType.Message) {
				OnClientMessageReceived(new ClientMessageReceivedEventArgs(message.BuildDataToString(), client, message.BuildMetadataFromBytes()));
			}

			if (message.MessageType == MessageType.Object) {
				var obj = message.BuildObjectFromBytes(out var type);

				if (obj == null || type == null) {
					OnClientObjectReceived(new ClientObjectReceivedEventArgs(obj, type,client, message.BuildMetadataFromBytes()));
				} else
					SocketLogger?.Log("Error receiving an object.", LogLevel.Error);
			}

		}

		#endregion

		#region Sending Data

		protected abstract void SendToSocket(int clientId, byte[] payload);

		protected abstract Task<bool> SendToSocketAsync(int clientId, byte[] payload);

		protected bool SendInternal(int clientid, MessageType msgType, byte[] data, IDictionary<object,object> metadata, IDictionary<object,object> extraInfo, EncryptionType eType, CompressionType cType) {

			try
			{
				if (EncryptionMethod != EncryptionType.None && (EncryptionPassphrase == null || EncryptionPassphrase.Length == 0))
					SocketLogger?.Log($"Please set a valid encryptionmethod when trying to encrypt a message.{Environment.NewLine}This message will not be encrypted.", LogLevel.Warning);

				var payload = MessageBuilder.Initialize(msgType, SocketLogger)
					.AddCompression(cType)
					.AddEncryption(EncryptionPassphrase, eType)
					.AddMessageBytes(data)
					.AddMetadata(metadata)
					.AddAdditionalInternalInfo(extraInfo)
					.BuildMessage();

				SendToSocket(clientid, payload);

				return true;
			}
			catch (Exception) {
				return false;
			}
		}

		protected async Task<bool> SendInternalAsync(int clientId, MessageType msgType, byte[] data, IDictionary<object, object> metadata, IDictionary<object, object> extraInfo, EncryptionType eType, CompressionType cType) {
			try
			{
				if (EncryptionMethod != EncryptionType.None && (EncryptionPassphrase == null || EncryptionPassphrase.Length == 0))
					SocketLogger?.Log($"Please set a valid encryptionmethod when trying to encrypt a message.{Environment.NewLine}This message will not be encrypted.", LogLevel.Warning);

				var payload = MessageBuilder.Initialize(msgType, SocketLogger)
					.AddCompression(cType)
					.AddEncryption(EncryptionPassphrase, eType)
					.AddMessageBytes(data)
					.AddMetadata(metadata)
					.AddAdditionalInternalInfo(extraInfo)
					.BuildMessage();

				return await SendToSocketAsync(clientId, payload);
			}
			catch (Exception) {
				return false;
			}
		}

		#region Message

		public bool SendMessage(int clientId, string message, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression)
		{
			return SendInternal(clientId, MessageType.Message, Encoding.UTF8.GetBytes(message), metadata, null, encryption, compression);
		}

		public bool SendMessage(int clientId, string message, IDictionary<object, object> metadata)
		{
			return SendMessage(clientId, message, metadata, EncryptionMethod, CompressionMethod);
		}

		public bool SendMessage(int clientId, string message)
		{
			return SendMessage(clientId, message, null);
		}

		public async Task<bool> SendMessageAsync(int clientId, string message, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			var msg = Encoding.UTF8.GetBytes(message);
			return await SendInternalAsync(clientId, MessageType.Message, msg, metadata, null, encryption, compression);
		}

		public async Task<bool> SendMessageAsync(int clientId, string message, IDictionary<object, object> metadata) {
			return await SendMessageAsync(clientId, message, metadata, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendMessageAsync(int clientId, string message) {
			return await SendMessageAsync(clientId, message);
		}

		#endregion

		#region Bytes

		public bool SendBytes(int clientId, byte[] data, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return SendInternal(clientId, MessageType.Bytes, data, metadata, null, encryption, compression);
		}

		public bool SendBytes(int clientId, byte[] data, IDictionary<object, object> metadata) {
			return SendBytes(clientId, data, metadata, EncryptionMethod, CompressionMethod);
		}

		public bool SendBytes(int clientId, byte[] data) {
			return SendBytes(clientId, data);
		}

		public async Task<bool> SendBytesAsync(int clientId, byte[] data, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			return await SendInternalAsync(clientId, MessageType.Bytes, data, metadata, null, encryption, compression);
		}

		public async Task<bool> SendBytesAsync(int clientId, byte[] data, IDictionary<object, object> metadata) {
			return await SendBytesAsync(clientId, data, metadata, EncryptionMethod, CompressionMethod);
		}

		public async Task<bool> SendBytesAsync(int clientId, byte[] data) {
			return await SendBytesAsync(clientId, data, null);
		}

		#endregion

		#region Objects

		public bool SendObject(int clientId, object obj, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);

			var info = new Dictionary<object, object>();
			info.Add("Type", obj.GetType());

			return SendInternal(clientId, MessageType.Object, bytes, metadata, info, encryption, compression);
		}

		public bool SendObject(int clientId, object obj, IDictionary<object, object> metadata) {
			return SendObject(clientId, obj, metadata, EncryptionMethod, CompressionMethod);
		}

		public bool SendObject(int clientId, object obj) {
			return SendObject(clientId, obj, null);
		}

		public async Task<bool> SendObjectAsync(int clientId, object obj, IDictionary<object, object> metadata, EncryptionType encryption, CompressionType compression) {
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);

			var info = new Dictionary<object, object>();
			info.Add("Type", obj.GetType());

			return await SendInternalAsync(clientId, MessageType.Object, bytes, metadata, null, encryption, compression);
		}

		public async Task<bool> SendObjectAsync(int clientId, object obj, IDictionary<object, object> metadata) {
			return await SendObjectAsync(clientId, obj, metadata, EncryptionMethod,CompressionMethod);
		}

		public async Task<bool> SendObjectAsync(int clientId, object obj) {
			return await SendObjectAsync(clientId, obj, null);
		}

		#endregion

		#endregion


	}

}