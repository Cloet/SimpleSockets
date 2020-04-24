using System;
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

        private DateTime _startTime;

        private long _receivedBytes = 0;

        private long _sentBytes = 0;

        internal LogHelper SocketLogger { get; set; }

        protected CancellationTokenSource TokenSource { get; set;}

        protected CancellationToken Token { get; set; }

        internal DataReceiver DataReceiver { get; set; }

        /// <summary>
        /// Indicates if a socket is allowed to receive files from another socket.
        /// </summary>
        /// <value></value>
        public bool FileTransferEnabled { get; set; }

        public abstract Action<string> Logger { get; set; }

        public byte[] EncryptionPassphrase { get; set; }

        public EncryptionType EncryptionMethod { get; set; } = EncryptionType.None;

        public CompressionType CompressionMethod { get; set; } = CompressionType.GZip;

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

        public DateTime StartTime { get => _startTime; }

        public TimeSpan UpTime { get => DateTime.Now.ToUniversalTime() - StartTime ; }

        public long Received { get => _receivedBytes; internal set { _receivedBytes = value; }}

        public long Sent { get => _sentBytes; internal set { _sentBytes = value; }}

        public abstract bool SslEncryption { get; }

        public abstract SocketProtocolType SocketProtocol { get; }

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

        public string Statistics { 
            get {
                var stats = "=======================================================================" + Environment.NewLine;
                stats    += "|  Statistics                                                         |" + Environment.NewLine;
                stats    += "|---------------------------------------------------------------------|" + Environment.NewLine;
                stats    += "| - Started        : " + StartTime.ToString().PadRight(48) + "|" + Environment.NewLine;
                stats    += "| - UpTime         : " + UpTime.ToString().PadRight(48) + "|" + Environment.NewLine;
                stats    += "| - Bytes Received : " + Received.ToString().PadRight(48) + "|" + Environment.NewLine;
                stats    += "| - Bytes Sent     : " + Sent.ToString().PadRight(48) + "|" + Environment.NewLine;
                stats    += "| - Protocol       : " + Enum.GetName(typeof(SocketProtocolType), SocketProtocol).ToString().PadRight(48) + "|" + Environment.NewLine;
                stats    += "| - Ssl            : " + (SslEncryption ? "Yes".PadRight(48) : "No".PadRight(48)) + "|" + Environment.NewLine;
                stats    += "=======================================================================" + Environment.NewLine;
                return stats;
            }
        }
        
        /// <summary>
        /// Disposes of the Socket.
        /// </summary>
        public abstract void Dispose();

    }

}