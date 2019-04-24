using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AsyncClientServer.Compression;
using AsyncClientServer.Cryptography;
using AsyncClientServer.Messaging.Metadata;
using AsyncClientServer.Server;

namespace AsyncClientServer.Client
{

	/// <summary>
	/// Event that triggers when a client is connected to server
	/// </summary>
	/// <param name="tcpClient"></param>
	public delegate void ConnectedHandler(ITcpClient tcpClient);

	/// <summary>
	/// Event that is triggered when the client has disconnected from the server.
	/// </summary>
	public delegate void DisconnectedFromServerHandler(ITcpClient tcpClient, string ipServer, int port);

	/// <summary>
	/// Event that triggers when client receives a message
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="msg"></param>
	public delegate void ClientMessageReceivedHandler(ITcpClient tcpClient, string msg);

	/// <summary>
	/// Event that is triggered when the client receives a serialized object.
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="serializedObject"></param>
	public delegate void ClientObjectReceivedHandler(ITcpClient tcpClient, string serializedObject);

	/// <summary>
	/// Event that is triggered when the client receives a command.
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="command"></param>
	public delegate void ClientCommandReceivedHandler(ITcpClient tcpClient, string command);

	/// <summary>
	/// Event that triggers when client sends a message
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="close"></param>
	public delegate void ClientMessageSubmittedHandler(ITcpClient tcpClient, bool close);

	/// <summary>
	/// Event that is triggered when a file is received from the server, returns the new file path
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="path"></param>
	public delegate void FileFromServerReceivedHandler(ITcpClient tcpClient, string path);

	/// <summary>
	/// Event that is triggered when a message failed to send
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="exceptionMessage"></param>
	public delegate void DataTransferFailedHandler(ITcpClient tcpClient,byte[] messageData, string exceptionMessage);

	/// <summary>
	/// Event that is triggered when a message has failed to send
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="exceptionMessage"></param>
	public delegate void ErrorHandler(ITcpClient tcpClient, string exceptionMessage);

	/// <summary>
	/// Event that is triggered when a file is received from the server and show the progress.
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="bytesReceived"></param>
	/// <param name="messageSize"></param>
	public delegate void ProgressFileTransferHandler(ITcpClient tcpClient, int bytesReceived, int messageSize);

	public abstract class TcpClient : SendToServer, ITcpClient
	{

		protected Socket _listener;
		protected bool _close;
		protected readonly ManualResetEvent _connected = new ManualResetEvent(false);
		protected readonly ManualResetEvent _sent = new ManualResetEvent(false);
		protected IPEndPoint _endpoint;
		protected static System.Timers.Timer _keepAliveTimer;

		protected CancellationTokenSource TokenSource { get; set; }
		protected CancellationToken Token { get; set; }

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

		/// <summary>
		/// Used to encrypt files/folders
		/// </summary>
		public Encryption MessageEncrypter
		{
			set => Encrypter = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Used to compress files before sending
		/// </summary>
		public FileCompression ClientFileCompressor
		{
			set => FileCompressor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Used to compress folder before sending
		/// </summary>
		public FolderCompression ClientFolderCompressor
		{
			set => FolderCompressor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		/// <summary>
		/// This is how many seconds te client waits to try and reconnect to the server
		/// </summary>
		public int ReconnectInSeconds { get; protected set; }


		public event ConnectedHandler Connected;
		public event ClientMessageReceivedHandler MessageReceived;
		public event ClientObjectReceivedHandler ObjectReceived;
		public event ClientCommandReceivedHandler CommandReceived;
		public event ClientMessageSubmittedHandler MessageSubmitted;
		public event FileFromServerReceivedHandler FileReceived;
		public event ProgressFileTransferHandler ProgressFileReceived;
		public event DisconnectedFromServerHandler Disconnected;
		public event DataTransferFailedHandler MessageFailed;
		public event ErrorHandler ErrorThrown;

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

			Encrypter = new Aes256();
			FileCompressor = new GZipCompression();
			FolderCompressor = new ZipCompression();
		}

		//Timer that tries reconnecting every x seconds
		private void KeepAlive(Object source, ElapsedEventArgs e)
		{
			if (!IsConnected())
			{
				Disconnected?.Invoke(this, this.IpServer, this.Port);
				Close();
				_connected.Reset();
				StartClient(IpServer, Port, ReconnectInSeconds);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Starts the client.
		/// <para>requires server ip, port number and how many seconds the client should wait to try to connect again. Default is 5 seconds</para>
		/// </summary>
		public abstract void StartClient(string ipServer, int port, int reconnectInSeconds = 5);

		/// <inheritdoc />
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
		protected abstract void OnConnectCallback(IAsyncResult result);

		//*******Receiving Data**************////

		/// <summary>
		/// Start receiving data from server.
		/// </summary>
		protected  void Receive()
		{
			//Start receiving data
			var state = new SocketState(_listener);
			StartReceiving(state);
		}


		//When client receives message
		protected void ReceiveCallback(IAsyncResult result)
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


		/// <summary>
		/// Start receiving bytes from server
		/// </summary>
		/// <param name="state"></param>
		/// <param name="offset"></param>
		internal abstract void StartReceiving(ISocketState state, int offset = 0);

		//Handle a message
		protected abstract void HandleMessage(IAsyncResult result);

		//******************************************///

		//*****Message Sending****///

		//Send message and invokes MessageSubmitted.
		protected abstract void SendCallback(IAsyncResult result);

		//************************///


		//***File Transfer***///


		//Gets called when file is done sending
		protected abstract void SendCallbackPartial(IAsyncResult result);

		protected override void FileTransferCompleted(bool close, int id)
		{
			try
			{
				if (close)
					Close();
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
				MessageSubmitted?.Invoke(this, close);
				_sent.Set();
			}

		}

		//*****************///

		//Closes client
		public void Close()
		{
			try
			{
				_connected.Reset();
				TokenSource.Cancel();

				if (!IsConnected())
				{
					return;
				}

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
			Close();
			_connected.Dispose();
			_sent.Dispose();

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Change the buffer size of the server
		/// </summary>
		/// <param name="bufferSize"></param>
		public void ChangeSocketBufferSize(int bufferSize)
		{
			if (bufferSize < 1024)
				throw new ArgumentException("The buffer size should be more then 1024 bytes.");

			SocketState.ChangeBufferSize(bufferSize);
		}

		#region Invokes


		internal void InvokeMessage(string text)
		{
			MessageReceived?.Invoke(this,  text);
		}

		internal void InvokeObject(string serializedObject)
		{
			ObjectReceived?.Invoke(this, serializedObject);
		}

		internal void InvokeCommand(string command)
		{
			CommandReceived?.Invoke(this, command);
		}

		protected void InvokeMessageSubmitted(bool close)
		{
			MessageSubmitted?.Invoke(this, close);
		}

		internal void InvokeFileReceived(string filePath)
		{
			FileReceived?.Invoke(this, filePath);
		}

		internal void InvokeFileTransferProgress(int bytesReceived, int messageSize)
		{
			ProgressFileReceived?.Invoke(this, bytesReceived, messageSize);
		}

		protected void InvokeConnected(ITcpClient a)
		{
			Connected?.Invoke(a);
		}

		protected void InvokeDisconnected(ITcpClient a)
		{
			Disconnected?.Invoke(a, a.IpServer, a.Port);
		}

		protected void InvokeMessageFailed(byte[] messageData, string exception)
		{
			MessageFailed?.Invoke(this, messageData, exception);
		}

		protected void InvokeErrorThrown(string exception)
		{
			ErrorThrown?.Invoke(this, exception);
		}

		#endregion

	}
}
