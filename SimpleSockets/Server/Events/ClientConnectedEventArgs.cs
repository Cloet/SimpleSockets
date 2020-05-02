using System;

namespace SimpleSockets.Server {

    public class ClientConnectedEventArgs: EventArgs {

        internal ClientConnectedEventArgs(IClientInfo clientInfo) {
            ClientInfo = clientInfo;
        }

        public IClientInfo ClientInfo { get; private set; }

    }

}