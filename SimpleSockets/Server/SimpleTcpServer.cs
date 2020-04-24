using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Helpers;
using SimpleSockets.Messaging;

namespace SimpleSockets.Server {

    public class SimpleTcpServer : SimpleServer
    {
        public override bool SslEncryption => throw new System.NotImplementedException();

        public override SocketProtocolType SocketProtocol => throw new System.NotImplementedException();

        public override void Listen(string ip, int port, int limit = 500)
        {
            if (limit <= 1)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater then or equal to 1.");
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "The port number must be between 1-65535");

            ListenerPort = port;
            ListenerIp = ip;

            var endpoint = new IPEndPoint(ListenerIPFromString(ip), port);

            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;

            Task.Run(() => {
                try {
                    using (var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)) {
                        Listener = listener;
                        listener.Bind(endpoint);
                        listener.Listen(limit);

                        OnServerStartedListening();
                        while(!Token.IsCancellationRequested) {
                            CanAcceptConnections.Reset();
                            listener.BeginAccept(OnClientConnects, listener);
                            CanAcceptConnections.Wait(Token);
                        }
                    }
                } catch (ObjectDisposedException ode) {
                    SocketLogger?.Log(ode,LogLevel.Fatal);
                }
            }, Token);

        }

        protected virtual void OnClientConnects(IAsyncResult result) {
            CanAcceptConnections.Set();
            try {
                IClientMetadata client;
                lock (ConnectedClients) {
                    var id = !ConnectedClients.Any() ? 1 : ConnectedClients.Keys.Max() + 1;
                    client = new ClientMetadata(((Socket)result.AsyncState).EndAccept(result), id);

                    var exists = ConnectedClients.FirstOrDefault(x => x.Value == client);

                    if (exists.Value == client) {
                        id = exists.Key;
                        ConnectedClients.Remove(id);
                        ConnectedClients.Add(id, client);
                    } else 
                        ConnectedClients.Add(id, client);

                    OnClientConnected(new ClientConnectedEventArgs(client));
                }
                Receive(client);
            } catch (Exception ex) {
                SocketLogger.Log("Unable to make a connection with a client", ex, LogLevel.Error);
            }
        }

        internal virtual void Receive(IClientMetadata client) {
            try {

                DataReceiver rec = client.DataReceiver;
                while (!Token.IsCancellationRequested) {
                    
                    if (rec == null)
                        rec = client.DataReceiver;

                    rec.ReceivingData.Wait(Token);
                    rec.Timeout.Reset();
                    rec.ReceivingData.Reset();

                    rec.Listener.BeginReceive(rec.Buffer, 0, rec.BufferSize, SocketFlags.None, ReceiveCallback, client);
                }
            } catch (Exception ex) {
                SocketLogger?.Log("Error receiving data from client " + client.Id, ex, LogLevel.Error);
            }
        }

        internal virtual void ReceiveCallback(IAsyncResult result) {
            var client = (IClientMetadata)result.AsyncState;
            var dReceiver = client.DataReceiver;
            dReceiver.Timeout.Set();
            try {
                if (!IsClientConnected(client.Id))
                    ShutDownClient(client.Id, DisconnectReason.Unknown);
                else {
                    var received = dReceiver.Listener.EndReceive(result);

                    if (received > 0) {
                        
                    }
                    
                    // Allow server to receive more bytes of a client.
                    dReceiver.ReceivingData.Set();
                }
            } catch (Exception ex) {
                SocketLogger?.Log("Error receiving a message.", ex, LogLevel.Error);
            }
        }

        protected override void SendToSocket(int clientId, byte[] payload, bool shutdownClient)
        {
            throw new System.NotImplementedException();
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }

    }

}