using System.Threading;
using SimpleSockets.Client;

namespace SimpleSockets {

    public class SimpleTcpClient : SimpleClient
    {
        public override bool SslEncryption => false;

        public override SocketProtocolType SocketProtocol => SocketProtocolType.Tcp;

        private readonly ManualResetEventSlim ReceivingData = new ManualResetEventSlim(true);

        public bool Disposed { get; set; }

        public SimpleTcpClient(): base() {

        }

        public void Connect(string serverIp, int serverPort, int autoReconnect) {
            if ()
        }

        public override void Dispose()
        {
            ReceivingData.Dispose();
        }
    }

}