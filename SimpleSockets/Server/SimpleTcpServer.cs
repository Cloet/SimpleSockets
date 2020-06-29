using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Helpers;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets.Server {

    public class SimpleTcpServer : SimpleServer
    {

		/// <summary>
		/// Event fired when the server successfully validates the ssl certificate.
		/// </summary>
		public event EventHandler<ClientInfoEventArgs> SslAuthSuccess;
		protected virtual void OnSslAuthSuccess(ClientInfoEventArgs eventArgs) => SslAuthSuccess?.Invoke(this, eventArgs);

		/// <summary>
		/// Event fired when server is unable to validate the ssl certificate
		/// </summary>
		public event EventHandler<ClientInfoEventArgs> SslAuthFailed;
		protected virtual void OnSslAutFailed(ClientInfoEventArgs eventArgs) => SslAuthFailed?.Invoke(this, eventArgs);

		/// <summary>
		/// Indicates if Ssl Encryption is used.
		/// </summary>
		public bool SslEncryption { get; private set; }

		protected X509Certificate2 _serverCertificate = null;
		protected TlsProtocol _tlsProtocol;

		/// <summary>
		/// Maximum connected concurrent clients.
		/// Defaults to 500.
		/// </summary>
		public int MaximumConnections { get; private set; } = 500;

		/// <summary>
		/// Only used when using Ssl.
		/// </summary>
		public bool AcceptInvalidCertificates { get; set; }

		/// <summary>
		/// Only used when using Ssl.
		/// </summary>
		public bool MutualAuthentication { get; set; }

		#region Constructors

		public SimpleTcpServer(): base(SocketProtocolType.Tcp) {
			SslEncryption = false;
        }

		/// <summary>
		/// Constructor for the client, enables ssl.
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="tls"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		public SimpleTcpServer(SslContext context) : base(SocketProtocolType.Tcp)
		{
			_serverCertificate = context.Certificate ?? throw new ArgumentNullException(nameof(context));
			SslEncryption = true;

			_tlsProtocol = context.TlsProtocol;
			AcceptInvalidCertificates = context.AcceptInvalidCertificates;
			MutualAuthentication = context.MutualAuth;
		}

		#endregion

		#region Ssl Authentication

		private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicy)
		{
			return !AcceptInvalidCertificates ? _serverCertificate.Verify() : AcceptInvalidCertificates;
		}

		private async Task<bool> Authenticate(ISessionMetadata state)
		{
			try
			{
				SslProtocols protocol;

				switch (_tlsProtocol)
				{
					case TlsProtocol.Tls10:
						protocol = SslProtocols.Tls;
						break;
					case TlsProtocol.Tls11:
						protocol = SslProtocols.Tls11;
						break;
					case TlsProtocol.Tls12:
						protocol = SslProtocols.Tls12;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				await state.SslStream.AuthenticateAsServerAsync(_serverCertificate, true, protocol, false);

				if (!state.SslStream.IsEncrypted)
					throw new Exception("Stream from client " + state.Id + " is not encrypted.");

				if (!state.SslStream.IsAuthenticated)
					throw new Exception("Stream from client " + state.Id + " not authenticated.");

				if (MutualAuthentication && !state.SslStream.IsMutuallyAuthenticated)
					throw new AuthenticationException("Failed to mutually authenticate.");

				OnSslAuthSuccess(new ClientInfoEventArgs(state));
				return true;
			}
			catch (Exception ex)
			{
				OnSslAutFailed(new ClientInfoEventArgs(state));
				SocketLogger?.Log("Failed to authenticate ssl certificate of a client.",ex, LogLevel.Error);
				return false;
			}
		}

		#endregion

		/// <summary>
		/// Listen to the IP:Port combination for connections.
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public void Listen(string ip, int port, int limit)
        {
            if (limit <= 1)
                throw new ArgumentOutOfRangeException(nameof(limit),limit, "Limit must be greater then or equal to 1.");
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port),port, "The port number must be between 1-65535");

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
                        while(!Disposed && !Token.IsCancellationRequested) {
                            CanAcceptConnections.Reset();
                            listener.BeginAccept(OnClientConnects, listener);
                            CanAcceptConnections.WaitOne();
                        }
                    }
				} catch (OperationCanceledException) {
					SocketLogger?.Log("Server has been closed down.", LogLevel.Trace);
                } catch (ObjectDisposedException ode) {
                    SocketLogger?.Log(ode,LogLevel.Fatal);
                } catch (Exception ex) {
					SocketLogger?.Log("An unhandled exception occurred. " + ex.ToString(), LogLevel.Error);
				}
            }, Token);

        }

		/// <summary>
		/// Listen to the given port for connections.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public void Listen(int port, int limit) => Listen("*", port, limit);

		/// <inheritdoc />
		public override void Listen(string ip, int port) => Listen(ip, port, MaximumConnections);

		// Called when a client connects. Starts a receiving thread for the client.
		protected virtual void OnClientConnects(IAsyncResult result) {

			if (Token.IsCancellationRequested)
				return;

            try {
                ISessionMetadata client;

				// It's possible this method is still called when a cancelation of the server is requested.
				if (ConnectedClients == null) {
					return;
				}

                lock (ConnectedClients) {
                    var id = !ConnectedClients.Any() ? 1 : ConnectedClients.Keys.Max() + 1;
                    client = new SessionMetadata(((Socket)result.AsyncState).EndAccept(result), id, SocketLogger);

					if (!IsConnectionAllowed(client)) {
						SocketLogger?.Log($"Unidentified client tried to connect. {client.Info()}", LogLevel.Warning);
						return;
					}

                    var exists = ConnectedClients.FirstOrDefault(x => x.Value == client);

                    if (exists.Value == client) {
                        id = exists.Key;
                        ConnectedClients.Remove(id);
                        ConnectedClients.Add(id, client);
                    } else 
                        ConnectedClients.Add(id, client);
					client.WritingData.Set();
                }
				CanAcceptConnections.Set();	
				
				if (SslEncryption)
				{
					var stream = new NetworkStream(client.Listener);
					client.SslStream = new SslStream(stream, false, AcceptCertificate);

					SocketLogger?.Log($"Authenticating client {client.Id}...", LogLevel.Trace);
					var success = Authenticate(client).Result;
					SocketLogger?.Log($"Done authenticating client {client.Id}.", LogLevel.Trace);

					if (!success) {
						lock (ConnectedClients) {
							ConnectedClients.Remove(client.Id);
						}
						SocketLogger?.Log("Unable to authenticate server.", LogLevel.Error);
					}
				}

				OnClientConnected(new ClientInfoEventArgs(client));
				Task.Run( () => Receive(client));


            } catch (Exception ex) {
				CanAcceptConnections.Set();
                SocketLogger.Log("Unable to make a connection with a client", ex, LogLevel.Error);
            }
        }

		// Starts receiving data. Calls ReceiveCallback when data is received.
        protected virtual void Receive(ISessionMetadata client) {
			try
			{
				while (!Token.IsCancellationRequested)
				{

					client.ReceivingData.WaitOne();
					client.Timeout.Reset();
					client.ReceivingData.Reset();

					var rec = client.DataReceiver;

					if (SslEncryption)
					{
						var sslStream = client.SslStream;
						sslStream.BeginRead(rec.Buffer, 0, rec.Buffer.Length, ReceiveCallback, client);
					}
					else {
						client.Listener.BeginReceive(rec.Buffer, 0, rec.BufferSize, SocketFlags.None, ReceiveCallback, client);
					}

					if (Timeout.TotalMilliseconds > 0 && !client.Timeout.WaitOne(Timeout))
					  	throw new SocketException((int)SocketError.TimedOut);
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

		// Handles the received data from Receive
        protected virtual void ReceiveCallback(IAsyncResult result) {
            var client = (ISessionMetadata)result.AsyncState;
            client.Timeout.Set();
            try {
                if (!IsClientConnected(client.Id))
                    ShutDownClient(client.Id, DisconnectReason.Unknown);
                else {
					int received = 0;

					if (SslEncryption)
						received = client.SslStream.EndRead(result);
					else
						received = client.Listener.EndReceive(result);

					Statistics?.AddReceivedBytes(received);

					// Add byte per byte to datareceiver,
					// This way we can use a delimiter to check if a message has been received.
					if (received > 0) {
						SocketLogger?.Log($"Received {received} bytes from a client. {client.Info()}", LogLevel.Trace);
						var readBuffer = new byte[received];
						Array.Copy(client.DataReceiver.Buffer, 0, readBuffer, 0, readBuffer.Length);
						ByteDecoder(client, readBuffer);
					}

					// Resets buffer of the datareceiver.
					client.DataReceiver.ClearBuffer();
					
					// Allow server to receive more bytes of a client.
					client.ReceivingData.Set();
                }
            } catch (Exception ex) {
				client.ReceivingData.Set();
                SocketLogger?.Log("Error receiving a message.", ex, LogLevel.Error);
				SocketLogger?.Log("Trying to restart the datareceiver for client " + client.Id, LogLevel.Debug);
				Receive(client);
			}
        }

		// Sends data to a client. When data is send SendCallback is called
        protected override bool SendToSocket(int clientId, byte[] payload)
        {
			ISessionMetadata client = null;
			ConnectedClients?.TryGetValue(clientId, out client);
			try
			{

				if (client != null) {
					client.WritingData.WaitOne();
					client.WritingData.Reset();

					if (SslEncryption)
					{
						Statistics?.AddSentBytes(payload.Length);
						client.SslStream.BeginWrite(payload, 0, payload.Length, SendCallback, client);
						SocketLogger?.Log($"Sent {payload.Length} bytes to a client. {client.Info()}", LogLevel.Trace);
					}
					else {
						client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, client);
					}

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

		// Called when SendToSocket completes.
		protected void SendCallback(IAsyncResult result)
		{
			var client = (ISessionMetadata)result.AsyncState;

			try
			{

				Statistics?.AddSentMessages(1);

				if (SslEncryption)
					client.SslStream.EndWrite(result);
				else
				{
					var count = client.Listener.EndSend(result);
					Statistics?.AddSentBytes(count);
					SocketLogger?.Log($"Sent {count} bytes to a client. {client.Info()}", LogLevel.Trace);
				}

				client.WritingData.Set();
			}
			catch (Exception ex)
			{
				client.WritingData.Set();
				SocketLogger?.Log("An error occurred when sending a message to client " + client.Id, ex, LogLevel.Error);
			}
		}

		// Similar to SendToSocket but is done async.
		protected override async Task<bool> SendToSocketAsync(int clientId, byte[] payload) {

			ISessionMetadata client = null;
			ConnectedClients?.TryGetValue(clientId, out client);
			try
			{
				if (client != null) {

					client.WritingData.WaitOne();
					client.WritingData.Reset();
					Statistics?.AddSentMessages(1);

					if (SslEncryption)
					{
						var result = client.SslStream.BeginWrite(payload, 0, payload.Length, _ => { }, client);
						await Task.Factory.FromAsync(result, (r) => client.SslStream.EndWrite(r));
						Statistics?.AddSentBytes(payload.Length);
						SocketLogger?.Log($"Sent {payload.Length} bytes to a client. {client.Info()}", LogLevel.Trace);
					}
					else {
						var result = client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, _ => { }, client);
						var count = await Task.Factory.FromAsync(result, (r) => client.Listener.EndSend(r));
						Statistics?.AddSentBytes(count);
						SocketLogger?.Log($"Sent {count} bytes to a client. {client.Info()}", LogLevel.Trace);
					}

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

		/// <summary>
		/// Disposes of the server.
		/// </summary>
        public override void Dispose()
        {
			base.Dispose();
		}

    }

}