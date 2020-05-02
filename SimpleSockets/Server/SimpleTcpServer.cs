using System;
using System.Collections.Generic;
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
						Listening = true;

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
                    client = new ClientMetadata(((Socket)result.AsyncState).EndAccept(result), id, SocketLogger);

                    var exists = ConnectedClients.FirstOrDefault(x => x.Value == client);

                    if (exists.Value == client) {
                        id = exists.Key;
                        ConnectedClients.Remove(id);
                        ConnectedClients.Add(id, client);
                    } else 
                        ConnectedClients.Add(id, client);

					client.WritingData.Set();
                    OnClientConnected(new ClientConnectedEventArgs(client));
                }
                Receive(client);
            } catch (Exception ex) {
                SocketLogger.Log("Unable to make a connection with a client", ex, LogLevel.Error);
            }
        }

        internal virtual void Receive(IClientMetadata client) {
			try
			{
				while (!Token.IsCancellationRequested)
				{

					client.ReceivingData.Wait(Token);
					client.Timeout.Reset();
					client.ReceivingData.Reset();

					var rec = client.DataReceiver;
					client.Listener.BeginReceive(rec.Buffer, 0, rec.BufferSize, SocketFlags.None, ReceiveCallback, client);
				}
			}
			catch (SocketException se) {
				if (se.SocketErrorCode == SocketError.TimedOut)
				{
					SocketLogger?.Log("Client" + client.Id + " disconnected from the server.", LogLevel.Debug);
					OnClientDisconnected(new ClientDisconnectedEventArgs(client, DisconnectReason.Timeout));
				}
				else
					SocketLogger?.Log(se, LogLevel.Error);
				ShutDownClient(client.Id);
            } catch (Exception ex) {
                SocketLogger?.Log("Error receiving data from client " + client.Id, ex, LogLevel.Error);
				ShutDownClient(client.Id);
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
				Receive(client);
				SocketLogger?.Log("Trying to restart the datareceiver for client " + client.Id, LogLevel.Debug);
			}
        }

        protected override void SendToSocket(int clientId, byte[] payload)
        {
			IClientMetadata client = null;
			ConnectedClients?.TryGetValue(clientId, out client);
			try
			{

				if (client != null) {
					client.WritingData.Wait();
					client.WritingData.Reset();
					client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, client);
				}

			}
			catch (Exception ex) {
				if (client != null)
					client.WritingData.Set();
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
			}
        }

		protected override async Task<bool> SendToSocketAsync(int clientId, byte[] payload) {

			IClientMetadata client = null;
			ConnectedClients?.TryGetValue(clientId, out client);
			try
			{
				if (client != null) {

					client.WritingData.Wait();
					client.WritingData.Reset();

					var result = client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, _ => { }, client);
					var count = await Task.Factory.FromAsync(result, (r) => client.Listener.EndSend(r));
					Statistics?.AddSentBytes(count);
					client.WritingData.Set();

					return true;
				}

				return false;
			}
			catch (Exception ex) {
				if (client != null)
					client.WritingData.Set();
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
				return false;
			}
		}

        protected void SendCallback(IAsyncResult result) {
            var client = (IClientMetadata)result.AsyncState;
            
            try {
                var count = client.Listener.EndSend(result);
				Statistics?.AddSentBytes(count);
				client.WritingData.Set();
            } catch (Exception ex) {
				client.WritingData.Set();
                SocketLogger?.Log("An error occurred when sending a message to client " + client.Id, ex, LogLevel.Error);
            }
        }

        public override void Dispose()
        {
			try
			{
				if (!Disposed)
				{
					TokenSource.Cancel();
					TokenSource.Dispose();
					Listening = false;
					Listener.Dispose();
					CanAcceptConnections.Dispose();

					foreach (var id in ConnectedClients.Keys.ToList())
					{
						ShutDownClient(id, DisconnectReason.Kicked);
					}

					ConnectedClients = new Dictionary<int, IClientMetadata>();
					TokenSource.Dispose();
					Disposed = true;
					GC.SuppressFinalize(this);
				}
				else
				{
					throw new ObjectDisposedException(nameof(SimpleTcpServer), "This object is already disposed.");
				}

			}
			catch (Exception ex)
			{
				SocketLogger?.Log(ex, LogLevel.Error);
			}
		}

    }

}