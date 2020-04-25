using System;

namespace SimpleSockets {

    public class ClientDisConnectedEventArgs: EventArgs {

        internal ClientDisConnectedEventArgs(IClientInfo clientInfo, DisconnectReason reason) {
            ClientInfo = clientInfo;
            ReasonForDisconnect = reason;
        }

        public IClientInfo ClientInfo { get; private set; }

        public DisconnectReason ReasonForDisconnect { get; private set; }

    }

}