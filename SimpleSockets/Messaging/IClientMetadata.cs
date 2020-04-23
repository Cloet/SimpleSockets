using System.Net.Sockets;

namespace SimpleSockets {

    internal interface IClientMetadata: IClientInfo {
        Socket Listener { get; set; }
    }

}