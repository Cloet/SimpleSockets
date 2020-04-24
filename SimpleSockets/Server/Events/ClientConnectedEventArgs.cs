using System;

namespace SimpleSockets {

    public class ClientConnectedEventArgs: EventArgs {

        internal ClientConnectedEventArgs(IClientInfo clientInfo) {
            ClientInfo = clientInfo;
        }

        public IClientInfo ClientInfo { get; set; }

    }

}