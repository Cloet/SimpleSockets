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

namespace AsyncClientServer.Server
{
	public sealed class AsyncSocketSslListener : ServerListener
	{
		private readonly X509Certificate _serverCertificate = null;
		private bool _acceptInvalidCertificates = true;
		private readonly ManualResetEvent _mreRead = new ManualResetEvent(true);
		private readonly ManualResetEvent _mreWriting = new ManualResetEvent(true);
		private readonly TlsProtocol _tlsProtocol;

		public bool AcceptInvalidCertificates { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="password"></param>
		/// <param name="tlsProtocol"></param>
		/// <param name="acceptInvalidCertificates"></param>
		public AsyncSocketSslListener(string certificate, string password,TlsProtocol tlsProtocol = TlsProtocol.Tls12, bool acceptInvalidCertificates = true): base()
		{

			if (string.IsNullOrEmpty(certificate))
				throw new ArgumentNullException(nameof(certificate));

			AcceptInvalidCertificates = acceptInvalidCertificates;
			_tlsProtocol = tlsProtocol;

			if (string.IsNullOrEmpty(password))
			{
				_serverCertificate = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(certificate)));
			}
			else
			{
				_serverCertificate = new X509Certificate2(File.ReadAllBytes(Path.GetFullPath(certificate)), password);
			}
		}

		public override void StartListening(string ip, int port, int limit = 500)
		{

			if (string.IsNullOrEmpty(ip))
				throw new ArgumentNullException(nameof(ip));
			if (port < 1)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (limit < 0)
				throw new ArgumentException("Limit cannot be under 0.");
			if (limit == 0)
				throw new ArgumentException("Limit cannot be 0.");


			Port = port;
			Ip = ip;

			var host = Dns.GetHostEntry(ip);
			var ipServer = host.AddressList[0];
			var endpoint = new IPEndPoint(ipServer, port);

			try
			{
				using (var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
				{
					listener.Bind(endpoint);
					listener.Listen(Limit);

					ServerHasStartedInvoke();
					while (true)
					{
						_mre.Reset();
						listener.BeginAccept(OnClientConnect, listener);
						_mre.WaitOne();
					}
				}
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}


		}

		private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicy)
		{
			if (sslPolicy == SslPolicyErrors.None)
				return true;

			return AcceptInvalidCertificates;
		}

		protected override void OnClientConnect(IAsyncResult result)
		{
			_mre.Set();
			try
			{
				IStateObject state;
				int id;

				lock (_clients)
				{
					id = !_clients.Any() ? 1 : _clients.Keys.Max() + 1;
					
					state = new StateObject.StateObject(((Socket)result.AsyncState).EndAccept(result), id);
				}

				var stream = new NetworkStream(state.Listener);

				if (_acceptInvalidCertificates)
				{
					state.SslStream = new SslStream(stream, false,new RemoteCertificateValidationCallback(AcceptCertificate));
				}
				else
				{
					state.SslStream = new SslStream(stream, false);
				}


				Task.Run(() =>
				{
					var success = Authenticate(state).Result;

					if (success)
					{
						_clients.Add(id, state);
						ClientConnectedInvoke(id, state);
						StartReceiving(state);
					}
					else
					{
						throw new AuthenticationException("Unable to authenticate server.");
					}

				}, new CancellationTokenSource(10000).Token);


			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		private async Task<bool> Authenticate(IStateObject state)
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

				await state.SslStream.AuthenticateAsServerAsync(_serverCertificate, true, protocol, false);

				if (!state.SslStream.IsEncrypted)
				{
					throw new Exception("Stream from client " + state.Id + " is not encrypted.");
				}

				if (!state.SslStream.IsAuthenticated)
				{
					throw new Exception("Stream from client " + state.Id + " not authenticated.");
				}

				return true;
			}
			catch (IOException ex)
			{
				switch (ex.Message)
				{
					case "Authentication failed because the remote party has closed the transport stream.":
					case "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.":
						throw new IOException("IOException " + state.Id + " closed the connection.");
					case "The handshake failed due to an unexpected packet format.":
						throw new IOException("IOException " + state.Id + " disconnected, invalid handshake.");
					default:
						throw new IOException(ex.Message, ex);
				}

			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Start receiving
		internal override void StartReceiving(IStateObject state, int offset = 0)
		{
			try
			{
				if (state.Buffer.Length < state.BufferSize && offset == 0)
				{
					state.ChangeBuffer(new byte[state.BufferSize]);
				}

				SslStream sslStream = state.SslStream;

				_mreRead.WaitOne();
				_mreRead.Reset();
				sslStream.BeginRead(state.Buffer, offset, state.BufferSize - offset, ReceiveCallback, state);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}

		}

		//Handles messages
		protected override void HandleMessage(IAsyncResult result)
		{

			var state = (StateObject.StateObject)result.AsyncState;
			try
			{


				//Check if client is still connected.
				//If client is disconnected, send disconnected message
				//and remove from clients list
				if (!IsConnected(state.Id))
				{
					ClientDisconnectedInvoke(state.Id);
					lock (_clients)
					{
						_clients.Remove(state.Id);
					}
				}
				//Else start receiving and handle the message.
				else
				{

					var receive = state.SslStream.EndRead(result);

					_mreRead.Set();
					//var receive = state.Listener.EndReceive(result);

					if (state.Flag == 0)
					{
						state.CurrentState = new InitialHandlerState(state, null, this);
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

					//sslstream has inconsistent buffers
					StartReceiving(state);
				}
			}
			catch (Exception ex)
			{
				state.Reset();
				throw new Exception(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Send data to client
		/// <para>Method used to send bytes to client. Easier to use methods in <see cref="SendToClient"/></para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="bytes"></param>
		/// <param name="close"></param>
		protected override void SendBytes(int id, byte[] bytes, bool close)
		{
			var state = GetClient(id);

			if (state == null)
			{
				throw new Exception("Client does not exist.");
			}

			if (!IsConnected(state.Id))
			{
				//Sets client with id to disconnected
				ClientDisconnectedInvoke(state.Id);
				Close(state.Id);
				throw new Exception("Destination socket is not connected.");
			}

			try
			{
				var send = bytes;

				state.Close = close;

				_mreWriting.WaitOne();

				_mreWriting.Reset();
				state.SslStream.BeginWrite(send, 0, send.Length, SendCallback, state);
			}
			catch (SocketException se)
			{
				throw new SocketException(se.ErrorCode);
			}
			catch (ArgumentException ae)
			{
				throw new ArgumentException(ae.Message, ae);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//End the send and invoke MessageSubmitted event.
		protected override void SendCallback(IAsyncResult result)
		{
			var state = (IStateObject)result.AsyncState;

			try
			{
				state.SslStream.EndWrite(result);
				_mreWriting.Set();
				if (state.Close)
					Close(state.Id);
			}
			catch (SocketException se)
			{
				throw new SocketException(se.ErrorCode);
			}
			catch (ObjectDisposedException ode)
			{
				throw new ObjectDisposedException(ode.ObjectName, ode.Message);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
			finally
			{
				InvokeMessageSubmitted(state.Id, state.Close);
			}
		}


		protected override void SendBytesOfFile(byte[] bytes, int id)
		{
			var state = GetClient(id);

			if (state == null)
			{
				throw new Exception("Client does not exist.");
			}

			if (!IsConnected(state.Id))
			{
				//Sets client with id to disconnected
				ClientDisconnectedInvoke(state.Id);
				throw new Exception("Destination socket is not connected.");
			}

			try
			{
				var send = bytes;
				_mreWriting.WaitOne();

				_mreWriting.Reset();
				state.SslStream.BeginWrite(send, 0, send.Length, FileTransferPartialCallback, state);
			}
			catch (SocketException se)
			{
				throw new SocketException(se.ErrorCode);
			}
			catch (ArgumentException ae)
			{
				throw new ArgumentException(ae.Message, ae);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//End the send and invoke MessageSubmitted event.
		protected override void FileTransferPartialCallback(IAsyncResult result)
		{
			var state = (IStateObject)result.AsyncState;

			try
			{
				state.SslStream.EndWrite(result);
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
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}


	}
}
