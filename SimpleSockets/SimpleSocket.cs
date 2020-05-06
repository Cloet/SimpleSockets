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
        public EncryptionType EncryptionMethod { get; set; } = EncryptionType.None;

		/// <summary>
		/// The default compression when sending messages.
		/// Alternate compressions can be set for each message.
		/// </summary>
        public CompressionType CompressionMethod { get; set; } = CompressionType.GZip;

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

		/// <summary>
		/// Disposes of the Socket.
		/// </summary>
		public abstract void Dispose();

    }

}