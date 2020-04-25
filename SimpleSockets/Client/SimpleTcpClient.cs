using SimpleSockets.Client;

namespace SimpleSockets {

    public class SimpleTcpClient : SimpleClient
    {
        public override bool SslEncryption => false;

        public override SocketProtocolType SocketProtocol => SocketProtocolType.Tcp;

        private readonly ManualResetEventSlim ReceivingData = new ManualResetEventSlim(true);

        public SimpleTcpClient(): base() {

        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }

}