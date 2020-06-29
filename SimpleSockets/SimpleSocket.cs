using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Client;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;

namespace SimpleSockets {

    public abstract class SimpleSocket: IDisposable {
        
        private string _tempPath;

        private LogLevel _logLevel = LogLevel.Error;

        private byte[] _preSharedKey = null;

        internal LogHelper SocketLogger { get; set; }

        protected CancellationTokenSource TokenSource { get; set;}

        protected CancellationToken Token { get; set; }

		protected bool Disposed { get; set; }

		protected IDictionary<Guid,Response> _responsePackets = new Dictionary<Guid,Response>();

		/// <summary>
		/// Indicates if a socket is allowed to receive files from another socket.
		/// </summary>
		/// <value></value>
		public bool FileTransferEnabled { get; set; }

		/// <summary>
		/// Handles all log messages.
		/// </summary>
        public abstract Action<string> Logger { get; set; }

		/// <summary>
		/// The passphrase used for encryption.
		/// Length should be 32 Bytes.
		/// </summary>
        public byte[] EncryptionPassphrase { get; set; }

		/// <summary>
		/// The default encryption used when sending messages.
		/// Alternate methods can be set for each message.
		/// </summary>
        public EncryptionMethod EncryptionMethod { get; set; } = EncryptionMethod.None;

		/// <summary>
		/// The default compression when sending messages.
		/// Alternate compressions can be set for each message.
		/// </summary>
        public CompressionMethod CompressionMethod { get; set; } = CompressionMethod.GZip;

		/// <summary>
		/// Readbuffer used when receiving messages.
		/// </summary>
		public int BufferSize { get => SessionMetadata.BufferSize; }

		/// <summary>
		/// Only messages less then or equal to the given level will be logged.
		/// </summary>
        public LogLevel LoggerLevel {
            get => _logLevel;
            set {
                _logLevel = value;
                SocketLogger?.ChangeLogLevel(value);
            }
        }

		/// <summary>
		/// The PreSharedKey.
		/// If this value is defined the socket will expect all received messages to contain the same PreSharedKey.
		/// If a key is expected but not found the message will be skipped.
		/// </summary>
        public byte[] PreSharedKey { 
            get => _preSharedKey;
            set  {
                if (value.Length != 16)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _preSharedKey = value;
            }
        }

		/// <summary>
		/// The protocol the socket uses.
		/// UDP or TCP
		/// </summary>
        public SocketProtocolType SocketProtocol { get; private set; }

		/// <summary>
		/// Some statistics of the socket.
		/// Use (ToString()) for short description.
		/// </summary>
		public SocketStatistics Statistics { get; private set; }

		/// <summary>
		/// The temporary path used when encryption/decryption/compression/decompression of files/folder.
		/// </summary>
        public string TempPath {
            get => string.IsNullOrEmpty(_tempPath) ? Path.GetTempPath() : _tempPath;
            set {
                var temp = new FileInfo(value);
                if (temp.Directory != null)
                {
                    _tempPath = temp.Directory.FullName + Path.DirectorySeparatorChar;
                    if (!Directory.Exists(_tempPath))
                        Directory.CreateDirectory(_tempPath);

                    return;
                }

                throw new ArgumentException("'" + value + "' is an invalid path.");
            }
        }

		// Base constructor
		public SimpleSocket(SocketProtocolType protocolType)
		{
			SocketProtocol = protocolType;
			Statistics = new SocketStatistics(SocketProtocol);
		}

		// Decodes a byte array.
		protected abstract void ByteDecoder(ISessionMetadata session, byte[] array);

		/// <summary>
		/// Disposes of the Socket.
		/// </summary>
		public abstract void Dispose();

		#region Static constructors

		/// <summary>
		/// Creates a new tcp server.
		/// </summary>
		/// <returns></returns>
		public static SimpleServer CreateTcpServer() => new SimpleTcpServer();

		/// <summary>
		/// Creates a new Tcp server with ssl encryption.
		/// </summary>
		/// <param name="cert"></param>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public static SimpleServer CreateTcpSslServer(SslContext context) => new SimpleTcpServer(context);

		/// <summary>
		/// Creates a new udp server.
		/// </summary>
		/// <returns></returns>
		public static SimpleServer CreateUdpServer() => new SimpleUdpServer();

		/// <summary>
		/// Creates a new Tcp client
		/// </summary>
		/// <returns></returns>
		public static SimpleClient CreateTcpClient() => new SimpleTcpClient();

		/// <summary>
		/// Creates a new Tcp client with ssl encryption.
		/// </summary>
		/// <param name="cert"></param>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public static SimpleClient CreateTcpSslClient(SslContext context) => new SimpleTcpClient(context);

		/// <summary>
		/// Creates a new Tcp
		/// </summary>
		/// <returns></returns>
		public static SimpleClient CreateUdpClient() => new SimpleUdpClient();

		#endregion

		protected Response GetResponse(Guid guid, DateTime expiration) {
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
				if (DateTime.Now.ToUniversalTime() >= expiration.ToUniversalTime())
					break;
				Task.Delay(50).Wait();
			}

			throw new TimeoutException("No response received within the expected time window.");
		}

	}

}