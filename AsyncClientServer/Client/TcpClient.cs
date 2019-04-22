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
using System.Windows.Forms;
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
	public delegate void ClientMessageReceivedHandler(ITcpClient tcpClient, string header, string msg);
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
		/// Event that is triggered when a message fails to send
		/// </summary>
		public event DataTransferFailedHandler MessageFailed;

		/// <summary>
		/// Event that is triggered when an error is thrown
		/// </summary>
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
		protected void Close()
		{
			try
			{
				if (!this.IsConnected())
				{
					_connected.Reset();
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

			GC.SuppressFinalize(this);
		}



		#region Invokes

		/// <summary>
		/// Invokes MessageReceived event of the client.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="text"></param>
		internal void InvokeMessage(string header, string text)
		{
			MessageReceived?.Invoke(this, header, text);
		}

		protected void InvokeMessageSubmitted(bool close)
		{
			MessageSubmitted?.Invoke(this, close);
		}

		/// <summary>
		/// Invokes FileReceived event of the client
		/// </summary>
		/// <param name="filePath"></param>
		internal void InvokeFileReceived(string filePath)
		{
			FileReceived?.Invoke(this, filePath);
		}

		/// <summary>
		/// Invokes ProgressReceived event
		/// </summary>
		/// <param name="bytesReceived"></param>
		/// <param name="messageSize"></param>
		internal void InvokeFileTransferProgress(int bytesReceived, int messageSize)
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
			Disconnected?.Invoke(a, a.IpServer, a.Port);
		}

		/// <summary>
		/// Triggered when message has failed to send
		/// </summary>
		/// <param name="messageData"></param>
		/// <param name="exception"></param>
		protected void InvokeMessageFailed(byte[] messageData, string exception)
		{
			MessageFailed?.Invoke(this,messageData, exception);
		}

		/// <summary>
		/// Triggered when error is thrown
		/// </summary>
		/// <param name="exception"></param>
		protected void InvokeErrorThrown(string exception)
		{
			ErrorThrown?.Invoke(this, exception);
		}

		#endregion

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

	}
}
