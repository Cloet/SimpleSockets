using System;

namespace SimpleSockets.Server {

    public class ClientDisconnectedEventArgs: EventArgs {

        internal ClientDisconnectedEventArgs(IClientInfo clientInfo, DisconnectReason reason) {
            ClientInfo = clientInfo;
            ReasonForDisconnect = reason;
        }

        public IClientInfo ClientInfo { get; private set; }

        public DisconnectReason ReasonForDisconnect { get; private set; }

    }

}