using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
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
		internal virtual void OnMessageReceivedHandler(Packet packet)
		{

			Statistics?.AddReceivedMessages(1);

			var extraInfo = packet.AdditionalInternalInfo;
			var eventHandler = packet.GetDynamicCallbackClient(extraInfo, DynamicCallbacks);

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

				if (!(obj == null || type == null))
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

		protected bool SendInternal(PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionType encryption, CompressionType compression)
		{
			var packet = PacketBuilder.NewPacket
				.SetBytes(data)
				.SetPacketType(msgType)
				.SetMetadata(metadata)
				.SetCompression(compression)
				.SetEncryption(encryption)
				.SetDynamicCallback(eventKey)
				.Build();

			return SendPacket(packet);
		}

		protected async Task<bool> SendInternalAsync(PacketType msgType, byte[] data, IDictionary<object, object> metadata, string eventKey, EncryptionType encryption, CompressionType compression)
		{
			var packet = PacketBuilder.NewPacket
				.SetBytes(data)
				.SetPacketType(msgType)
				.SetMetadata(metadata)
				.SetCompression(compression)
				.SetEncryption(encryption)
				.SetDynamicCallback(eventKey)
				.Build();

			return await SendPacketAsync(packet);
		}

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
				packet.Encrypt = (EncryptionMethod != EncryptionType.None);
				packet.EncryptMode = EncryptionMethod;
			}

			if (packet.addDefaultCompression)
			{
				packet.Compress = (CompressionMethod != CompressionType.None);
				packet.CompressMode = CompressionMethod;
			}

			return packet;
		}

		/// <summary>
		/// Send a packet build with <seealso cref="PacketBuilder"/>.
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
		/// Sends a packet build with <seealso cref="PacketBuilder"/>
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

		public bool SendMessage(string message) {
			return SendInternal(PacketType.Message, Encoding.UTF8.GetBytes(message), null, null, EncryptionMethod, CompressionMethod);
		}

		#endregion

	}

}