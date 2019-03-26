using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AsyncClientServer.Server;
using AsyncClientServer.StateObject;
using AsyncClientServer.StateObject.StateObjectState;

namespace AsyncClientServer.Client
{

	/// <summary>
	/// Event that triggers when a client is connected to server
	/// </summary>
	/// <param name="a"></param>
	public delegate void ConnectedHandler(ITcpClient a);

	/// <summary>
	/// Event that is triggered when the client has disconnected from the server.
	/// </summary>
	public delegate void DisconnectedFromServerHandler(ITcpClient a, string ipServer, int port);

	/// <summary>
	/// Event that triggers when client receives a message
	/// </summary>
	/// <param name="a"></param>
	/// <param name="msg"></param>
	public delegate void ClientMessageReceivedHandler(ITcpClient a, string header, string msg);
	/// <summary>
	/// Event that triggers when client sends a message
	/// </summary>
	/// <param name="a"></param>
	/// <param name="close"></param>
	public delegate void ClientMessageSubmittedHandler(ITcpClient a, bool close);

	/// <summary>
	/// Event that is triggered when a file is received from the server, returns the new file path
	/// </summary>
	/// <param name="path"></param>
	public delegate void FileFromServerReceivedHandler(ITcpClient a, string path);

	/// <summary>
	/// Event that is triggered when a file is received from the server and show the progress.
	/// </summary>
	/// <param name="a"></param>
	/// <param name="bytesReceived"></param>
	/// <param name="messageSize"></param>
	public delegate void ProgressFileTransferHandler(ITcpClient a, int bytesReceived, int messageSize);

	public abstract class TcpClient: SendToServer, ITcpClient
	{

		protected Socket _listener;
		protected bool _close;
		protected readonly ManualResetEvent _connected = new ManualResetEvent(false);
		protected readonly ManualResetEvent _sent = new ManualResetEvent(false);
		protected IPEndPoint _endpoint;
		protected static System.Timers.Timer _keepAliveTimer;

		/// <inheritdoc />
		/// <summary>
		/// The port of the server
		/// </summary>
		public int Port { get; protected set; }

		/// <inheritdoc />
		/// <summary>
		/// The ip of the server
		/// </summary>
		public string IpServer { get; protected set; }

		/// <inheritdoc />
		/// <summary>
		/// This is how many seconds te client waits to try and reconnect to the server
		/// </summary>
		public int ReconnectInSeconds { get; protected set; }


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
		/// Event that tracks the progress of a FileTransfer.
		/// </summary>
		public event ProgressFileTransferHandler ProgressFileReceived;

		/// <summary>
		/// Event that is used to check if the client is still connected to the server.
		/// </summary>
		public event DisconnectedFromServerHandler Disconnected;

		/// <summary>
		/// Constructor
		/// Use StartClient() to start a connection to a server.
		/// </summary>
		protected TcpClient()
		{
			_keepAliveTimer = new System.Timers.Timer(15000);
			_keepAliveTimer.Elapsed += KeepAlive;
			_keepAliveTimer.AutoReset = true;
			_keepAliveTimer.Enabled = false;
		}

		//Timer that tries reconnecting every x seconds
		private void KeepAlive(Object source, ElapsedEventArgs e)
		{
			if (!IsConnected())
			{
				Disconnected?.Invoke(this, this.IpServer, this.Port);
				this.Close();
				_connected.Reset();
				StartClient(IpServer, Port, ReconnectInSeconds);
			}
		}

		/// <summary>
		/// Starts the client.
		/// <para>requires server ip, port number and how many seconds the client should wait to try to connect again. Default is 5 seconds</para>
		/// </summary>
		public abstract void StartClient(string ipServer, int port, int reconnectInSeconds);

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

		//When client connects.
		protected void OnConnectCallback(IAsyncResult result)
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
		private void Receive()
		{
			//Start receiving data
			var state = new StateObject.StateObject(_listener);
			StartReceiving(state);
		}


		//When client receives message
		private void ReceiveCallback(IAsyncResult result)
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

		//Start receiving
		private void StartReceiving(IStateObject state)
		{
			if (state.Buffer.Length < state.BufferSize)
			{
				state.ChangeBuffer(new byte[state.BufferSize]);
			}

			state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None,
				this.ReceiveCallback, state);
		}

		//Handle a message
		private void HandleMessage(IAsyncResult result)
		{

			try
			{

				var state = (StateObject.StateObject)result.AsyncState;
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
				state.Reset();
				StartReceiving(state);


			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
		}

		/// <summary>
		/// Sends data to server
		/// <para>This method should not be used,instead use methods in <see cref="SendToServer"/></para>
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
					_listener.BeginSend(send, 0, send.Length, SocketFlags.None, SendCallback, _listener);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Send message and invokes MessageSubmitted.
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

			MessageSubmitted?.Invoke(this, _close);

			_sent.Set();
		}

		//Close client
		protected void Close()
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
			Close();
		}



		#region Invokes

		/// <summary>
		/// Invokes MessageReceived event of the client.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="text"></param>
		public void InvokeMessage(string header, string text)
		{
			MessageReceived?.Invoke(this, header, text);
		}

		/// <inheritdoc />
		/// <summary>
		/// Invokes FileReceived event of the client
		/// </summary>
		/// <param name="filePath"></param>
		public void InvokeFileReceived(string filePath)
		{
			FileReceived?.Invoke(this, filePath);
		}

		/// <summary>
		/// Invokes ProgressReceived event
		/// </summary>
		/// <param name="bytesReceived"></param>
		/// <param name="messageSize"></param>
		public void InvokeFileTransferProgress(int bytesReceived, int messageSize)
		{
			ProgressFileReceived?.Invoke(this, bytesReceived, messageSize);
		}

		/// <summary>
		/// Invokes Client connected to server
		/// </summary>
		protected void InvokeConnected(ITcpClient a)
		{
			Connected?.Invoke(a);
		}

		/// <summary>
		/// Invokes client disconnected from server
		/// </summary>
		/// <param name="a"></param>
		protected void InvokeDisconnected(ITcpClient a)
		{
			Disconnected?.Invoke(a,a.IpServer,a.Port);
		}

		#endregion


	}
}
