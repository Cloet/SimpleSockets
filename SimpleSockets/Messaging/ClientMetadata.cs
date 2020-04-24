using System.Net.Sockets;
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


        public DataReceiver DataReceiver { get; private set;}

        private LogHelper _logger;
        private Socket _listener;

        public ClientMetadata(Socket listener, int id, LogHelper logger = null) {
            Id = id;
            ShouldShutDown = false;
            _logger = logger;
            _listener = listener;
            DataReceiver = new DataReceiver(listener, logger);
        }

        public void ResetDataReceiver()
        {
            DataReceiver.Dispose();
            DataReceiver = new DataReceiver(_listener, _logger);
        }

        public void Dispose() {
            DataReceiver.Dispose();
        }
    }

}