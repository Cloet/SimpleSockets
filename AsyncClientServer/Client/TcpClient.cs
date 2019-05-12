using AsyncClientServer.Compression;
using AsyncClientServer.Cryptography;
using AsyncClientServer.Messaging;
using AsyncClientServer.Messaging.Metadata;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

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
	/// Event that is triggered when client receives a custom header message.
	/// </summary>
	/// <param name="tcpClient"></param>
	/// <param name="msg"></param>
	public delegate void ClientCustomHeaderReceivedHandler(ITcpClient tcpClient, string msg, string header);

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
		//Protected variabeles
		protected Socket Listener;
		protected bool CloseClient;
		protected readonly ManualResetEvent ConnectedMre = new ManualResetEvent(false);
		protected readonly ManualResetEvent SentMre = new ManualResetEvent(false);
		protected IPEndPoint Endpoint;
		protected static System.Timers.Timer KeepAliveTimer;
		private bool _disconnectedInvoked;

		//Contains messages
		protected BlockingQueue<Message> BlockingMessageQueue = new BlockingQueue<Message>();

		//Tokensource to cancel running tasks
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

		//Events
		public event ConnectedHandler Connected;
		public event ClientMessageReceivedHandler MessageReceived;
		public event ClientCustomHeaderReceivedHandler CustomHeaderReceived;
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
			KeepAliveTimer = new System.Timers.Timer(15000);
			KeepAliveTimer.Elapsed += KeepAlive;
			KeepAliveTimer.AutoReset = true;
			KeepAliveTimer.Enabled = false;

			Encrypter = new Aes256();
			FileCompressor = new GZipCompression();
			FolderCompressor = new ZipCompression();
		}

		//Timer that tries reconnecting every x seconds
		private void KeepAlive(object source, ElapsedEventArgs e)
		{
			if (Token.IsCancellationRequested)
			{
				Close();
				ConnectedMre.Reset();
			} else if (!IsConnected())
			{
				Close();
				ConnectedMre.Reset();
				StartClient(IpServer, Port, ReconnectInSeconds);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Starts the client.
		/// <para>requires server ip, port number and how many seconds the client should wait to try to connect again. Default is 5 seconds</para>
		/// </summary>
		public abstract void StartClient(string ipServer, int port, int reconnectInSeconds = 5);

		//Convert string to IPAddress
		protected IPAddress GetIp(string ip)
		{
			try
			{
				IPAddress[] list = Dns.GetHostEntry(ip).AddressList;
				return list.First();
				//return Dns.GetHostAddresses(ip).First();
			}
			catch (SocketException se)
			{
				throw new Exception("Invalid server IP", se);
			}
			catch (Exception ex)
			{
				throw new Exception("Error trying to get IPAddress from string : " + ip, ex);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Check if client is connected to server
		/// </summary>
		/// <returns>bool</returns>
		public bool IsConnected()
		{
			try
			{
				return !((Listener.Poll(1000, SelectMode.SelectRead) && (Listener.Available == 0)) || !Listener.Connected);
			}
			catch (Exception)
			{
				return false;
			}
		}


		/// <summary>
		/// Closes the client.
		/// </summary>
		public void Close()
		{
			try
			{
				ConnectedMre.Reset();
				TokenSource.Cancel();

				if (!IsConnected())
				{
					return;
				}

				Listener.Shutdown(SocketShutdown.Both);
				Listener.Close();
				Listener = null;
				InvokeDisconnected(this);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		/// <summary>
		/// Safely close client and break all connections to server.
		/// </summary>
		public virtual void Dispose()
		{
			Close();
			ConnectedMre.Dispose();
			SentMre.Dispose();
			KeepAliveTimer.Enabled = false;
			KeepAliveTimer.Dispose();
			TokenSource.Dispose();

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


		//When client connects.
		protected abstract void OnConnectCallback(IAsyncResult result);


		#region Receiving Data



		/// <summary>
		/// Start receiving data from server.
		/// </summary>
		protected void Receive()
		{
			//Start receiving data
			var state = new SocketState(Listener);
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

		#endregion

		#region Sending data

		//Gets called when a message has been sent to the server.
		protected abstract void SendCallback(IAsyncResult result);

		//Gets called when file is done sending
		protected abstract void SendCallbackPartial(IAsyncResult result);

		//Gets called when Filetransfer is completed.
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
				SentMre.Set();
			}

		}

		//Sends message from queue
		protected abstract void BeginSendFromQueue(Message message);

		//Loops the queue for messages that have to be sent.
		protected void SendFromQueue()
		{
			while (!Token.IsCancellationRequested)
			{

				ConnectedMre.WaitOne();

				if (IsConnected())
				{
					BlockingMessageQueue.TryDequeue(out var message);
					BeginSendFromQueue(message);
				}
				else
				{
					Close();
					ConnectedMre.Reset();
				}

			}
		}

		#endregion



		#region Invokes


		internal void InvokeMessage(string text)
		{
			MessageReceived?.Invoke(this,  text);
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

		internal void InvokeCustomHeaderReceived(string msg, string header)
		{
			CustomHeaderReceived?.Invoke(this, msg, header);
		}

		protected void InvokeConnected(ITcpClient a)
		{
			Connected?.Invoke(a);
			_disconnectedInvoked = false;
		}

		protected void InvokeDisconnected(ITcpClient a)
		{
			if (_disconnectedInvoked == false)
			{
				Disconnected?.Invoke(a, a.IpServer, a.Port);
				_disconnectedInvoked = true;
			}
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
