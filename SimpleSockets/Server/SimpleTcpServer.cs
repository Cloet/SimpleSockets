using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Helpers;
using SimpleSockets.Messaging;

namespace SimpleSockets.Server {

    public class SimpleTcpServer : SimpleServer
    {

        public SimpleTcpServer(bool useSsl): base(useSsl, SocketProtocolType.Tcp) {

        }

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
                    client = new ClientMetadata(((Socket)result.AsyncState).EndAccept(result), id, EncryptionMethod, CompressionMethod, SocketLogger);

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
                while (!Token.IsCancellationRequested) {
                    
                    client.ReceivingData.Wait(Token);
                    client.Timeout.Reset();
                    client.ReceivingData.Reset();

					var rec = client.DataReceiver;
					client.Listener.BeginReceive(rec.Buffer, 0, rec.BufferSize, SocketFlags.None, ReceiveCallback, client);
                }
            } catch (Exception ex) {
                SocketLogger?.Log("Error receiving data from client " + client.Id, ex, LogLevel.Error);
            }
        }

        internal virtual void ReceiveCallback(IAsyncResult result) {
            var client = (IClientMetadata)result.AsyncState;
            var dReceiver = client.DataReceiver;
            client.Timeout.Set();
            try {
                if (!IsClientConnected(client.Id))
                    ShutDownClient(client.Id, DisconnectReason.Unknown);
                else {
                    var received = client.Listener.EndReceive(result);

					// Add byte per byte to datareceiver,
					// This way we can use a delimiter to check if a message has been received.
					if (received > 0) {
						var readBuffer = client.DataReceiver.Buffer.Take(received).ToArray();
						for (int i = 0; i < readBuffer.Length; i++)
						{
							var end = client.DataReceiver.AppendByteToReceived(readBuffer[i]);
							if (end)
							{
								var message = client.DataReceiver.BuildMessageFromPayload(EncryptionPassphrase, PreSharedKey);
								if (message != null)
									OnMessageReceivedHandler(client, message);
								client.ResetDataReceiver();
							}
						}
					}

					// Resets buffer of the datareceiver.
					client.DataReceiver.ClearBuffer();
					
					// Allow server to receive more bytes of a client.
					client.ReceivingData.Set();
                }
            } catch (Exception ex) {
				client.ReceivingData.Set();
                SocketLogger?.Log("Error receiving a message.", ex, LogLevel.Error);
            }
        }

        protected override void SendToSocket(int clientId, byte[] payload, bool shutdownClient)
        {
            IClientMetadata client = null;
            ConnectedClients?.TryGetValue(1, out client);

            if (client != null) {
                client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, client);
            }
        }

        protected void SendCallback(IAsyncResult result) {
            var client = (IClientMetadata)result.AsyncState;
            
            try {
                client.Listener.EndSend(result);
                if (client.ShouldShutDown)
                    ShutDownClient(client.Id);
            } catch (Exception ex) {
                SocketLogger?.Log("An error occurred when sending a message to client " + client.Id, ex, LogLevel.Error);
            }
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }

    }

}