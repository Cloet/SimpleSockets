using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using AsyncClientServer.Helper;
using AsyncClientServer.Model.StateObjectState;

namespace AsyncClientServer.Model
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
	/// Event that is triggered when the server receives a file
	/// </summary>
	/// <param name="id"></param>
	/// <param name="filepath"></param>
	public delegate void FileFromClientReceivedHandler(int id, string filepath);

	/// <summary>
	/// Event that is triggered when the server has started
	/// </summary>
	public delegate void ServerHasStartedHandler();

	/// <summary>
	/// This class is the server, singleton class
	/// <para>Handles sending and receiving data to/from clients</para>
	/// <para>Extends <see cref="SendToClient"/>, Implements <seealso cref="IAsyncSocketListener"/></para>
	/// </summary>
	public class AsyncSocketListener : SendToClient, IAsyncSocketListener
	{

		private const ushort Limit = 500;
		private readonly ManualResetEvent _mre = new ManualResetEvent(false);
		private readonly IDictionary<int, IStateObject> _clients = new Dictionary<int, IStateObject>();
		private static System.Timers.Timer _keepAliveTimer;

		public event MessageReceivedHandler MessageReceived;
		public event MessageSubmittedHandler MessageSubmitted;
		public event ClientDisconnectedHandler ClientDisconnected;
		public event FileFromClientReceivedHandler FileReceived;
		public event ServerHasStartedHandler ServerHasStarted;

		/// <summary>
		/// Get dictionary of clients
		/// </summary>
		/// <returns></returns>
		public IDictionary<int, IStateObject> GetClients()
		{
			return _clients;
		}

		/// <summary>
		/// Get the port used to start the server
		/// </summary>
		public int Port { get; private set; }

		private AsyncSocketListener()
		{
			_keepAliveTimer = new System.Timers.Timer(60000);
			_keepAliveTimer.Elapsed += KeepAlive;
			_keepAliveTimer.AutoReset = true;
			_keepAliveTimer.Enabled = true;
		}

		/// <summary>
		/// Get the instance of the server
		/// </summary>
		public static AsyncSocketListener Instance { get; } = new AsyncSocketListener();

		public void CheckClient(int id)
		{
			if (!IsConnected(id))
			{
				ClientDisconnected?.Invoke(id);
				_clients.Remove(id);
			}
		}

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

		private void KeepAlive(Object source, ElapsedEventArgs e)
		{
			CheckAllClients();
		}



		/// <summary>
		/// Starts listening on the given port.
		/// </summary>
		/// <param name="port"></param>
		public void StartListening(int port)
		{
			Port = port;

			var host = Dns.GetHostEntry("127.0.0.1");
			var ip = host.AddressList[0];
			var endpoint = new IPEndPoint(ip, port);

			try
			{
				using (var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
				{
					listener.Bind(endpoint);
					listener.Listen(Limit);
					ServerHasStarted?.Invoke();
					while (true)
					{
						this._mre.Reset();
						listener.BeginAccept(this.OnClientConnect, listener);
						this._mre.WaitOne();
					}
				}
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		/* Gets a socket from the clients dictionary by his Id. */
		private IStateObject GetClient(int id)
		{
			IStateObject state;

			return _clients.TryGetValue(id, out state) ? state : null;
		}

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
		public void OnClientConnect(IAsyncResult result)
		{
			_mre.Set();
			try
			{
				IStateObject state;

				lock (_clients)
				{
					var id = !_clients.Any() ? 1 : _clients.Keys.Max() + 1;

					state = new StateObject(((Socket)result.AsyncState).EndAccept(result), id);
					_clients.Add(id, state);
				}
				StartReceiving(state);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		/// <summary>
		/// Handles messages the server receives.
		/// </summary>
		/// <param name="result"></param>
		public void ReceiveCallback(IAsyncResult result)
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

		
		private void StartReceiving(IStateObject state)
		{

			if (state.Buffer.Length < state.BufferSize)
			{
				state.ChangeBuffer(new byte[state.BufferSize]);
			}

			state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None,
				this.ReceiveCallback, state);
		}

		/// <summary>
		/// Invokes filereceived event
		/// </summary>
		/// <param name="id"></param>
		/// <param name="filePath"></param>
		public void InvokeFileReceived(int id, string filePath)
		{
			FileReceived?.Invoke(id, filePath);
		}

		public void InvokeMessageReceived(int id, string header, string text)
		{
			MessageReceived?.Invoke(id, header, text);
		}

		/// <summary>
		/// Invokes message received event
		/// </summary>
		/// <param name="id"></param>
		/// <param name="header"></param>
		/// <param name="text"></param>
		public void InvokeMessage(int id, string header, string text)
		{
			MessageReceived?.Invoke(id, header, text);
		}

		private void HandleMessage(IAsyncResult result)
		{

			try
			{

				var state = (StateObject)result.AsyncState;
				var receive = state.Listener.EndReceive(result);

				if (state.Flag == 0)
				{
					state.CurrentState = new InitialHandlerState(state);
				}

				if (receive > 0)
				{
					state.CurrentState.Receive(receive);
					//CurrentState.Receive(state, receive);
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
				StartReceiving(state);


			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
		}

		/// <summary>
		/// Send data to client
		/// <para>You should not use this method. Use "SendToClient" instead</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="bytes"></param>
		/// <param name="close"></param>
		protected override void SendBytes(int id, Byte[] bytes, bool close)
		{
			var state = this.GetClient(id);

			if (state == null)
			{
				throw new Exception("Client does not exist.");
			}

			if (!this.IsConnected(state.Id))
			{
				//Sets client with id to disconnected
				ClientDisconnected?.Invoke(state.Id);
				throw new Exception("Destination socket is not connected.");
			}

			try
			{
				var send = bytes;

				state.Close = close;
				state.Listener.BeginSend(send, 0, send.Length, SocketFlags.None, this.SendCallback, state);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			catch (ArgumentException ae)
			{
				throw new Exception(ae.ToString());
			}
		}

		private void SendCallback(IAsyncResult result)
		{
			var state = (IStateObject)result.AsyncState;

			try
			{
				state.Listener.EndSend(result);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			catch (ObjectDisposedException ode)
			{
				throw new Exception(ode.ToString());
			}
			finally
			{
				MessageSubmitted?.Invoke(state.Id, state.Close);
			}
		}

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
					_clients.Remove(state.Id);
					ClientDisconnected?.Invoke(state.Id);
				}
			}
		}

		/// <summary>
		/// Properly dispose the class.
		/// </summary>
		public void Dispose()
		{
			try
			{
				foreach (var id in _clients.Keys)
				{
					this.Close(id);
				}

				_mre.Dispose();
			}
			catch (Exception ex)
			{
				return;
				//throw new Exception(ex.ToString());
			}
		}

	}
}
