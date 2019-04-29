using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AsyncClientServer.Messaging.Handlers;
using AsyncClientServer.Messaging.Metadata;

namespace AsyncClientServer.Client
{

	


	/// <summary>
	/// The Following code handles the client in an Async fashion.
	/// <para>To send messages to the corresponding Server, you should use the class "SendToServer"</para>
	/// <para>Extends <see cref="TcpClient"/>
	/// </para>
	/// </summary>
	public sealed class AsyncClient : TcpClient
	{

		public AsyncClient(): base()
		{
		}

		/// <summary>
		/// Starts the client.
		/// <para>requires server ip, port number and how many seconds the client should wait to try to connect again. Default is 5 seconds</para>
		/// </summary>
		public override void StartClient(string ipServer, int port, int reconnectInSeconds = 5)
		{

			if (string.IsNullOrEmpty(ipServer))
				throw new ArgumentNullException(nameof(ipServer));
			if (port < 1 || port > 65535)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (reconnectInSeconds < 3)
				throw new ArgumentOutOfRangeException(nameof(reconnectInSeconds));


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
					if (Token.IsCancellationRequested)
						return;

					//Try and connect
					_listener = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					_listener.BeginConnect(_endpoint, OnConnectCallback, _listener);
					_connected.WaitOne();

					//If client is connected activate connected event
					if (IsConnected())
					{
						InvokeConnected(this);
					}
					else {
						_keepAliveTimer.Enabled = false;
						InvokeDisconnected(this);
						Close();
						_connected.Reset();
						_listener.BeginConnect(_endpoint, OnConnectCallback, _listener);
						
					}

				}
				catch (Exception ex)
				{
					throw new Exception(ex.Message, ex);
				}
			},Token);


		}

		protected override void OnConnectCallback(IAsyncResult result)
		{
			var server = (Socket)result.AsyncState;

			try
			{
				//Client is connected to server and set connected variable
				server.EndConnect(result);
				_connected.Set();
				_keepAliveTimer.Enabled = true;
				Receive();
			}
			catch (SocketException)
			{
				Thread.Sleep(ReconnectInSeconds * 1000);
				if (!Token.IsCancellationRequested)
					_listener.BeginConnect(_endpoint, OnConnectCallback, _listener);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		#region MESSAGE SENDING

		/// <summary>
		/// Sends data to server
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="close"></param>
		protected override void SendBytes(byte[] bytes, bool close)
		{

			try
			{
				if (!IsConnected())
				{
					Close();
					InvokeMessageFailed(bytes, "Server socket is not connected.");
				}
				else
				{
					var send = bytes;

					_close = close;
					_listener.BeginSend(send, 0, send.Length, SocketFlags.None, SendCallback, _listener);
				}
			}
			catch (Exception ex)
			{
				InvokeMessageFailed(bytes, ex.Message);
			}
		}

		//Send message and invokes MessageSubmitted.
		protected override void SendCallback(IAsyncResult result)
		{
			try
			{
				var receiver = (Socket) result.AsyncState;
				receiver.EndSend(result);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			catch (ObjectDisposedException se)
			{
				throw new Exception(se.ToString());
			}
			finally
			{
				InvokeMessageSubmitted(_close);

				if (_close)
					Close();

				_sent.Set();
			}
		}

		#endregion

		#region FILE SENDING

		//Sends bytes of file
		protected override void SendBytesPartial(byte[] bytes, int id)
		{
			try
			{

				if (!IsConnected())
				{
					Close();
					InvokeMessageFailed(bytes, "Server socket is not connected.");
				}
				else
				{
					var send = bytes;

					_listener.BeginSend(send, 0, send.Length, SocketFlags.None, SendCallbackPartial, _listener);
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
				var receiver = (Socket)result.AsyncState;
				receiver.EndSend(result);
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

		#endregion

		#region Receiving

		//Start receiving
		internal override void StartReceiving(ISocketState state, int offset = 0)
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

			state.Listener.BeginReceive(state.Buffer, offset, state.BufferSize - offset, SocketFlags.None,
				this.ReceiveCallback, state);
		}

		//Handle a message
		protected override void HandleMessage(IAsyncResult result)
		{
			var state = (SocketState)result.AsyncState;
			try
			{

				var receive = state.Listener.EndReceive(result);

				if (state.Flag == 0)
				{
					state.CurrentState = new InitialHandlerState(state, this, null);
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
				InvokeErrorThrown(ex.Message);
			}
		}

		#endregion

	}
}
