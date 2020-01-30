using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets.Client
{
	public class SimpleSocketTcpClient: SimpleSocketClient
	{

		private readonly ManualResetEvent _mreReceiving = new ManualResetEvent(true);

		public SimpleSocketTcpClient() : base()
		{

		}

		/// <summary>
		/// Starts the client.
		/// <para>requires server ip, port number and how many seconds the client should wait to try to connect again. Default is 5 seconds</para>
		/// </summary>
		public override void StartClient(string ipServer, int port, int reconnectInSeconds = 5)
		{

			if (Disposed)
				return;

			if (string.IsNullOrEmpty(ipServer))
				throw new ArgumentNullException(nameof(ipServer));
			if (port < 1 || port > 65535)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (reconnectInSeconds < 3)
				throw new ArgumentOutOfRangeException(nameof(reconnectInSeconds));


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
					if (Token.IsCancellationRequested || Disposed)
						return;

					//Try and connect
					Listener = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					Listener.BeginConnect(Endpoint, this.OnConnectCallback, Listener);
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
						Listener.BeginConnect(Endpoint, this.OnConnectCallback, Listener);
					}

				}
				catch (Exception ex)
				{
					throw new Exception(ex.Message, ex);
				}
			}, Token);


		}
		
		protected override void OnConnectCallback(IAsyncResult result)
		{
			if (Disposed)
				return;

			var server = (Socket)result.AsyncState;

			try
			{
				//Client is connected to server and set connected variable
				server.EndConnect(result);
				ConnectedMre.Set();
				KeepAliveTimer.Enabled = true;
				var state = new ClientMetadata(Listener);
				Receive(state);
			}
			catch (ObjectDisposedException ex)
			{
				RaiseErrorThrown(ex);
			}
			catch (SocketException)
			{
				Thread.Sleep(ReconnectInSeconds * 1000);
				if (!Token.IsCancellationRequested && !Disposed)
					Listener.BeginConnect(Endpoint, OnConnectCallback, Listener);
			}
			catch (Exception ex)
			{
				Dispose();
				RaiseErrorThrown(ex);
			}
		}

		#region MESSAGE SENDING

		protected override void BeginSendFromQueue(MessageWrapper message)
		{
			try
			{
				Listener.BeginSend(message.Data, 0, message.Data.Length, SocketFlags.None, SendCallback, message);
			}
			catch (Exception ex)
			{
				RaiseErrorThrown(ex);
			}
		}

		//Send message and invokes MessageSubmitted.
		protected override void SendCallback(IAsyncResult result)
		{
			var message = (MessageWrapper)result.AsyncState;
			try
			{
				Listener.EndSend(result);
			}
			catch (SocketException se)
			{
				RaiseErrorThrown(se);
			}
			catch (ObjectDisposedException se)
			{
				RaiseErrorThrown(se);
			}
			finally
			{
				if (!message.Partial)
					RaiseMessageSubmitted(CloseClient);

				if (!message.Partial && CloseClient && BlockingMessageQueue.Count == 0)
					Close();

				SentMre.Set();
			}
		}

		protected override void SendToSocket(byte[] data, bool close, bool partial = false, int id = -1)
		{
			//If socket has been ordered to close, prevent adding new messages to queue
			if (CloseClient) return;
			CloseClient = close;
			BlockingMessageQueue.Enqueue(new MessageWrapper(data, partial));
		}


		#endregion


		#region Receiving

		protected internal override void Receive(IClientMetadata state, int offset = 0)
		{
			try {

				bool firstRead = true;
				while (!Token.IsCancellationRequested)
				{
					_mreReceiving.WaitOne();
					_mreReceiving.Reset();

					if (!firstRead)
						offset = state.Buffer.Length;

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

					firstRead = false;

					state.Listener.BeginReceive(state.Buffer, offset, state.BufferSize - offset, SocketFlags.None, this.ReceiveCallback, state);
				}
			}
			catch (Exception ex) {
				throw new Exception(ex.Message, ex);
			}

		}

		protected override async void ReceiveCallback(IAsyncResult result)
		{
			// if (!IsConnected())
			// {
			// 	RaiseLog(new Exception("Socket is not connected, can't receive messages."));
			// 	return;
			// }

			var state = (ClientMetadata)result.AsyncState;
			try
			{

				var receive = state.Listener.EndReceive(result);

				// if 0 bytes are received this mostly means the socket has been closed => timeout etc...
				if (receive == 0)
				{
					if (!IsConnected())
					{
						Log("Client has been closed due to timeout.");
						RaiseDisconnected();
						Close();
						return;
					}
				}

				if (receive > 0) {
					if (state.UnhandledBytes != null && state.UnhandledBytes.Length > 0)
					{
						receive += state.UnhandledBytes.Length;
						state.UnhandledBytes = null;
					}

					//Does header check
					if (state.Flag == 0)
					{
						if (state.SimpleMessage == null)
							state.SimpleMessage = new SimpleMessage(state, this, true);
						await state.SimpleMessage.ReadBytesAndBuildMessage(receive);
					}
					else if (receive > 0)
					{
						await state.SimpleMessage.ReadBytesAndBuildMessage(receive);
					}
				}

				_mreReceiving.Set();
				// Receive(state, state.Buffer.Length);
			}
			catch (Exception ex)
			{
				state.Reset();
				RaiseErrorThrown(ex);
				_mreReceiving.Set();
			}
		}
		
		#endregion
		
	}
}
