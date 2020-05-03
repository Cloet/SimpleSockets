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

namespace SimpleSockets.Server {

    public class SimpleTcpServer : SimpleServer
    {

        public SimpleTcpServer(): base(false, SocketProtocolType.Tcp) {

        }

		/// <summary>
		/// Constructor for the client, enables ssl.
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="tls"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		public SimpleTcpServer(X509Certificate2 certificate, TlsProtocol tls = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false) : base(true, SocketProtocolType.Tcp)
		{
			_serverCertificate = certificate ?? throw new ArgumentNullException(nameof(certificate));

			_tlsProtocol = tls;
			AcceptInvalidCertificates = acceptInvalidCertificates;
			MutualAuthentication = mutualAuth;
		}

		/// <summary>
		/// Constructor, enables ssl.
		/// </summary>
		/// <param name="certData"></param>
		/// <param name="certPass"></param>
		/// <param name="tls"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		public SimpleTcpServer(byte[] certData, string certPass, TlsProtocol tls = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false) : base(true, SocketProtocolType.Tcp)
		{
			if (certData == null || certData.Length == 0)
				throw new ArgumentNullException(nameof(certData));

			if (string.IsNullOrEmpty(certPass))
				_serverCertificate = new X509Certificate2(certData);
			else
				_serverCertificate = new X509Certificate2(certData, certPass);

			_tlsProtocol = tls;
			AcceptInvalidCertificates = acceptInvalidCertificates;
			MutualAuthentication = mutualAuth;
		}

		/// <summary>
		/// Constructor, enables ssl.
		/// </summary>
		/// <param name="cert">The location of the certificate.</param>
		/// <param name="certPass"></param>
		/// <param name="tls"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		public SimpleTcpServer(string cert, string certPass, TlsProtocol tls = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false) : this(File.ReadAllBytes(Path.GetFullPath(cert)), certPass, tls, acceptInvalidCertificates, mutualAuth)
		{
		}

		#region Ssl Authentication

		private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicy)
		{
			return !AcceptInvalidCertificates ? _serverCertificate.Verify() : AcceptInvalidCertificates;
		}

		private async Task<bool> Authenticate(IClientMetadata state)
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
		public override void Listen(string ip, int port, int limit = 500)
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

        protected virtual async void OnClientConnects(IAsyncResult result) {

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

					CanAcceptConnections.Set();
					client.WritingData.Set();
                }

				if (!IsConnectionAllowed(client)) {
					SocketLogger?.Log("A blacklisted ip tried to connect.",LogLevel.Error);
					lock(ConnectedClients)
					{
						ConnectedClients.Remove(client.Id);
					}
					return;
				}

				if (SslEncryption)
				{
					var stream = new NetworkStream(client.Listener);
					client.SslStream = new SslStream(stream, false, AcceptCertificate);

					var success = await Authenticate(client);

					if (success)
					{
						OnClientConnected(new ClientInfoEventArgs(client));
						Receive(client);
					}
					else {
						lock (ConnectedClients) {
							ConnectedClients.Remove(client.Id);
						}
						SocketLogger?.Log("Unable to authenticate server.", LogLevel.Error);
					}
				}
				else {
					OnClientConnected(new ClientInfoEventArgs(client));
					Receive(client);
				}

            } catch (Exception ex) {
				CanAcceptConnections.Set();
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

					if (SslEncryption)
					{
						var sslStream = client.SslStream;
						sslStream.BeginRead(rec.Buffer, 0, rec.Buffer.Length, ReceiveCallback, client);
					}
					else {
						client.Listener.BeginReceive(rec.Buffer, 0, rec.BufferSize, SocketFlags.None, ReceiveCallback, client);
					}

					if (Timeout.TotalMilliseconds > 0 && !client.Timeout.Wait(Timeout))
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

        internal virtual void ReceiveCallback(IAsyncResult result) {
            var client = (IClientMetadata)result.AsyncState;
            var dReceiver = client.DataReceiver;
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

					if (SslEncryption)
					{
						Statistics?.AddSentBytes(payload.Length);
						client.SslStream.BeginWrite(payload, 0, payload.Length, SendCallback, client);
					}
					else {
						client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, client);
					}
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
					Statistics?.AddSentMessages(1);

					if (SslEncryption)
					{
						var result = client.SslStream.BeginWrite(payload, 0, payload.Length, _ => { }, client);
						await Task.Factory.FromAsync(result, (r) => client.SslStream.EndWrite(r));
						Statistics?.AddSentBytes(payload.Length);
					}
					else {
						var result = client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, _ => { }, client);
						var count = await Task.Factory.FromAsync(result, (r) => client.Listener.EndSend(r));
						Statistics?.AddSentBytes(count);
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

        protected void SendCallback(IAsyncResult result) {
            var client = (IClientMetadata)result.AsyncState;
            
            try {

				Statistics?.AddSentMessages(1);

				if (SslEncryption)
					client.SslStream.EndWrite(result);
				else {
					var count = client.Listener.EndSend(result);
					Statistics?.AddSentBytes(count);
				}

				client.WritingData.Set();
            } catch (Exception ex) {
				client.WritingData.Set();
                SocketLogger?.Log("An error occurred when sending a message to client " + client.Id, ex, LogLevel.Error);
            }
        }

		/// <summary>
		/// Disposes of the server.
		/// </summary>
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