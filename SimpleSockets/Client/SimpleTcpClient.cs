using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Client;
using SimpleSockets.Helpers;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets
{

    public class SimpleTcpClient : SimpleClient
    {

		#region Events
		/// <summary>
		/// Event fired when the client successfully validates the ssl certificate.
		/// </summary>
		public event EventHandler SslAuthSuccess;
		protected virtual void OnSslAuthSuccess() => SslAuthSuccess?.Invoke(this, null);

		/// <summary>
		/// Event fired when client is unable to validate the ssl certificate
		/// </summary>
		public event EventHandler SslAuthFailed;
		protected virtual void OnSslAutFailed() => SslAuthFailed?.Invoke(this, null);
		#endregion

		protected SslStream _sslStream;

		protected X509Certificate2 _sslCertificate;

		protected X509Certificate2Collection _sslCertificateCollection;

		protected TlsProtocol _tlsProtocol;

		/// <summary>
		/// Indicates if an ssl certificate should be mutually authenticated.
		/// </summary>
		public bool MutualAuthentication { get; set; }

		/// <summary>
		/// Indicates if invalid ssl certificates should be accepted.
		/// </summary>
		public bool AcceptInvalidCertificates { get; set; }

		/// <summary>
		/// Indicates if the client uses ssl.
		/// </summary>
		public bool SslEncryption { get; private set; }

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public SimpleTcpClient(): base(SocketProtocolType.Tcp) {
        }

		/// <summary>
		/// Constructor, enables ssl.
		/// </summary>
		/// <param name="certData"></param>
		/// <param name="certPass"></param>
		/// <param name="tls"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		public SimpleTcpClient(SslContext context) : base(SocketProtocolType.Tcp) {
			SslEncryption = true;

			if (context.Certificate == null)
				throw new ArgumentException("No ssl certificate found.", nameof(context));

			_sslCertificate = context.Certificate;
			_tlsProtocol = context.TlsProtocol;
			AcceptInvalidCertificates = context.AcceptInvalidCertificates;
			MutualAuthentication = MutualAuthentication;
		}

		#endregion

		#region Ssl-Helpers
		// Validates a certificate
		protected bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicy)
		{
			return !AcceptInvalidCertificates ? _sslCertificate.Verify() : AcceptInvalidCertificates;
		}

		// Tries to authenticate an ssl stream.
		protected async Task<bool> Authenticate(SslStream sslStream)
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

				await sslStream.AuthenticateAsClientAsync(ServerIp, _sslCertificateCollection, protocol, !AcceptInvalidCertificates);

				if (!sslStream.IsEncrypted)
					throw new AuthenticationException("Stream from the server is not encrypted");

				if (!sslStream.IsAuthenticated)
					throw new AuthenticationException("Stream from server not authenticated.");

				if (MutualAuthentication && !sslStream.IsMutuallyAuthenticated)
					throw new AuthenticationException("Failed to mutually authenticate.");

				OnSslAuthSuccess();
				return true;
			}
			catch (AuthenticationException ex)
			{
				OnSslAutFailed();
				SocketLogger?.Log(ex, LogLevel.Fatal);
				return false;
			}
		}

		// Disposes of the sslstream.
		protected void DisposeSslStream()
		{
			if (_sslStream == null) return;
			_sslStream.Dispose();
			_sslStream = null;
		}
		#endregion

		/// <summary>
		/// Tries to connect to a server.
		/// </summary>
		/// <param name="serverIp"></param>
		/// <param name="serverPort"></param>
		/// <param name="autoReconnect">Set to 0 to disable autoreconnect.</param>
		public override void ConnectTo(string serverIp, int serverPort, TimeSpan autoReconnect, int maxReconnectAttempts) {

			if (string.IsNullOrEmpty(serverIp))
				throw new ArgumentNullException(nameof(serverIp),"Invalid server ip.");
			if (serverPort < 1 || serverPort > 65535)
				throw new ArgumentOutOfRangeException(nameof(serverPort),"A server port must be between 1 and 65535.");
			if (autoReconnect.TotalSeconds < 5) // at least 5 seconds.
				throw new ArgumentOutOfRangeException(nameof(autoReconnect),"The autoreconnect time needs to be at least 5 seconds.");

			if (SslEncryption) 
				_sslCertificateCollection = new X509Certificate2Collection { _sslCertificate };

			ServerIp = serverIp;
			ServerPort = serverPort;
			AutoReconnect = autoReconnect;
			MaxAttempts = maxReconnectAttempts;

			EndPoint = new IPEndPoint(GetIp(ServerIp), ServerPort);

			TokenSource = new CancellationTokenSource();
			Token = TokenSource.Token;

			Task.Run(() =>
			{
				if (Disposed || Token.IsCancellationRequested)
					return;

				Listener = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				Listener.BeginConnect(EndPoint, OnConnected, Listener);
				Connected.WaitOne();		

			});
        }

		// Called when the client clients.
		private void OnConnected(IAsyncResult result) {
			var socket = (Socket)result.AsyncState;

			try
			{
				socket.EndConnect(result);

				bool success = !SslEncryption;

				if (SslEncryption)
				{
					var stream = new NetworkStream(Listener);
					_sslStream = new SslStream(stream, false, ValidateCertificate, null);

					success = Authenticate(_sslStream).Result;
				}

				if (success) {
					ReconnectAttempt = 0;
					Connected.Set();
					var metadata = new SessionMetadata(Listener, -1, SocketLogger);
					OnConnectedToServer();
					Sent.Set();
					SendAuthenticationMessage();
					Receive(metadata);
				}

				if (SslEncryption && !success)
					throw new AuthenticationException("Client cannot be authenticated.");
			}
			catch (SocketException)
			{
				if (Disposed)
					return;

				if (SslEncryption)
					DisposeSslStream();

				Connected.Reset();

				if (AutoReconnect.TotalMilliseconds <= 0)
					return;

				ReconnectAttempt++;
				if (MaxAttempts == 0 || ReconnectAttempt > MaxAttempts) {
					ReconnectAttempt = 0;
					return;
				}
				SocketLogger?.Log($"Attempting to reestablish the connection, attempt {ReconnectAttempt} of {MaxAttempts}", LogLevel.Debug);

				Thread.Sleep((int)AutoReconnect.TotalMilliseconds);
				if (!Disposed && Listener != null)
					Listener.BeginConnect(EndPoint, OnConnected, Listener);
				else if (!Disposed && Listener == null)
					ConnectTo(ServerIp, ServerPort, AutoReconnect, MaxAttempts);
			} catch (ObjectDisposedException) {
				ShutDownConnectionLost();
			} catch (OperationCanceledException) {
				ShutDownConnectionLost();
			}
			catch (Exception ex) {
				ShutDownConnectionLost();
				SocketLogger?.Log("Error finalizing connection.", ex, LogLevel.Fatal);
			}
		}

		// Called after a client connects and receives bytes from a server.
		protected virtual void Receive(ISessionMetadata metadata) {
			while (!Token.IsCancellationRequested)
			{

				metadata.ReceivingData.WaitOne();
				metadata.Timeout.Reset();
				metadata.ReceivingData.Reset();

				var rec = metadata.DataReceiver;

				if (SslEncryption)
				{
					if (_sslStream == null) {
						ShutDownConnectionLost();
						throw new SocketException((int)SocketError.NotConnected);
					}

					metadata.SslStream = _sslStream;

					metadata.SslStream.BeginRead(rec.Buffer, 0, rec.Buffer.Length, ReceiveCallback, metadata);
				}
				else {
					if (metadata.Listener == null)
					{
						ShutDownConnectionLost();
						throw new SocketException((int)SocketError.NotConnected);
					}

					metadata.Listener.BeginReceive(rec.Buffer, 0, rec.Buffer.Length, SocketFlags.None, ReceiveCallback, metadata);
				}
			}
		}

		// Called everytime a client receives bytes from the server
		protected virtual void ReceiveCallback(IAsyncResult result)
		{
			var client = (ISessionMetadata)result.AsyncState;
			client.Timeout.Set();
			try
			{
				if (!client.Listener.Connected)
					throw new SocketException((int)SocketError.NotConnected);

				if (!IsConnected())
					ShutDownConnectionLost();
				else
				{
					int received = 0;
					if (SslEncryption)
						received = client.SslStream.EndRead(result);
					else
						received = client.Listener.EndReceive(result);

					Statistics?.AddReceivedBytes(received);

					if (received > 0)
					{
						SocketLogger?.Log($"Received {received} bytes.", LogLevel.Trace);
						var readBuffer = new byte[received];
						Array.Copy(client.DataReceiver.Buffer, 0, readBuffer, 0, readBuffer.Length);
						ByteDecoder(client, readBuffer);
					}
					// Allow server to receive more bytes of a client.
					client.ReceivingData.Set();
				}
			}
			catch (SocketException se) {
				if (se.ErrorCode != (int)SocketError.NotConnected && se.ErrorCode != 107)
					SocketLogger?.Log("Server was forcibly closed.", se, LogLevel.Fatal);
				ShutDownConnectionLost();
				client.ReceivingData.Set();
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error receiving a message.", ex, LogLevel.Error);
				if (SslEncryption)
					DisposeSslStream();
			}
		}

		// Send bytes to a connected client.
		protected override async Task<bool> SendToServerAsync(byte[] payload)
		{
			try
			{
				if (!IsConnected())
					throw new InvalidOperationException("Client is not connected.");

				Sent.WaitOne();
				Sent.Reset();

				if (Listener == null || (_sslStream == null && SslEncryption))
					return false;

				if (SslEncryption)
				{
					var result = _sslStream.BeginWrite(payload, 0, payload.Length, SendCallback, _sslStream);
					await Task.Factory.FromAsync(result, (r) => _sslStream.EndWrite(r));
					Statistics?.AddSentBytes(payload.Length);
					SocketLogger?.Log($"Sent {payload.Length} bytes to the server.", LogLevel.Trace);
				}
				else {
					var result = Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, _ => { }, Listener);
					var count = await Task.Factory.FromAsync(result, (r) => Listener.EndSend(r));
					Statistics?.AddSentBytes(count);
					SocketLogger?.Log($"Sent {count} bytes to the server.", LogLevel.Trace);
				}

				Statistics?.AddSentMessages(1);

				Sent.Set();
				return true;
			}
			catch (Exception ex) {
				OnMessageFailed(new MessageFailedEventArgs(payload, FailedReason.NotConnected));
				Sent.Set();
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
				return false;
			}
		}

		// Send bytes to a connected client.
		protected override bool SendToServer(byte[] payload) {
			try
			{
				if (!IsConnected())	{
					throw new InvalidOperationException("Client is not connected.");
				}

				if ((_sslStream == null && SslEncryption) || Listener == null)
					throw new InvalidOperationException(SslEncryption ? "Sslstream is null." : "Listener is null.");

				Sent.WaitOne();
				Sent.Reset();

				if (SslEncryption) {
					Statistics?.AddSentBytes(payload.Length);
					_sslStream.BeginWrite(payload, 0, payload.Length, SendCallback, _sslStream);
					SocketLogger?.Log($"Sent {payload.Length} bytes to the server.", LogLevel.Trace);
				}
				else
					Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, Listener);

				return true;
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
				OnMessageFailed(new MessageFailedEventArgs(payload, FailedReason.NotConnected));
				return false;
			}
		}

		// Called by SendToServer.
		protected void SendCallback(IAsyncResult result) {
			try
			{
				Statistics?.AddSentMessages(1);
				if (SslEncryption)
				{
					var sslStream = (SslStream)result.AsyncState;
					sslStream.EndWrite(result);
				}
				else {
					var socket = (Socket)result.AsyncState;
					var count = socket.EndSend(result);
					Statistics?.AddSentBytes(count);
					SocketLogger?.Log($"Sent {count} bytes to the server.", LogLevel.Trace);
				}
					
				Sent.Set();
			}
			catch (Exception ex) {
				Sent.Set();
				SocketLogger?.Log(ex, LogLevel.Error);
			}
		}

		/// <summary>
		/// Shutdowns the client
		/// </summary>
		public override void ShutDown()
		{
			try
			{
				base.ShutDown();
				if (SslEncryption)
					DisposeSslStream();
			}
			catch (Exception ex)
			{
				SocketLogger?.Log(ex, LogLevel.Error);
			}
		}

		/// <summary>
		/// Disposes of the client
		/// </summary>
		public override void Dispose()
        {
			base.Dispose();
        }

	}

}
