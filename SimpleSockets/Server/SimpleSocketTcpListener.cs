using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets.Server
{
	public class SimpleSocketTcpListener: SimpleSocketListener
	{
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

						RaiseServerHasStarted();
						while (!Token.IsCancellationRequested)
						{
							CanAcceptConnections.Reset();
							listener.BeginAccept(OnClientConnect, listener);
							CanAcceptConnections.WaitOne();
						}

					}
				}
				catch (ObjectDisposedException ode)
				{
					RaiseErrorThrown(ode);
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
				IClientMetadata state;

				lock (ConnectedClients)
				{
					var id = !ConnectedClients.Any() ? 1 : ConnectedClients.Keys.Max() + 1;

					state = new ClientMetadata(((Socket)result.AsyncState).EndAccept(result), id);

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

					RaiseClientConnected(state);
				}
				Receive(state);
			}
			catch (ObjectDisposedException ode)
			{
				RaiseErrorThrown(ode);
			}
			catch (SocketException se)
			{
				RaiseErrorThrown(se);
			}

		}

		#region Receiving

		protected internal override void Receive(IClientMetadata state, int offset = 0)
		{
			try {
				var firstRead = true;

				while (!Token.IsCancellationRequested)
				{
					state.MreTimeout.Reset();
					state.MreReceiving.WaitOne();
					state.MreReceiving.Reset();

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

					state.Listener.BeginReceive(state.Buffer, offset, state.BufferSize - offset, SocketFlags.None, ReceiveCallback, state);
					if (!state.MreTimeout.WaitOne(Timeout, false))
					{
						throw new SocketException((int)SocketError.TimedOut);
					}
				}
			}
			catch (SocketException se)
			{
				if (se.SocketErrorCode == SocketError.TimedOut)
				{
					Log("Socket has timed out.");
					RaiseClientTimedOut(state);
				}
				else
					RaiseErrorThrown(se);
			}
			catch (Exception ex)
			{
				Log("Error trying to receive from client with id:" + state.Id + " and Guid: " + state.Guid);
				RaiseErrorThrown(ex);
			}
			finally
			{
				Log("Closing socket from client with id:" + state.Id + " and Guid: " + state.Guid);
				Close(state.Id);
				Log("Socket from client with id:" + state.Id + " and Guid: " + state.Guid + " has been closed.");
			}
		}

		protected override async void ReceiveCallback(IAsyncResult result)
		{
			var state = (ClientMetadata)result.AsyncState;
			state.MreTimeout.Set();
			try
			{
				if (state.Listener == null)
					return;

				//Check if client is still connected.
				//If client is disconnected, send disconnected message
				//and remove from clients list
				if (!IsConnected(state.Id))
				{
					RaiseClientDisconnected(state);
					lock (ConnectedClients)
					{
						ConnectedClients.Remove(state.Id);
					}
				}
				//Else start receiving and handle the message.
				else
				{
					var receive = state.Listener.EndReceive(result);

					if (state.UnhandledBytes != null && state.UnhandledBytes.Length > 0)
					{
						receive += state.UnhandledBytes.Length;
						state.UnhandledBytes = null;
					}

					//Does header check
					if (state.Flag == 0)
					{
						if (state.SimpleMessage == null)
							state.SimpleMessage = new SimpleMessage(state, this, Debug);
						await ParallelQueue.Enqueue(() => state.SimpleMessage.ReadBytesAndBuildMessage(receive));
					}else if (receive > 0)
					{
						await ParallelQueue.Enqueue(() => state.SimpleMessage.ReadBytesAndBuildMessage(receive));
					}

					state.MreReceiving.Set();
				}
			}
			catch (Exception ex)
			{
				state.Reset();
				RaiseErrorThrown(ex);
				RaiseLog(ex);
				RaiseLog("Error handling message from client with guid : " + state.Guid + ".");
				state.MreReceiving.Set();
				// Receive(state, state.Buffer.Length);
			}
		}

		#endregion

		#region Message Sending

		protected override void BeginSendFromQueue(MessageWrapper message)
		{
			try
			{
				message.State.Listener.BeginSend(message.Data, 0, message.Data.Length, SocketFlags.None, SendCallback, message);
			}
			catch (Exception ex)
			{
				RaiseMessageFailed(message.State, message.Data, ex);
			}
		}

		/// <summary>
		/// Send bytes to client.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="close"></param>
		/// <param name="partial"></param>
		/// <param name="id"></param>
		protected override void SendToSocket(byte[] data, bool close,bool partial = false, int id = -1)
		{
			var state = GetClient(id);
			try
			{
				if (state != null)
				{
					if (!IsConnected(state.Id))
					{
						//Sets client with id to disconnected
						RaiseClientDisconnected(state);
						throw new Exception("Message failed to send because the destination socket is not connected.");
					}
					else
					{
						state.Close = close;
						BlockingMessageQueue.Enqueue(new MessageWrapper(data, state, partial));
					}
				}
			}
			catch (Exception ex)
			{
				RaiseMessageFailed(state, data, ex);
			}
		}

		//End the send and invoke MessageSubmitted event.
		protected override void SendCallback(IAsyncResult result)
		{
			var message = (MessageWrapper)result.AsyncState;
			var state = message.State;

			try
			{
				state.Listener.EndSend(result);
				if (!message.Partial && state.Close)
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
				if (!message.Partial)
					RaiseMessageSubmitted(state, state.Close);
				message.Dispose();
			}
		}

		#endregion




	}
}
