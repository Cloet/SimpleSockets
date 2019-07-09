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
using AsyncClientServer.Messaging;
using AsyncClientServer.Messaging.Handlers;
using AsyncClientServer.Messaging.Metadata;

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

		/// <summary>
		/// Start listening on specified port and ip.
		/// <para/>The limit is the maximum amount of client which can connect at one moment. You can just fill in 'null' or "" as the ip value.
		/// That way it will automatically choose an ip to listen to. Using IPAddress.Any.
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public override void StartListening(string ip, int port, int limit = 500)
		{

			if (port < 1)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (limit < 0)
				throw new ArgumentException("Limit cannot be under 0.");
			if (limit == 0)
				throw new ArgumentException("Limit cannot be 0.");


			Port = port;
			Ip = ip;

			var endpoint = new IPEndPoint(DetermineListenerIp(ip), port);

			TokenSource = new CancellationTokenSource();
			Token = TokenSource.Token;

			Task.Run(() => SendFromQueue(), Token);

			Task.Run(() =>
			{
				try
				{
					using (var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
					{
						Listener = listener;
						listener.Bind(endpoint);
						listener.Listen(Limit);

						ServerHasStartedInvoke();
						while (!Token.IsCancellationRequested)
						{
							CanAcceptConnections.Reset();
							listener.BeginAccept(OnClientConnect, listener);
							CanAcceptConnections.WaitOne();
						}
					}
				}
				catch (SocketException se)
				{
					throw new Exception(se.ToString());
				}
			}, Token);

		}

		protected override void OnClientConnect(IAsyncResult result)
		{
			CanAcceptConnections.Set();
			try
			{
				ISocketState state;
				int id;

				lock (ConnectedClients)
				{
					id = !ConnectedClients.Any() ? 1 : ConnectedClients.Keys.Max() + 1;

					state = new SocketState(((Socket)result.AsyncState).EndAccept(result), id);
				}

				//If the server shouldn't accept the IP do nothing.
				if (!IsConnectionAllowed(state))
					return;

				var stream = new NetworkStream(state.Listener);

				//Create SslStream
				state.SslStream = new SslStream(stream, false,new RemoteCertificateValidationCallback(AcceptCertificate));

				Task.Run(() =>
				{
					var success = Authenticate(state).Result;

					if (success)
					{
						lock (ConnectedClients)
						{
							ConnectedClients.Add(id, state);
						}
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

		#region SSL Auth

		private bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicy)
		{
			if (sslPolicy == SslPolicyErrors.None)
				return true;

			return AcceptInvalidCertificates;
		}

		private async Task<bool> Authenticate(ISocketState state)
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
				throw new IOException(ex.Message, ex);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		#endregion

		#region Receiving

		//Start receiving
		internal override void StartReceiving(ISocketState state, int offset = 0)
		{
			try
			{
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

			var state = (SocketState)result.AsyncState;
			try
			{


				//Check if client is still connected.
				//If client is disconnected, send disconnected message
				//and remove from clients list
				if (!IsConnected(state.Id))
				{
					ClientDisconnectedInvoke(state.Id);
					lock (ConnectedClients)
					{
						ConnectedClients.Remove(state.Id);
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
				InvokeErrorThrown(ex.Message);
				StartReceiving(state);
			}
		}

		#endregion

		#region Message Sending

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

			try
			{
				if (state == null)
				{
					throw new Exception("Client does not exist.");
				}

				if (!IsConnected(state.Id))
				{
					//Sets client with id to disconnected
					ClientDisconnectedInvoke(state.Id);
					Close(state.Id);
					throw new Exception("Message failed to send because the destination socket is not connected.");
				}

				state.Close = close;
				BlockingMessageQueue.Enqueue(new Message(bytes, MessageType.Complete, state));
			}
			catch (Exception ex)
			{
				InvokeMessageFailed(id, bytes, ex.Message);
			}
		}

		//Send partial message
		protected override void SendBytesPartial(byte[] bytes, int id)
		{
			var state = GetClient(id);

			try
			{
				if (state == null)
				{
					throw new Exception("Client does not exist.");
				}

				if (!IsConnected(state.Id))
				{
					//Sets client with id to disconnected
					ClientDisconnectedInvoke(state.Id);
					Close(state.Id);
					InvokeMessageFailed(id, bytes, "Message failed to send because the destination socket is not connected.");
				}

				BlockingMessageQueue.Enqueue(new Message(bytes, MessageType.Partial, state));

			}
			catch (Exception ex)
			{
				InvokeMessageFailed(id, bytes, ex.Message);
			}

		}

		protected override void BeginSendFromQueue(Message message)
		{
			try
			{
				_mreWriting.WaitOne();
				_mreWriting.Reset();

				if (message.MessageType == MessageType.Partial)
					message.SocketState.SslStream.BeginWrite(message.MessageBytes, 0, message.MessageBytes.Length, SendCallbackPartial, message.SocketState);
				if (message.MessageType == MessageType.Complete)
					message.SocketState.SslStream.BeginWrite(message.MessageBytes, 0, message.MessageBytes.Length, SendCallback, message.SocketState);
			}
			catch (Exception ex)
			{
				InvokeMessageFailed(message.SocketState.Id, message.MessageBytes, ex.Message);
			}
		}

		#endregion

		#region Callbacks

		//End the send and invoke MessageSubmitted event.
		protected override void SendCallback(IAsyncResult result)
		{
			var state = (ISocketState)result.AsyncState;

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

		//End the send and invoke MessageSubmitted event.
		protected override void SendCallbackPartial(IAsyncResult result)
		{
			var state = (ISocketState)result.AsyncState;

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

		#endregion


		/// <summary>
		/// Disposes of the AsyncSocketSslListener.
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
				throw new Exception("Error trying to dispose of " + nameof(AsyncSocketSslListener) + " class.", ex);
			}

		}




	}
}
