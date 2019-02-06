using System;
using System.CodeDom;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using AsyncClientServer.Helper;
using AsyncClientServer.Model.StateObjectState;

namespace AsyncClientServer.Model
{

	/// <summary>
	/// Event that triggers when a client is connected to server
	/// </summary>
	/// <param name="a"></param>
	public delegate void ConnectedHandler(IAsyncClient a);

	/// <summary>
	/// Event that is triggered when the client has disconnected from the server.
	/// </summary>
	public delegate void DisconnectedFromServerHandler(string ipServer, int port);

	/// <summary>
	/// Event that triggers when client receives a message
	/// </summary>
	/// <param name="a"></param>
	/// <param name="msg"></param>
	public delegate void ClientMessageReceivedHandler(IAsyncClient a, string header, string msg);
	/// <summary>
	/// Event that triggers when client sends a message
	/// </summary>
	/// <param name="a"></param>
	/// <param name="close"></param>
	public delegate void ClientMessageSubmittedHandler(IAsyncClient a, bool close);

	/// <summary>
	/// Event that is triggered when a file is received from the server, returns the new file path
	/// </summary>
	/// <param name="path"></param>
	public delegate void FileFromServerReceivedHandler(IAsyncClient a, string path);


	/// <summary>
	/// The Following code handles the client in an Async fashion.
	/// <para>To send messages to the corresponding Server, you should use the class "SendToServer"</para>
	/// <para>Extends <see cref="SendToServer"/>, Implements
	/// <seealso cref="IAsyncClient"/>
	/// </para>
	/// </summary>
	public class AsyncClient : SendToServer, IAsyncClient
	{
		private readonly string[] _messageTypes = { "FILETRANSFER", "COMMAND", "MESSAGE", "OBJECT" };
		private Socket _listener;
		private bool _close;
		private readonly ManualResetEvent _connected = new ManualResetEvent(false);
		private readonly ManualResetEvent _sent = new ManualResetEvent(false);
		private IPEndPoint _endpoint;
		private static System.Timers.Timer _keepAliveTimer;

		/// <summary>
		/// The port of the server
		/// </summary>
		public int Port { get; private set; }

		/// <summary>
		/// The ip of the server
		/// </summary>
		public string IpServer { get; private set; }

		/// <summary>
		/// This is how many seconds te client waits to try and reconnect to the server
		/// </summary>
		public int ReconnectInSeconds { get; private set; }


		/// <summary>
		/// This event is used to check if the client is connected
		/// </summary>
		public event ConnectedHandler Connected;
		/// <summary>
		/// This event is used to check if the client received a message
		/// </summary>
		public event ClientMessageReceivedHandler MessageReceived;
		/// <summary>
		/// This event is used to check if the client sends a message
		/// </summary>
		public event ClientMessageSubmittedHandler MessageSubmitted;

		/// <summary>
		/// Event that is used to check when a file is received from the server
		/// </summary>
		public event FileFromServerReceivedHandler FileReceived;

		/// <summary>
		/// Event that is used to check if the client is still connected to the server.
		/// </summary>
		public event DisconnectedFromServerHandler Disconnected;

		/// <summary>
		/// Constructor
		/// Use StartClient() to start a connection to a server.
		/// </summary>
		public AsyncClient()
		{
			_keepAliveTimer = new System.Timers.Timer(15000);
			_keepAliveTimer.Elapsed += KeepAlive;
			_keepAliveTimer.AutoReset = true;
			_keepAliveTimer.Enabled = false;
		}


		private void KeepAlive(Object source, ElapsedEventArgs e)
		{
			if (!IsConnected())
			{
				Disconnected?.Invoke(this.IpServer, this.Port);
				this.Close();
				_connected.Reset();
				StartClient(IpServer, Port, ReconnectInSeconds);
			}
		}

		/// <summary>
		/// Starts the client.
		/// <para>requires server ip, port number and how many seconds the client should wait to try to connect again. Default is 5 seconds</para>
		/// </summary>
		public void StartClient(string ipServer, int port, int reconnectInSeconds)
		{
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
					this.Connected?.Invoke(this);
				}
				else
				{
					_keepAliveTimer.Enabled = false;
					Disconnected?.Invoke(this.IpServer, this.Port);
					this.Close();
					_connected.Reset();
					_listener.BeginConnect(_endpoint, this.OnConnectCallback, _listener);
				}

			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
		}

		/// <summary>
		/// Method starts the client requires ip of server and port number.
		/// <para>Loops connect every 5 seconds.</para>
		/// </summary>
		/// <param name="ipServer"></param>
		/// <param name="port"></param>
		public void StartClient(string ipServer, int port)
		{
			StartClient(ipServer, port, 5);
		}

		/// <summary>
		/// Check if client is connected to server
		/// </summary>
		/// <returns>bool</returns>
		public bool IsConnected()
		{
			try
			{
				return !((_listener.Poll(1000, SelectMode.SelectRead) && (_listener.Available == 0)) || !_listener.Connected);
			}
			catch (Exception)
			{
				return false;
			}
		}

		private void OnConnectCallback(IAsyncResult result)
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
				_listener.BeginConnect(_endpoint, this.OnConnectCallback, _listener);
			}
		}

		/// <summary>
		/// Start receiving data from server.
		/// </summary>
		public void Receive()
		{
			//Start receiving data
			var state = new StateObject(_listener);
			StartReceiving(state);
		}

		public void InvokeMessage(string header, string text)
		{
			MessageReceived?.Invoke(this, header, text);
		}

		public void InvokeFileReceived(string filePath)
		{
			FileReceived?.Invoke(this, filePath);
		}

		public void ReceiveCallback(IAsyncResult result)
		{
			try
			{
				if (IsConnected())
				{
					HandleMessage(result);
				}
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

		public void InvokeAndReset(IStateObject state)
		{

			foreach (var v in _messageTypes)
			{
				if (v == state.Header)
				{
					MessageReceived?.Invoke(this, state.Header, state.Text);
					state.Reset();
					if (!state.Close)
					{
						return;
					}
					this.Dispose();
					return;

				}
			}


			FileReceived?.Invoke(this, state.Header);
			if (!state.Close)
			{
				state.Reset();
				return;
			}
			this.Dispose();



		}

		private void HandleMessage(IAsyncResult result)
		{

			try
			{

				var state = (StateObject)result.AsyncState;
				var receive = state.Listener.EndReceive(result);

				if (state.Flag == 0)
				{
					state.CurrentState = new InitialHandlerState(state, this);
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
				InvokeAndReset(state);
				//ChangeState(new InitReceiveState(this));
				StartReceiving(state);


			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
		}

		/// <summary>
		/// Sends data to server
		/// <para>This method should not be used,instead use methods in "SendToServer"</para>
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
					_listener.BeginSend(send, 0, send.Length, SocketFlags.None, this.SendCallback, _listener);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
		}

		private void SendCallback(IAsyncResult result)
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

			this.MessageSubmitted?.Invoke(this, _close);

			_sent.Set();
		}

		private void Close()
		{
			try
			{
				if (!this.IsConnected())
				{
					return;
				}

				_connected.Reset();
				_listener.Shutdown(SocketShutdown.Both);
				_listener.Close();
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		/// <summary>
		/// Safely close client and break all connections to server.
		/// </summary>
		public void Dispose()
		{
			_connected.Dispose();
			_sent.Dispose();
			this.Close();
		}

	}
}
