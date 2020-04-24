using System;
using System.Net.Security;
using System.Net.Sockets;
using SimpleSockets.Messaging;

namespace SimpleSockets {

    internal interface IClientMetadata: IClientInfo, IDisposable {

        DataReceiver DataReceiver { get; }

        bool ShouldShutDown { get; set; }

    }

}