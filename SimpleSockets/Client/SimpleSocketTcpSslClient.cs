using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets.Client
{

	public class SimpleSocketTcpSslClient: SimpleSocketClient
	{

		#region Vars

		private SslStream _sslStream;
		private readonly X509Certificate2 _sslCertificate;
		private X509Certificate2Collection _sslCertificateCollection;
		private readonly ManualResetEvent _mreRead = new ManualResetEvent(true);
		private readonly ManualResetEvent _mreWriting = new ManualResetEvent(true);
		private readonly TlsProtocol _tlsProtocol;

		public bool MutualAuthentication { get; set; }

		public bool AcceptInvalidCertificates { get; set; }

		#endregion

		#region Constructor

		public SimpleSocketTcpSslClient(string cert, string certPass, TlsProtocol tls = TlsProtocol.Tls12,bool acceptInvalidCertificates = true, bool mutualAuth = false) : base()
		{
			if (string.IsNullOrEmpty(cert))
				throw new ArgumentNullException(nameof(cert));

			if (string.IsNullOrEmpty(certPass))
				_sslCertificate = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(cert)));
			else
				_sslCertificate = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(cert)), certPass);

			_tlsProtocol = tls;
			AcceptInvalidCertificates = acceptInvalidCertificates;
			MutualAuthentication = mutualAuth;
		}

		#endregion

		#region Start

		public override void StartClient(string ipServer, int port, int reconnectInSeconds = 5)
		{
			if (string.IsNullOrEmpty(ipServer))
				throw new ArgumentNullException(nameof(ipServer));
			if (port < 1 || port > 65535)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (reconnectInSeconds < 3)
				throw new ArgumentOutOfRangeException(nameof(reconnectInSeconds));

			_sslCertificateCollection = new X509Certificate2Collection { _sslCertificate };

			Ip = ipServer;
			Port = port;
			ReconnectInSeconds = reconnectInSeconds;
			KeepAliveTimer.Enabled = false;

			if (EnableExtendedAuth)
				SendAuthMessage();

			Endpoint = new IPEndPoint(GetIp(ipServer), port);

			TokenSource = new CancellationTokenSource();
			Token = TokenSource.Token;

			Task.Run(SendFromQueue, Token);

			Task.Run(() =>
			{
				try
				{
					//Try and connect
					Listener = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					Listener.BeginConnect(Endpoint, OnConnectCallback, Listener);
					ConnectedMre.WaitOne();

					//If client is connected activate connected event
					if (IsConnected())
					{
						RaiseConnected();
					}
					else
					{
						KeepAliveTimer.Enabled = false;
						RaiseDisconnected();
						Close();
						ConnectedMre.Reset();
						Listener.BeginConnect(Endpoint, OnConnectCallback, Listener);
					}

				}
				catch (Exception ex)
				{
					RaiseErrorThrown(ex);
				}
			}, Token);
		}

		protected override void OnConnectCallback(IAsyncResult result)
		{
			var client = (Socket)result.AsyncState;

			try
			{
				//Client is connected to server and set connected variable
				client.EndConnect(result);

				var stream = new NetworkStream(Listener);
				_sslStream = new SslStream(stream, false, ValidateCertificate, null);

				var success = Authenticate(_sslStream).Result;

				if (success)
				{
					ConnectedMre.Set();
					KeepAliveTimer.Enabled = true;
					var state = new ClientMetadata(Listener);
					Receive(state);
				}
				else
				{
					throw new AuthenticationException("Client cannot be authenticated.");
				}
			}
			catch (SocketException)
			{
				DisposeSslStream();

				Thread.Sleep(ReconnectInSeconds * 1000);
				Listener.BeginConnect(Endpoint, OnConnectCallback, Listener);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		#endregion

		#region Ssl Auth

		//Validates the certificate
		private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicy)
		{
			return !AcceptInvalidCertificates ? _sslCertificate.Verify() : AcceptInvalidCertificates;
		}

		//Authenticate SslStream
		private async Task<bool> Authenticate(SslStream sslStream)
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


				await sslStream.AuthenticateAsClientAsync(Ip, _sslCertificateCollection, protocol, !AcceptInvalidCertificates);

				if (!sslStream.IsEncrypted)
				{
					throw new AuthenticationException("Stream from server is not encrypted.");
				}

				if (!sslStream.IsAuthenticated)
				{
					throw new AuthenticationException("Stream from server not authenticated.");
				}

				if (MutualAuthentication && !sslStream.IsMutuallyAuthenticated)
				{
					throw new AuthenticationException("Failed to mutually authenticate.");
				}

				RaiseAuthSuccess();
				return true;
			}
			catch (AuthenticationException ex)
			{
				RaiseAuthFailed();
				RaiseErrorThrown(ex);
				RaiseLog("Failed to authenticate ssl certificate.");
				return false;
			}
		}

		private void DisposeSslStream()
		{
			if (_sslStream == null) return;
			_sslStream.Dispose();
			_sslStream = null;
		}

		#endregion

		#region Sending

		protected override void SendToSocket(byte[] bytes, bool close, bool partial = false, int id = -1)
		{
			try
			{
				CloseClient = close;
				BlockingMessageQueue.Enqueue(new MessageWrapper(bytes, partial));
			}
			catch (Exception ex)
			{
				RaiseMessageFailed(null, bytes, ex);
			}
		}
		
		protected override void BeginSendFromQueue(MessageWrapper message)
		{
			try
			{
				_mreWriting.WaitOne();
				_mreWriting.Reset();

				_sslStream.BeginWrite(message.Data, 0, message.Data.Length, SendCallback, message);
			}
			catch (Exception ex)
			{
				RaiseMessageFailed(message.State, message.Data, ex);
			}
		}

		protected override void SendCallback(IAsyncResult result)
		{
			var message = (MessageWrapper) result.AsyncState;
			try
			{
				_sslStream.EndWrite(result);
				_mreWriting.Set();
			}
			catch (SocketException se)
			{
				throw new SocketException(se.ErrorCode);
			}
			catch (ObjectDisposedException ode)
			{
				throw new ObjectDisposedException(ode.ObjectName, ode.Message);
			}
			finally
			{
				if (!message.Partial)
					RaiseMessageSubmitted(CloseClient);
				if (!message.Partial && CloseClient)
					Close();

				SentMre.Set();
			}
		}

		#endregion

		#region Receiving

		protected internal override void Receive(IClientMetadata state, int offset = 0)
		{
			try
			{
				state.SslStream = _sslStream;

				if (offset > 0)
				{
					state.UnhandledBytes = state.Buffer;
				}

				if (state.Buffer.Length < state.BufferSize)
				{
					state.ChangeBuffer(new byte[state.BufferSize]);
					if (offset > 0)
						Array.Copy(state.UnhandledBytes, 0, state.Buffer, 0, state.UnhandledBytes.Length);
				}

				_mreRead.WaitOne();
				_mreRead.Reset();
				state.SslStream.BeginRead(state.Buffer, offset, state.BufferSize - offset, ReceiveCallback, state);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		protected override async void ReceiveCallback(IAsyncResult result)
		{
			var state = (ClientMetadata)result.AsyncState;
			try
			{
				var receive = state.SslStream.EndRead(result);
				_mreRead.Set();

				if (state.UnhandledBytes != null && state.UnhandledBytes.Length > 0)
				{
					receive += state.UnhandledBytes.Length;
					state.UnhandledBytes = null;
				}

				if (state.Flag == 0)
				{
					if (state.SimpleMessage == null)
						state.SimpleMessage = new SimpleMessage(state, this, Debug);
					await state.SimpleMessage.ReadBytesAndBuildMessage(receive);
				}
				else if (receive > 0)
					await state.SimpleMessage.ReadBytesAndBuildMessage(receive);

				Receive(state, state.Buffer.Length);
			}
			catch (Exception ex)
			{
				state.Reset();
				DisposeSslStream();
				RaiseErrorThrown(ex);
				Receive(state);
			}
		}

		#endregion


		/// <summary>
		/// Disposes the AsyncSslClient class.
		/// </summary>
		public override void Dispose()
		{
			try
			{
				base.Dispose();
				_mreRead.Dispose();
				_mreWriting.Dispose();
			}
			catch (Exception ex)
			{
				throw new Exception("Error trying to dispose of " + nameof(SimpleSocketTcpSslClient) + " class.", ex);
			}

		}

	}
}
