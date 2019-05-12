using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AsyncClientServer.Cryptography;
using AsyncClientServer.Compression;
using AsyncClientServer.Messaging;
using AsyncClientServer.Messaging.Metadata;

namespace AsyncClientServer.Server
{

	/// <summary>
	/// Event that is triggered when a message is received
	/// </summary>
	/// <param name="id"></param>
	/// <param name="msg"></param>
	public delegate void MessageReceivedHandler(int id, string msg);

	/// <summary>
	/// Event that is triggered when a custom header message is received
	/// </summary>
	/// <param name="id"></param>
	/// <param name="msg"></param>
	public delegate void CustomHeaderMessageReceivedHandler(int id, string msg, string header);

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
	public delegate void ClientConnectedHandler(int id, ISocketInfo clientInfo);

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
	/// Triggered when a message was unable to complete it's transmission.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="messageData"></param>
	/// <param name="exceptionMessage"></param>
	public delegate void DataTransferToClientFailedHandler(int id, byte[] messageData, string exceptionMessage);

	/// <summary>
	/// Triggered when an error is thrown
	/// </summary>
	/// <param name="exceptionMessage"></param>
	public delegate void ServerErrorThrownHandler(string exceptionMessage);

	/// <summary>
	/// Event that is triggered when the server has started
	/// </summary>
	public delegate void ServerHasStartedHandler();

	/// <summary>
	/// Base class for ServerListener.
	/// <para>Use AsyncSocketListener or AsyncSocketSslListener.</para>
	/// </summary>
	public abstract class ServerListener : SendToClient, IServerListener
	{

		private static System.Timers.Timer _keepAliveTimer;
		internal IDictionary<int, ISocketState> ConnectedClients = new Dictionary<int, ISocketState>();

		protected int Limit = 500;
		protected readonly ManualResetEvent CanAcceptConnections = new ManualResetEvent(false);
		protected Socket Listener { get; set; }
		protected bool Disposed { get; set; }
		protected BlockingQueue<Message> BlockingMessageQueue = new BlockingQueue<Message>();

		protected CancellationTokenSource TokenSource { get; set; }
		protected CancellationToken Token { get; set; }

		/// <inheritdoc />
		/// <summary>
		/// Returns true when the server is running.
		/// </summary>
		public bool IsServerRunning { get; protected set; }

		//Events
		public event MessageReceivedHandler MessageReceived;
		public event CustomHeaderMessageReceivedHandler CustomHeaderReceived;
		public event MessageSubmittedHandler MessageSubmitted;
		public event ClientDisconnectedHandler ClientDisconnected;
		public event ClientConnectedHandler ClientConnected;
		public event FileFromClientReceivedHandler FileReceived;
		public event FileTransferProgressHandler ProgressFileReceived;
		public event ServerHasStartedHandler ServerHasStarted;
		public event DataTransferToClientFailedHandler MessageFailed;
		public event ServerErrorThrownHandler ErrorThrown;

		/// <summary>
		/// Get dictionary of clients
		/// </summary>
		/// <returns></returns>
		internal override IDictionary<int, ISocketState> GetClients()
		{
			return ConnectedClients;
		}

		/// <inheritdoc />
		/// <summary>
		/// Returns all currently connected clients
		/// </summary>
		/// <returns></returns>
		public IDictionary<int, ISocketInfo> GetConnectedClients()
		{
			return ConnectedClients.ToDictionary(x => x.Key, x => (ISocketInfo) x.Value);
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
		public FileCompression ServerFileCompressor
		{
			set => FileCompressor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Used to compress folder before sending
		/// </summary>
		public FolderCompression ServerFolderCompressor
		{
			set => FolderCompressor = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Base constructor
		/// </summary>
		protected ServerListener()
		{
			//Set timer that checks all clients every 5 minutes
			_keepAliveTimer = new System.Timers.Timer(300000);
			_keepAliveTimer.Elapsed += KeepAlive;
			_keepAliveTimer.AutoReset = true;
			_keepAliveTimer.Enabled = true;

			IsServerRunning = false;

			Encrypter = new Aes256();
			FileCompressor = new GZipCompression();
			FolderCompressor = new ZipCompression();
		}

		/// <summary>
		/// Add a socket to the clients dictionary.
		/// Lock clients temporary to handle mulitple access.
		/// ReceiveCallback raise an event, after the message receiving is complete.
		/// </summary>
		/// <param name="result"></param>
		protected abstract void OnClientConnect(IAsyncResult result);

		//Converts string to IPAddress
		protected IPAddress GetIp(string ip)
		{
			try
			{
				if (string.IsNullOrEmpty(ip))
				{
					IPAddress ipAdr = IPAddress.Any;
					Ip = ipAdr.ToString();
					return ipAdr;
				}
				else
				{
					return IPAddress.Parse(ip);
				}
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

		//Timer that checks client every x seconds
		private void KeepAlive(object source, ElapsedEventArgs e)
		{
			CheckAllClients();
		}

		/* Gets a socket from the clients dictionary by his Id. */
		internal ISocketState GetClient(int id)
		{
			ISocketState state;

			return ConnectedClients.TryGetValue(id, out state) ? state : null;
		}

		#region Public Methods

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
				ConnectedClients.Remove(id);
			}
		}

		/// <summary>
		/// Check all clients and show which are disconnected.
		/// </summary>
		public void CheckAllClients()
		{
			lock (ConnectedClients)
			{
				if (ConnectedClients.Keys.Count > 0)
				{
					foreach (var id in ConnectedClients.Keys)
					{
						CheckClient(id);
					}
				}
			}
		}

		/// <summary>
		/// Starts listening on the given port.
		/// </summary>
		///	<param name="ip"></param> 
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public abstract void StartListening(string ip, int port, int limit = 500);

		/// <summary>
		/// Stops the server from listening
		/// </summary>
		public void StopListening()
		{
			TokenSource.Cancel();
			IsServerRunning = false;

			foreach (var id in ConnectedClients.Keys.ToList())
			{
				Close(id);
			}

			Listener.Close();
		}

		/// <summary>
		/// Resumes listening
		/// </summary>
		public void ResumeListening()
		{
			if (IsServerRunning)
				throw new Exception("The server is already running.");

			if (string.IsNullOrEmpty(Ip))
				throw new ArgumentException("This method should only be used after using 'StopListening()'");
			if (Port == 0)
				throw new ArgumentException("This method should only be used after using 'StopListening()'");

			StartListening(Ip, Port, Limit);
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

		/// <inheritdoc />
		/// <summary>
		/// Properly dispose the class.
		/// </summary>
		public virtual void Dispose()
		{
			try
			{
				if (!Disposed)
				{
					TokenSource.Cancel();
					TokenSource.Dispose();
					IsServerRunning = false;
					Listener.Dispose();
					CanAcceptConnections.Dispose();
					_keepAliveTimer.Enabled = false;
					_keepAliveTimer.Dispose();

					foreach (var id in ConnectedClients.Keys.ToList())
					{
						Close(id);
					}

					ConnectedClients = new Dictionary<int, ISocketState>();
					TokenSource.Dispose();
					Disposed = true;
					GC.SuppressFinalize(this);
				}
				else
				{
					throw new ObjectDisposedException(nameof(ServerListener), "This object is already disposed.");
				}

			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
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
				lock (ConnectedClients)
				{
					ConnectedClients.Remove(id);
					ClientDisconnected?.Invoke(state.Id);
				}
			}
		}

		#endregion

		#region Message Sending

		protected abstract void BeginSendFromQueue(Message message);

		protected void SendFromQueue()
		{

			while (!Token.IsCancellationRequested)
			{
				BlockingMessageQueue.TryPeek(out var message);

				if (IsConnected(message.SocketState.Id))
				{
					BlockingMessageQueue.TryDequeue(out message);
					BeginSendFromQueue(message);
				}
				else
				{
					Close(message.SocketState.Id);
				}

			}
		}

		#endregion

		#region Receiving Data

		//Handles messages the server receives
		protected void ReceiveCallback(IAsyncResult result)
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
		internal abstract void StartReceiving(ISocketState state, int offset = 0);

		//Handles messages
		protected abstract void HandleMessage(IAsyncResult result);

		#endregion

		#region Callbacks

		//End the send and invoke MessageSubmitted event.
		protected abstract void SendCallback(IAsyncResult result);

		//End the send and invoke MessageSubmitted event.
		protected abstract void SendCallbackPartial(IAsyncResult result);

		//Called when a File or Folder has been transfered.
		protected override void FileTransferCompleted(bool close, int id)
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

		#endregion

		#region Invokes

		protected void ClientDisconnectedInvoke(int id)
		{
			ClientDisconnected?.Invoke(id);
		}

		protected void ClientConnectedInvoke(int id, ISocketInfo clientInfo)
		{
			ClientConnected?.Invoke(id, clientInfo);
		}

		protected void ServerHasStartedInvoke()
		{
			IsServerRunning = true;
			ServerHasStarted?.Invoke();
		}

		internal void InvokeFileReceived(int id, string filePath)
		{
			FileReceived?.Invoke(id, filePath);
		}

		internal void InvokeFileTransferProgress(int id, int bytesReceived, int messageSize)
		{
			ProgressFileReceived?.Invoke(id, bytesReceived, messageSize);
		}

		protected void InvokeMessageSubmitted(int id, bool close)
		{
			MessageSubmitted?.Invoke(id, close);
		}

		protected void InvokeErrorThrown(string exception)
		{
			ErrorThrown?.Invoke(exception);
		}

		protected void InvokeMessageFailed(int id, byte[] messageData, string exception)
		{
			MessageFailed?.Invoke(id, messageData, exception);
		}

		internal void InvokeCustomHeaderReceived(int id, string msg, string header)
		{
			CustomHeaderReceived?.Invoke(id, msg, header);
		}

		internal void InvokeMessageReceived(int id, string text)
		{
			MessageReceived?.Invoke(id, text);
		}

		#endregion

	}
}
