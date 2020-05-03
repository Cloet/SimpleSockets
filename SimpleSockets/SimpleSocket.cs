using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Messaging;

namespace SimpleSockets {

    public abstract class SimpleSocket: IDisposable {
        
        private string _tempPath;

        private LogLevel _logLevel = LogLevel.Error;

        private byte[] _preSharedKey = null;

        internal LogHelper SocketLogger { get; set; }

        protected CancellationTokenSource TokenSource { get; set;}

        protected CancellationToken Token { get; set; }

		protected bool Disposed { get; set; }

		/// <summary>
		/// Indicates if a socket is allowed to receive files from another socket.
		/// </summary>
		/// <value></value>
		public bool FileTransferEnabled { get; set; }

        public abstract Action<string> Logger { get; set; }

        public byte[] EncryptionPassphrase { get; set; }

        public EncryptionType EncryptionMethod { get; set; } = EncryptionType.None;

        public CompressionType CompressionMethod { get; set; } = CompressionType.GZip;

		public int BufferSize { get => ClientMetadata.BufferSize; }

        public LogLevel LoggerLevel {
            get => _logLevel;
            set {
                _logLevel = value;
                SocketLogger?.ChangeLogLevel(value);
            }
        }

        public byte[] PreSharedKey { 
            get => _preSharedKey;
            set  {
                if (value.Length != 16)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _preSharedKey = value;
            }
        }

        public bool SslEncryption { get; private set; }

        public SocketProtocolType SocketProtocol { get; private set; }

		public SocketStatistics Statistics { get; private set; }

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

		public SimpleSocket(bool useSsl, SocketProtocolType protocolType) {
			SslEncryption = useSsl;
			SocketProtocol = protocolType;
			Statistics = new SocketStatistics(SslEncryption, SocketProtocol);
		}
        

        /// <summary>
        /// Disposes of the Socket.
        /// </summary>
        public abstract void Dispose();

    }

}