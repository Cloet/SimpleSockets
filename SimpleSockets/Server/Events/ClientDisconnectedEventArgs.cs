using System;

namespace SimpleSockets.Server {

    public class ClientDisconnectedEventArgs: EventArgs {

        internal ClientDisconnectedEventArgs(ISessionInfo clientInfo, DisconnectReason reason) {
            ClientInfo = clientInfo;
            ReasonForDisconnect = reason;
        }

        public ISessionInfo ClientInfo { get; private set; }

        public DisconnectReason ReasonForDisconnect { get; private set; }

    }

}