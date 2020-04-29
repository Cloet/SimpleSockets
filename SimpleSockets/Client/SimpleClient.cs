using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Messaging;

namespace SimpleSockets.Client {

    public abstract class SimpleClient : SimpleSocket
    {
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

		protected abstract void SendToServer(byte[] payload);

		#region Sending Data

		#region Messages
		public void SendMessage(string msg, IDictionary<object,object> metadata, bool encrypt, bool compress)
		{
			if (encrypt == true && (EncryptionMethod == EncryptionType.None || EncryptionPassphrase == null))
				SocketLogger?.Log($"Please set a valid encryptionmethod when trying to encrypt a message.{Environment.NewLine}This message will not be encrypted.", LogLevel.Warning);
			if (compress == true && CompressionMethod == CompressionType.None)
				SocketLogger?.Log($"Please choose a valid compressionmethod when trying to compress a message.{Environment.NewLine}This message will nog be compressed.", LogLevel.Warning);

			var payload = MessageBuilder.Initialize(MessageType.Message, SocketLogger)
				.AddCompression(compress == false ? CompressionType.None : CompressionMethod)
				.AddEncryption(EncryptionPassphrase, encrypt == false ? EncryptionType.None : EncryptionMethod)
				.AddMessageString(msg)
				.AddPreSharedKey(PreSharedKey)
				.AddMetadata(metadata)
				.BuildMessage();

			SendToServer(payload);
		}

		public void SendMessage(string msg, IDictionary<object,object> metadata)
		{
			SendMessage(msg,metadata, (CompressionType.None == CompressionMethod), (EncryptionMethod == EncryptionType.None));
		}

		public void SendMessage(string msg) {
			SendMessage(msg, null);
		}
		#endregion

		#region Bytes
		public void SendBytes(byte[] bytes, IDictionary<object, object> metadata, bool encrypt, bool compress) {
			var payload = MessageBuilder.Initialize(MessageType.Bytes, SocketLogger)
				.AddCompression(compress == false ? CompressionType.None : CompressionMethod)
				.AddEncryption(EncryptionPassphrase, encrypt == false ? EncryptionType.None : EncryptionMethod)
				.AddMessageBytes(bytes)
				.AddPreSharedKey(PreSharedKey)
				.AddMetadata(metadata)
				.BuildMessage();

			SendToServer(payload);
		}
		#endregion

		#endregion

	}

}