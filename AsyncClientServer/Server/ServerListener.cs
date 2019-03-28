using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AsyncClientServer.StateObject;
using AsyncClientServer.StateObject.StateObjectState;

namespace AsyncClientServer.Server
{

	/// <summary>
	/// Event that is triggered when a message is received
	/// </summary>
	/// <param name="id"></param>
	/// <param name="msg"></param>
	public delegate void MessageReceivedHandler(int id, string header, string msg);

	/// <summary>
	/// Event that is triggered a message is sent to the server
	/// </summary>
	/// <param name="id"></param>
	/// <param name="close"></param>
	public delegate void MessageSubmittedHandler(int id, bool close);

	/// <summary>
	/// Event that is triggered when the client has disconnected
	/// </summary>
	/// <param name="id"></param>
	public delegate void ClientDisconnectedHandler(int id);

	/// <summary>
	/// Event that is triggered when a client has connected;
	/// </summary>
	/// <param name="id"></param>
	public delegate void ClientConnectedHandler(int id);

	/// <summary>
	/// Event that is triggered when the server receives a file
	/// </summary>
	/// <param name="id"></param>
	/// <param name="filepath"></param>
	public delegate void FileFromClientReceivedHandler(int id, string filepath);

	/// <summary>
	/// Event that is triggered when a part of the message is received.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="bytes"></param>
	/// <param name="messageSize"></param>
	public delegate void FileTransferProgressHandler(int id, int bytes, int messageSize);

	/// <summary>
	/// Event that is triggered when the server has started
	/// </summary>
	public delegate void ServerHasStartedHandler();

	public abstract class ServerListener : SendToClient, IServerListener
	{

		protected int Limit = 500;
		protected readonly ManualResetEvent _mre = new ManualResetEvent(false);
		protected readonly IDictionary<int, IStateObject> _clients = new Dictionary<int, IStateObject>();
		private static System.Timers.Timer _keepAliveTimer;

		public bool ServerStarted { get; protected set; }

		//Events
		public event MessageReceivedHandler MessageReceived;
		public event MessageSubmittedHandler MessageSubmitted;
		public event ClientDisconnectedHandler ClientDisconnected;
		public event ClientConnectedHandler ClientConnected;
		public event FileFromClientReceivedHandler FileReceived;
		public event FileTransferProgressHandler ProgressFileReceived;
		public event ServerHasStartedHandler ServerHasStarted;

		/// <summary>
		/// Get dictionary of clients
		/// </summary>
		/// <returns></returns>
		public IDictionary<int, IStateObject> GetClients()
		{
			return _clients;
		}

		/// <inheritdoc />
		/// <summary>
		/// Get the port used to start the server
		/// </summary>
		public int Port { get; protected set; }

		/// <summary>
		/// Get the ip on which the server is running
		/// </summary>
		public string Ip { get; protected set; }

		//Constructor (Singleton pattern)
		protected void Init()
		{
			//Set timer that checks all clients every 5 minutes
			_keepAliveTimer = new System.Timers.Timer(300000);
			_keepAliveTimer.Elapsed += KeepAlive;
			_keepAliveTimer.AutoReset = true;
			_keepAliveTimer.Enabled = true;
		}



		/// <inheritdoc />
		/// <summary>
		/// Check if a client with given id is connected, remove if inactive.
		/// </summary>
		/// <param name="id"></param>
		public void CheckClient(int id)
		{
			if (!IsConnected(id))
			{
				ClientDisconnected?.Invoke(id);
				_clients.Remove(id);
			}
		}

		/// <summary>
		/// Check all clients and show which are disconnected.
		/// </summary>
		public void CheckAllClients()
		{
			lock (_clients)
			{
				if (_clients.Keys.Count > 0)
				{
					foreach (var id in _clients.Keys)
					{
						CheckClient(id);
					}
				}
			}
		}

		//Timer that checks client every x seconds
		private void KeepAlive(Object source, ElapsedEventArgs e)
		{
			CheckAllClients();
		}

		/// <summary>
		/// Starts listening on the given port.
		/// </summary>
		///	<param name="ip"></param> 
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public abstract void StartListening(string ip, int port, int limit = 500);

		/* Gets a socket from the clients dictionary by his Id. */
		private IStateObject GetClient(int id)
		{
			IStateObject state;

			return _clients.TryGetValue(id, out state) ? state : null;
		}

		/// <inheritdoc />
		/// <summary>
		/// returns if a certain client is connected
		/// </summary>
		/// <param name="id"></param>
		/// <returns>bool</returns>
		public bool IsConnected(int id)
		{
			try
			{

				var state = this.GetClient(id);

				return !((state.Listener.Poll(1000, SelectMode.SelectRead) && (state.Listener.Available == 0)) || !state.Listener.Connected);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}

		}


		/// <summary>
		/// Add a socket to the clients dictionary.
		/// Lock clients temporary to handle mulitple access.
		/// ReceiveCallback raise an event, after the message receiving is complete.
		/// </summary>
		/// <param name="result"></param>
		protected abstract void OnClientConnect(IAsyncResult result);

		//Handles messages the server receives
		private void ReceiveCallback(IAsyncResult result)
		{
			try
			{
				HandleMessage(result);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}


		}

		//Start receiving
		protected void StartReceiving(IStateObject state)
		{

			if (state.Buffer.Length < state.BufferSize)
			{
				state.ChangeBuffer(new byte[state.BufferSize]);
			}

			state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None,
				this.ReceiveCallback, state);
		}

		#region Invokes

		protected void ClientConnectedInvoke(int id)
		{
			ClientConnected?.Invoke(id);
		}

		protected void ServerHasStartedInvoke()
		{
			ServerHasStarted?.Invoke();
		}

		/// <inheritdoc />
		/// <summary>
		/// Invokes FileReceived event
		/// </summary>
		/// <param name="id"></param>
		/// <param name="filePath"></param>
		public void InvokeFileReceived(int id, string filePath)
		{
			FileReceived?.Invoke(id, filePath);
		}

		/// <summary>
		/// Invokes ProgressReceived event
		/// </summary>
		/// <param name="id"></param>
		/// <param name="bytesReceived"></param>
		/// <param name="messageSize"></param>
		public void InvokeFileTransferProgress(int id, int bytesReceived, int messageSize)
		{
			ProgressFileReceived?.Invoke(id, bytesReceived, messageSize);
		}

		/// <inheritdoc />
		/// <summary>
		/// Invokes MessageReceived event of the server.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="header"></param>
		/// <param name="text"></param>
		public void InvokeMessageReceived(int id, string header, string text)
		{
			MessageReceived?.Invoke(id, header, text);
		}

		#endregion

		//Handles messages
		private void HandleMessage(IAsyncResult result)
		{

			try
			{

				var state = (StateObject.StateObject)result.AsyncState;

				//Check if client is still connected.
				//If client is disconnected, send disconnected message
				//and remove from clients list
				if (!IsConnected(state.Id))
				{
					ClientDisconnected?.Invoke(state.Id);
					_clients.Remove(state.Id);
				}
				//Else start receiving and handle the message.
				else
				{
					var receive = state.Listener.EndReceive(result);

					if (state.Flag == 0)
					{
						state.CurrentState = new InitialHandlerState(state);
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

					//When something goes wrong
					state.Reset();
					StartReceiving(state);
				}




			}
			catch (Exception ex)
			{
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
				ClientDisconnected?.Invoke(state.Id);
				throw new Exception("Destination socket is not connected.");
			}

			try
			{
				var send = bytes;

				state.Close = close;
				state.Listener.BeginSend(send, 0, send.Length, SocketFlags.None, SendCallback, state);
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

		protected override async Task SendFile(string location, string remoteSaveLocation, bool encrypt,bool close, int id = -1)
		{
			var state = GetClient(id);

			if (state == null)
			{
				throw new Exception("Client does not exist.");
			}

			if (!IsConnected(state.Id))
			{
				//Sets client with id to disconnected
				ClientDisconnected?.Invoke(state.Id);
				throw new Exception("Destination socket is not connected.");
			}

			try
			{
				await BeginSendFile(location, remoteSaveLocation, encrypt, close, FileSendCallback, id);
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

		private void FileSendCallback(bool close,int id)
		{
			try
			{
				if (close)
					Close(id);
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
				MessageSubmitted?.Invoke(id, close);
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
				ClientDisconnected?.Invoke(state.Id);
				throw new Exception("Destination socket is not connected.");
			}

			try
			{
				var send = bytes;

				state.Listener.BeginSend(send, 0, send.Length, SocketFlags.None, SendCallbackFile, state);
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
		private void SendCallback(IAsyncResult result)
		{
			var state = (IStateObject)result.AsyncState;

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
				MessageSubmitted?.Invoke(state.Id, state.Close);
			}
		}

		//End the send and invoke MessageSubmitted event.
		protected void SendCallbackFile(IAsyncResult result)
		{
			var state = (IStateObject)result.AsyncState;

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

		/// <inheritdoc />
		/// <summary>
		/// Close a certain client
		/// </summary>
		/// <param name="id"></param>
		public void Close(int id)
		{
			var state = GetClient(id);

			if (state == null)
			{
				throw new Exception("Client does not exist.");
			}

			try
			{
				state.Listener.Shutdown(SocketShutdown.Both);
				state.Listener.Close();
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			finally
			{
				lock (_clients)
				{
					_clients.Remove(id);
					ClientDisconnected?.Invoke(state.Id);
				}
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Properly dispose the class.
		/// </summary>
		public void Dispose()
		{
			try
			{
				foreach (var id in _clients.Keys)
				{
					Close(id);
				}

				_mre.Dispose();
				GC.SuppressFinalize(this);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}



	}
}
