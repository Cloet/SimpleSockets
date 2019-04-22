using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AsyncClientServer.StateObject;
using AsyncClientServer.StateObject.MessageHandlerState;

namespace AsyncClientServer.Server
{



	/// <summary>
	/// This class is the server, singleton class
	/// <para>Handles sending and receiving data to/from clients</para>
	/// <para/>Extends <see cref="ServerListener"/>
	/// </summary>
	public sealed class AsyncSocketListener : ServerListener
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public AsyncSocketListener() : base()
		{
		}

		/// <summary>
		/// Start listening on specified port and ip.
		/// <para/>The limit is the maximum amount of client which can connect at one moment.
		/// </summary>
		/// <param name="ip">The ip the server will be listening to.</param>
		/// <param name="port">The port on which the server will be running.</param>
		/// <param name="limit">Optional parameter, default value is 500.</param>
		public override void StartListening(string ip, int port, int limit = 500)
		{
			if (string.IsNullOrEmpty(ip))
				throw new ArgumentNullException(nameof(ip));
			if (port < 1 || port > 65535)
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

			Task.Run(() =>
			{
				try
				{
					using (var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
					{
						_listener = listener;
						listener.Bind(endpoint);
						listener.Listen(Limit);

						ServerHasStartedInvoke();
						while (!Token.IsCancellationRequested)
						{
							_mre.Reset();
							listener.BeginAccept(OnClientConnect, listener);
							_mre.WaitOne();
						}

					}
				}
				catch (ObjectDisposedException)
				{
				}
				catch (SocketException se)
				{
					throw new Exception(se.ToString());
				}
			},Token);

		}

		protected override void OnClientConnect(IAsyncResult result)
		{

			if (Token.IsCancellationRequested)
				return;

			_mre.Set();
			try
			{
				ISocketState state;

				lock (_clients)
				{
					var id = !_clients.Any() ? 1 : _clients.Keys.Max() + 1;

					state = new StateObject.SocketState(((Socket) result.AsyncState).EndAccept(result), id);
					_clients.Add(id, state);
					ClientConnectedInvoke(id, state);
				}

				StartReceiving(state);
			}
			catch (ObjectDisposedException)
			{
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}

		}

		//Start receiving
		internal override void StartReceiving(ISocketState state, int offset = 0)
		{

			if (state.Buffer.Length < state.BufferSize && offset == 0)
			{
				state.ChangeBuffer(new byte[state.BufferSize]);
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
					lock (_clients)
					{
						_clients.Remove(state.Id);
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
				throw new Exception("Destination socket is not connected.");
			}

			try
			{
				var send = bytes;

				state.Close = close;
				state.Listener.BeginSend(send, 0, send.Length, SocketFlags.None, SendCallback, state);
			}
			catch (Exception ex)
			{
				InvokeMessageFailed(id,bytes,ex.Message);
			}
		}

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


		//Sends part of a message
		protected override void SendBytesPartial(byte[] bytes, int id)
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
				throw new Exception("Client is not connected.");
			}

			try
			{
				var send = bytes;

				state.Listener.BeginSend(send, 0, send.Length, SocketFlags.None, SendCallbackPartial, state);
			}
			catch (Exception ex)
			{
				InvokeMessageFailed(id, bytes, ex.Message);
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
	}
}
