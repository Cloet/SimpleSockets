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
using AsyncClientServer.StateObject;
using AsyncClientServer.StateObject.StateObjectState;

namespace AsyncClientServer.Client
{
	public class AsyncSslClient: TcpClient
	{

		private SslStream _sslStream;
		private readonly X509Certificate _sslCertificate;
		private X509Certificate2Collection _sslCertificateCollection;
		private readonly ManualResetEvent _mreRead = new ManualResetEvent(true);
		private readonly ManualResetEvent _mreWriting = new ManualResetEvent(true);
		public bool AcceptInvalidCertificates { get; set; }
		private bool _mutualAuth = false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="password"></param>
		/// <param name="acceptInvalidCertificates"></param>
		public AsyncSslClient(string certificate, string password, bool acceptInvalidCertificates = true) : base()
		{

			if (string.IsNullOrEmpty(certificate))
				throw new ArgumentNullException(nameof(certificate));

			if (string.IsNullOrEmpty(password))
			{
				_sslCertificate = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(certificate)));
			}
			else
			{
				_sslCertificate = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(certificate)), password);
			}

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
			if (port < 1)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (reconnectInSeconds < 3)
				throw new ArgumentOutOfRangeException(nameof(reconnectInSeconds));

			_sslCertificateCollection = new X509Certificate2Collection { _sslCertificate };

			IpServer = ipServer;
			Port = port;
			ReconnectInSeconds = reconnectInSeconds;
			_keepAliveTimer.Enabled = false;

			var host = Dns.GetHostEntry(ipServer);
			var ip = host.AddressList[0];
			_endpoint = new IPEndPoint(ip, port);


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
				throw new Exception(ex.ToString());
			}
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
				await sslStream.AuthenticateAsClientAsync(IpServer, _sslCertificateCollection, SslProtocols.Tls11, false);

				if (!sslStream.IsEncrypted)
				{
					throw new Exception("Stream from server is not encrypted.");
				}

				if (!sslStream.IsAuthenticated)
				{
					throw new Exception("Stream from server not authenticated.");
				}

				if (_mutualAuth && !sslStream.IsMutuallyAuthenticated)
				{
					throw new Exception("Stream from client server failed mutual authentication.");
				}

				return true;
			}
			catch (IOException ex)
			{
				switch (ex.Message)
				{
					case "Authentication failed because the remote party has closed the transport stream.":
					case "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.":
						throw new IOException("IOException server closed the connection.");
					case "The handshake failed due to an unexpected packet format.":
						throw new IOException("IOException server disconnected, invalid handshake.");
					default:
						throw new IOException(ex.Message, ex);
				}

			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
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
					Task<bool> success = Authenticate(_sslStream);
					if (success.Result)
					{
						_connected.Set();
						_keepAliveTimer.Enabled = true;
						Receive();
					}
				});


			}
			catch (SocketException)
			{
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

				if (!this.IsConnected())
				{
					throw new Exception("Destination socket is not connected.");
				}
				else
				{
					var send = bytes;

					_close = close;


					_mreWriting.WaitOne();

					_mreWriting.Reset();
					_sslStream.BeginWrite(send, 0, send.Length, SendCallback, _sslStream);
					//_listener.BeginSend(send, 0, send.Length, SocketFlags.None, SendCallback, _listener);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Send message and invokes MessageSubmitted.
		protected override void SendCallback(IAsyncResult result)
		{
			try
			{
				var receiver = (SslStream)result.AsyncState;
				receiver.EndWrite(result);
				receiver.Flush();
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

			InvokeMessageSubmitted(_close);

			_sent.Set();
		}


		//Start receiving
		public override void StartReceiving(IStateObject state, int offset = 0)
		{

			state.sslStream = _sslStream;
			if (state.Buffer.Length < state.BufferSize && offset == 0)
			{
				state.ChangeBuffer(new byte[state.BufferSize]);
			}

			_mreRead.WaitOne();

			_mreRead.Reset();
			state.sslStream.BeginRead(state.Buffer, offset, state.BufferSize - offset, ReceiveCallback, state);

		}


		//Handle a message
		protected override void HandleMessage(IAsyncResult result)
		{
			var state = (StateObject.StateObject)result.AsyncState;
			try
			{

				var receive = state.sslStream.EndRead(result);
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
				throw new Exception(ex.ToString());
			}
		}


		//Sends bytes of file
		protected override void SendBytesOfFile(byte[] bytes, int id)
		{
			try
			{

				if (!this.IsConnected())
				{
					throw new Exception("Destination socket is not connected.");
				}
				else
				{
					var send = bytes;

					_mreWriting.WaitOne();

					_mreWriting.Reset();
					_sslStream.BeginWrite(send, 0, send.Length, FileTransferPartialCallBack, _sslStream);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Gets called when file is done sending
		protected override void FileTransferPartialCallBack(IAsyncResult result)
		{
			try
			{
				var receiver = (SslStream)result.AsyncState;
				receiver.EndWrite(result);
				receiver.Flush();
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


	}
}
