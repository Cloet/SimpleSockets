using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AsyncClientServer.Messaging;
using AsyncClientServer.Messaging.Handlers;
using AsyncClientServer.Messaging.Metadata;

namespace AsyncClientServer.Server
{


	/// <summary>
	/// This class is the server, singleton class
	/// <para>Handles sending and receiving data to/from clients</para>
	/// <para/>Extends <see cref="ServerListener"/>
	/// </summary>
	public class AsyncSocketListener : ServerListener
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public AsyncSocketListener() : base()
		{
		}

		/// <summary>
		/// Start listening on specified port and ip.
		/// <para/>The limit is the maximum amount of client which can connect at one moment. You can just fill in 'null' or "" as the ip value.
		/// That way it will automatically choose an ip to listen to. Using IPAddress.Any.
		/// </summary>
		/// <param name="ip">The ip the server will be listening to.</param>
		/// <param name="port">The port on which the server will be running.</param>
		/// <param name="limit">Optional parameter, default value is 500.</param>
		public override void StartListening(string ip, int port, int limit = 500)
		{
			if (port < 1 || port > 65535)
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

			Task.Run(SendFromQueue, Token);

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
							listener.BeginAccept(this.OnClientConnect, listener);
							CanAcceptConnections.WaitOne();
						}

					}
				}
				catch (ObjectDisposedException ode)
				{
					InvokeErrorThrown(ode.Message);
				}
				catch (SocketException se)
				{
					throw new Exception(se.ToString());
				}
			}, Token);

		}

		protected override void OnClientConnect(IAsyncResult result)
		{

			if (Token.IsCancellationRequested)
				return;

			CanAcceptConnections.Set();
			try
			{
				ISocketState state;

				lock (ConnectedClients)
				{
					var id = !ConnectedClients.Any() ? 1 : ConnectedClients.Keys.Max() + 1;

					state = new SocketState(((Socket) result.AsyncState).EndAccept(result), id);


					//If the server shouldn't accept the IP do nothing.
					if (!IsConnectionAllowed(state))
						return;

					var client = ConnectedClients.FirstOrDefault(x => x.Value == state);

					if (client.Value == state)
					{
						id = client.Key;
						ConnectedClients.Remove(id);
						ConnectedClients.Add(id, state);
					}
					else
					{
						ConnectedClients.Add(id, state);
					}

					ClientConnectedInvoke(id, state);
				}

				StartReceiving(state);
			}
			catch (ObjectDisposedException ode)
			{
				InvokeErrorThrown(ode.Message);
			}
			catch (SocketException se)
			{
                this.InvokeErrorThrown(se.Message);
			}

		}

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
				ReceiveCallback, state);
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

					var receive = state.Listener.EndReceive(result);

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

		//Sends part of a message
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
				if (message.MessageType == MessageType.Partial)
					message.SocketState.Listener.BeginSend(message.MessageBytes, 0, message.MessageBytes.Length, SocketFlags.None, SendCallbackPartial, message.SocketState);
				if (message.MessageType == MessageType.Complete)
					message.SocketState.Listener.BeginSend(message.MessageBytes, 0, message.MessageBytes.Length, SocketFlags.None, SendCallbackPartial, message.SocketState);
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
				state.Listener.EndSend(result);
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
				state.Listener.EndSend(result);
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

	}
}
