using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using SimpleSockets.Messaging;

namespace SimpleSockets {

    internal interface IClientMetadata: IClientInfo, IDisposable {

        DataReceiver DataReceiver { get; }

        SslStream SslStream { get; }

        ManualResetEventSlim ReceivingData { get; set; }

        ManualResetEventSlim Timeout { get; set; }

        ManualResetEventSlim WritingData { get; set; }

        Socket Listener { get; set; }

        void ResetDataReceiver();

    }

}