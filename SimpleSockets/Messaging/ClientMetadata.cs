using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using SimpleSockets.Helpers;

namespace SimpleSockets.Messaging {

    internal class ClientMetadata : IClientMetadata
    {

        public int Id { get; private set; }

        public string ClientName { get; set; }

        public string Guid { get; set; }

        public string OsVersion { get; set; }

        public string UserDomainName { get; set; }

        public bool ShouldShutDown { get; set; }

        public SslStream SslStream { get; private set; }

        public ManualResetEventSlim ReceivingData { get; set; } = new ManualResetEventSlim(true);

        public ManualResetEventSlim Timeout { get; set; } = new ManualResetEventSlim(true);

        public ManualResetEventSlim WritingData { get; set; } = new ManualResetEventSlim(false);

        public Socket Listener { get; set; }

        public DataReceiver DataReceiver { get; private set;}

        private LogHelper _logger;

        public ClientMetadata(Socket listener, int id, LogHelper logger = null) {
            Id = id;
            ShouldShutDown = false;
            _logger = logger;
            Listener = listener;
            DataReceiver = new DataReceiver(logger);
        }

        public void ResetDataReceiver(byte[] extraBytes = null)
        {
            DataReceiver = null;
            DataReceiver = new DataReceiver(_logger, extraBytes);
        }

        public void Dispose() {
            DataReceiver = null;
        }
    }

}