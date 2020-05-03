using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Client;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Messaging;

namespace SimpleSockets {

    public class SimpleTcpClient : SimpleClient
    {

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="useSsl"></param>
        public SimpleTcpClient(): base(false, SocketProtocolType.Tcp) {
        }

		/// <summary>
		/// Constructor for the client, enables ssl.
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="tls"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		public SimpleTcpClient(X509Certificate2 certificate, TlsProtocol tls = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false): base(true, SocketProtocolType.Tcp) {
			_sslCertificate = certificate ?? throw new ArgumentNullException(nameof(certificate));

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
		public SimpleTcpClient(byte[] certData, string certPass, TlsProtocol tls = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false) : base(true,SocketProtocolType.Tcp) {
			if (certData == null || certData.Length == 0)
				throw new ArgumentNullException(nameof(certData));

			if (string.IsNullOrEmpty(certPass))
				_sslCertificate = new X509Certificate2(certData);
			else
				_sslCertificate = new X509Certificate2(certData, certPass);

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
		public SimpleTcpClient(string cert, string certPass, TlsProtocol tls = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false) : this(File.ReadAllBytes(Path.GetFullPath(cert)), certPass, tls, acceptInvalidCertificates, mutualAuth) {
		}

		/// <summary>
		/// Tries to connect to a server.
		/// </summary>
		/// <param name="serverIp"></param>
		/// <param name="serverPort"></param>
		/// <param name="autoReconnect">Set to 0 to disable autoreconnect.</param>
        public void ConnectTo(string serverIp, int serverPort, int autoReconnect = 5) {

			if (string.IsNullOrEmpty(serverIp))
				throw new ArgumentNullException(nameof(serverIp));
			if (serverPort < 1 || serverPort > 65535)
				throw new ArgumentOutOfRangeException(nameof(serverPort));
			if (autoReconnect > 0 && autoReconnect < 4)
				throw new ArgumentOutOfRangeException(nameof(autoReconnect));

			if (SslEncryption) 
				_sslCertificateCollection = new X509Certificate2Collection { _sslCertificate };

			ServerIp = serverIp;
			ServerPort = serverPort;
			AutoReconnect = new TimeSpan(0, 0, 0, autoReconnect);

			EndPoint = new IPEndPoint(GetIp(ServerIp), ServerPort);

			TokenSource = new CancellationTokenSource();
			Token = TokenSource.Token;

			Task.Run(() =>
			{
				if (Token.IsCancellationRequested)
					return;

				Listener = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				Listener.BeginConnect(EndPoint, OnConnected, Listener);
				Connected.Wait(Token);		

			});
        }

		private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicy) {
			return !AcceptInvalidCertificates ? _sslCertificate.Verify() : AcceptInvalidCertificates;
		}

		private async Task<bool> Authenticate(SslStream sslStream) {
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
			catch (AuthenticationException ex) {
				OnSslAutFailed();
				SocketLogger?.Log(ex, LogLevel.Fatal);
				return false;
			}
		}

		private void DisposeSslStream() {
			if (_sslStream == null) return;
			_sslStream.Dispose();
			_sslStream = null;
		}

		private async void OnConnected(IAsyncResult result) {
			var socket = (Socket)result.AsyncState;

			try
			{
				socket.EndConnect(result);

				bool success = !SslEncryption;

				if (SslEncryption)
				{
					var stream = new NetworkStream(Listener);
					_sslStream = new SslStream(stream, false, ValidateCertificate, null);

					success = await Authenticate(_sslStream);
				}

				if (success) {
					Connected.Set();
					var metadata = new ClientMetadata(Listener, -1, SocketLogger);
					Sent.Set();
					OnConnectedToServer();
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

				if (AutoReconnect.Seconds == 0)
					return;

				Thread.Sleep(AutoReconnect.Seconds);
				if (Listener != null & !Disposed)
					Listener.BeginConnect(EndPoint, OnConnected, Listener);
				else if (Listener == null && !Disposed)
					ConnectTo(ServerIp, ServerPort, AutoReconnect.Seconds);
			}
			catch (Exception ex) {
				OnDisconnectedFromServer();
				SocketLogger?.Log("Error finalizing connection.", ex, LogLevel.Fatal);
			}
		}

		internal virtual void Receive(IClientMetadata metadata) {
			while (!Token.IsCancellationRequested)
			{

				metadata.ReceivingData.Wait(Token);
				metadata.Timeout.Reset();
				metadata.ReceivingData.Reset();

				var rec = metadata.DataReceiver;

				if (SslEncryption)
				{
					if (_sslStream == null) {
						ShutDown();
						throw new SocketException((int)SocketError.NotConnected);
					}

					metadata.SslStream = _sslStream;

					metadata.SslStream.BeginRead(rec.Buffer, 0, rec.Buffer.Length, ReceiveCallback, metadata);
				}
				else {
					if (metadata.Listener == null)
					{
						ShutDown();
						throw new SocketException((int)SocketError.NotConnected);
					}

					metadata.Listener.BeginReceive(rec.Buffer, 0, rec.Buffer.Length, SocketFlags.None, ReceiveCallback, metadata);
				}
			}
		}

		internal virtual void ReceiveCallback(IAsyncResult result)
		{
			var client = (IClientMetadata)result.AsyncState;
			var dReceiver = client.DataReceiver;
			client.Timeout.Set();
			try
			{
				if (!client.Listener.Connected)
					throw new SocketException((int)SocketError.NotConnected);

				if (!IsConnected())
					ShutDown();
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
						var readBuffer = client.DataReceiver.Buffer.Take(received).ToArray();
						for (int i = 0; i < readBuffer.Length; i++)
						{
							var end = client.DataReceiver.AppendByteToReceived(readBuffer[i]);
							if (end)
							{
								var message = client.DataReceiver.BuildMessageFromPayload(EncryptionPassphrase, PreSharedKey);
								if (message != null)
									OnMessageReceivedHandler(message);
								client.ResetDataReceiver();
							}
						}
					}
					// Allow server to receive more bytes of a client.
					client.ReceivingData.Set();
				}
			}
			catch (SocketException se) {
				if (se.ErrorCode != (int)SocketError.NotConnected)
					SocketLogger?.Log("Server was forcibly closed.", se, LogLevel.Fatal);
				ShutDown();
				client.ReceivingData.Set();
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error receiving a message.", ex, LogLevel.Error);
				if (SslEncryption)
					DisposeSslStream();
			}
		}

		/// <summary>
		/// Shutdowns the client
		/// </summary>
		public void ShutDown() {
			try
			{
				if (SslEncryption)
					DisposeSslStream();

				Connected.Reset();
				if (Listener != null) {
					Listener.Shutdown(SocketShutdown.Both);
					Listener.Close();
					Listener = null;
					OnDisconnectedFromServer();
				}
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error closing the client", ex, LogLevel.Error);
			}
		}

		protected override async Task<bool> SendToServerAsync(byte[] payload)
		{
			try
			{
				if (!IsConnected())
					throw new InvalidOperationException("Client is not connected.");

				Sent.Wait();
				Sent.Reset();

				if (Listener == null || _sslStream == null)
					return false;

				if (SslEncryption)
				{
					var result = _sslStream.BeginWrite(payload, 0, payload.Length, SendCallback, _sslStream);
					await Task.Factory.FromAsync(result, (r) => _sslStream.EndWrite(r));
					Statistics?.AddSentBytes(payload.Length);
				}
				else {
					var result = Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, _ => { }, Listener);
					var count = await Task.Factory.FromAsync(result, (r) => Listener.EndSend(r));
					Statistics?.AddSentBytes(count);
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

		protected override void SendToServer(byte[] payload) {
			try
			{
				if (!IsConnected())	{
					throw new InvalidOperationException("Client is not connected.");
				}

				if (_sslStream == null || Listener == null)
					throw new InvalidOperationException(SslEncryption ? "Sslstream is null." : "Listener is null.");

				Sent.Wait();
				Sent.Reset();

				if (SslEncryption) {
					Statistics?.AddSentBytes(payload.Length);
					_sslStream.BeginWrite(payload, 0, payload.Length, SendCallback, _sslStream);
				}
				else
					Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, Listener);
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
				OnMessageFailed(new MessageFailedEventArgs(payload, FailedReason.NotConnected));
			}
		}

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
				}
					
				Sent.Set();
			}
			catch (Exception ex) {
				Sent.Set();
				SocketLogger?.Log(ex, LogLevel.Error);
			}
		}

		/// <summary>
		/// Disposes of the client
		/// </summary>
		public override void Dispose()
        {
			try
			{
				if (!Disposed)
				{
					ShutDown();
					TokenSource.Cancel();
					Sent.Dispose();
					Connected.Dispose();
					Disposed = true;
				}
			}
			catch (Exception ex) {
				SocketLogger?.Log(ex, LogLevel.Error);
			}
        }

	}

}