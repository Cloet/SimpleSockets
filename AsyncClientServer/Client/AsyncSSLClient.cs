using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using AsyncClientServer.Messaging.Handlers;
using AsyncClientServer.Messaging.Metadata;

namespace AsyncClientServer.Client
{
	public sealed class AsyncSslClient: TcpClient
	{

		private SslStream _sslStream;
		private readonly X509Certificate _sslCertificate;
		private X509Certificate2Collection _sslCertificateCollection;
		private readonly ManualResetEvent _mreRead = new ManualResetEvent(true);
		private readonly ManualResetEvent _mreWriting = new ManualResetEvent(true);
		public bool AcceptInvalidCertificates { get; set; }
		private readonly TlsProtocol _tlsProtocol;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="certificatePassword"></param>
		/// <param name="tls"></param>
		/// <param name="acceptInvalidCertificates"></param>
		public AsyncSslClient(string certificate, string certificatePassword,TlsProtocol tls = TlsProtocol.Tls12,bool acceptInvalidCertificates = true) : base()
		{

			if (string.IsNullOrEmpty(certificate))
				throw new ArgumentNullException(nameof(certificate));

			if (string.IsNullOrEmpty(certificatePassword))
			{
				_sslCertificate = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(certificate)));
			}
			else
			{
				_sslCertificate = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(certificate)), certificatePassword);
			}


			_tlsProtocol = tls;
			AcceptInvalidCertificates = acceptInvalidCertificates;

		}

		/// <summary>
		/// Start listening to the server
		/// </summary>
		/// <param name="ipServer"></param>
		/// <param name="port"></param>
		/// <param name="reconnectInSeconds"></param>
		public override void StartClient(string ipServer, int port, int reconnectInSeconds = 5)
		{
			if (string.IsNullOrEmpty(ipServer))
				throw new ArgumentNullException(nameof(ipServer));
			if (port < 1 || port > 65535)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (reconnectInSeconds < 3)
				throw new ArgumentOutOfRangeException(nameof(reconnectInSeconds));

			_sslCertificateCollection = new X509Certificate2Collection { _sslCertificate };

			IpServer = ipServer;
			Port = port;
			ReconnectInSeconds = reconnectInSeconds;
			_keepAliveTimer.Enabled = false;

			_endpoint = new IPEndPoint(GetIp(ipServer), port);

			TokenSource = new CancellationTokenSource();
			Token = TokenSource.Token;


			Task.Run(() =>
			{
				try
				{
					//Try and connect
					_listener = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					_listener.BeginConnect(_endpoint, this.OnConnectCallback, _listener);
					_connected.WaitOne();

					//If client is connected activate connected event
					if (IsConnected())
					{
						InvokeConnected(this);
					}
					else
					{
						_keepAliveTimer.Enabled = false;
						InvokeDisconnected(this);
						Close();
						_connected.Reset();
						_listener.BeginConnect(_endpoint, OnConnectCallback, _listener);
					}

				}
				catch (Exception ex)
				{
					InvokeErrorThrown(ex.Message);
				}
			},Token);

		}

		//Validates the certificate
		private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicy)
		{
			if (sslPolicy == SslPolicyErrors.None)
				return true;

			return AcceptInvalidCertificates;
		}

		private async Task<bool> Authenticate(SslStream sslStream)
		{
			try
			{
				SslProtocols protocol = SslProtocols.Tls12;

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
				}


				await sslStream.AuthenticateAsClientAsync(IpServer, _sslCertificateCollection, protocol, false);

				if (!sslStream.IsEncrypted)
				{
					throw new Exception("Stream from server is not encrypted.");
				}

				if (!sslStream.IsAuthenticated)
				{
					throw new Exception("Stream from server not authenticated.");
				}

				return true;
			}
			catch (IOException ex)
			{
				InvokeErrorThrown(ex.Message);
				return false;
			}
			catch (Exception ex)
			{
				InvokeErrorThrown(ex.Message);
				return false;
			}


		}

		protected override void OnConnectCallback(IAsyncResult result)
		{
			var client = (Socket)result.AsyncState;

			try
			{
				//Client is connected to server and set connected variable
				client.EndConnect(result);

				var stream = new NetworkStream(_listener);
				_sslStream = new SslStream(stream, false, new RemoteCertificateValidationCallback(ValidateCertificate),null);


				Task.Run(() =>
				{
					bool success = Authenticate(_sslStream).Result;

					
					if (success)
					{
							_connected.Set();
							_keepAliveTimer.Enabled = true;
							Receive();
					}
					else
					{
						throw new AuthenticationException("Client cannot be authenticated.");
					}

				}, new CancellationTokenSource(10000).Token);


			}
			catch (SocketException)
			{
				DisposeSslStream();
				
				Thread.Sleep(ReconnectInSeconds * 1000);
				_listener.BeginConnect(_endpoint, this.OnConnectCallback, _listener);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}

		}

		/// <summary>
		/// Sends data to server
		/// <para>This method should not be used,instead use methods in <see cref="SendToServer"/></para>
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="close"></param>
		protected override void SendBytes(byte[] bytes, bool close)
		{

			try
			{

				if (!IsConnected())
				{
					DisposeSslStream();
					InvokeDisconnected(this);
					Close();
					InvokeMessageFailed(bytes, "Server socket is not connected.");
				}
				else { 

					var send = bytes;

					_close = close;

					//Waits tille one write is complete before starting another one.
					_mreWriting.WaitOne();
					_mreWriting.Reset();

					_sslStream.BeginWrite(send, 0, send.Length, SendCallback, _sslStream);
				}
			}
			catch (Exception ex)
			{
				InvokeMessageFailed(bytes,ex.Message);
			}
		}

		//Send message and invokes MessageSubmitted.
		protected override void SendCallback(IAsyncResult result)
		{
			try
			{
				var receiver = (SslStream) result.AsyncState;
				receiver.EndWrite(result);
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
				InvokeMessageSubmitted(_close);

				if (_close)
					Close();

				_sent.Set();
			}


		}


		//Start receiving
		internal override void StartReceiving(ISocketState state, int offset = 0)
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


		//Handle a message
		protected override void HandleMessage(IAsyncResult result)
		{
			var state = (SocketState)result.AsyncState;
			try
			{
				var receive = state.SslStream.EndRead(result);
				_mreRead.Set();

				if (state.Flag == 0)
				{
					state.CurrentState = new InitialHandlerState(state, this,null);
				}

				if (receive > 0)
				{
					state.CurrentState.Receive(receive);
				}

				/*When the full message has been received. */
				if (state.Read == state.MessageSize)
				{
					StartReceiving(state);
					return;
				}

				/*Check if there still are messages to be received.*/
				if (receive == state.BufferSize)
				{
					StartReceiving(state);
					return;
				}

				StartReceiving(state);
			}
			catch (Exception ex)
			{
				state.Reset();
				DisposeSslStream();
				_listener.BeginConnect(_endpoint, OnConnectCallback, _listener);
				InvokeErrorThrown(ex.Message);
			}
		}


		//Sends bytes of file
		protected override void SendBytesPartial(byte[] bytes, int id)
		{
			try
			{

				if (!IsConnected())
				{
					DisposeSslStream();
					Close();
					InvokeMessageFailed(bytes, "Server socket is not connected.");
				}
				else
				{
					var send = bytes;

					_mreWriting.WaitOne();
					_mreWriting.Reset();
					_sslStream.BeginWrite(send, 0, send.Length, SendCallbackPartial, _sslStream);
				}
			}
			catch (Exception ex)
			{
				InvokeMessageFailed(bytes, ex.Message);
			}
		}

		//Gets called when file is done sending
		protected override void SendCallbackPartial(IAsyncResult result)
		{
			try
			{
				var receiver = (SslStream)result.AsyncState;
				receiver.EndWrite(result);
				_mreWriting.Set();
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			catch (ObjectDisposedException se)
			{
				throw new Exception(se.ToString());
			}
		}

		private void DisposeSslStream()
		{
			if (_sslStream != null)
			{
				_sslStream.Dispose();
				_sslStream = null;
			}
		}

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
				throw new Exception("Error trying to dispose of " + nameof(AsyncSslClient) + " class.", ex);
			}

		}

	}
}
