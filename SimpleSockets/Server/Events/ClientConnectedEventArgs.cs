namespace SimpleSockets {

    public class ClientConnectedEventArgs {

        internal ClientConnectedEventArgs(IClientInfo clientInfo) {
            ClientInfo = clientInfo;
        }

        public IClientInfo ClientInfo { get; set; }

    }

}