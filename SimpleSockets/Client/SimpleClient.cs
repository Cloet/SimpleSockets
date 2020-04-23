using System;
using System.Net.Sockets;
using SimpleSockets.Helpers;

namespace SimpleSockets.Client {

    public abstract class SimpleClient : SimpleSocket
    {
        private Action<string> _logger;

        public override Action<string> Logger { 
            get => _logger;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SocketLogger = LogHelper.InitializeLogger(true, SslEncryption , SocketProtocolType.Tcp == this.SocketProtocol, value, this.LogLevel);
                _logger = value;
            }
        }
    }

}