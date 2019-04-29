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
	/// Event that is triggered when a command is received
	/// </summary>
	/// <param name="id"></param>
	/// <param name="msg"></param>
	public delegate void CommandReceivedHandler(int id, string msg);

	/// <summary>
	/// Event that is triggered when a serialized object is received.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="serializedObject"></param>
	public delegate void ObjectReceivedHandler(int id, string serializedObject);

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

	//Calls the SendBytesAsync Method.
	public delegate void AsyncCallerBroadcast(bool close);

	public abstract class ServerListener : SendToClient, IServerListener
	{

		protected int Limit = 500;
		protected ManualResetEvent _serverCanListen = new ManualResetEvent(false);
		protected readonly ManualResetEvent _mre = new ManualResetEvent(false);
		internal IDictionary<int, ISocketState> _clients = new Dictionary<int, ISocketState>();
		private static System.Timers.Timer _keepAliveTimer;
		protected Socket _listener { get; set; }
		protected bool _disposed { get; set; }

		protected CancellationTokenSource TokenSource { get; set; }
		protected CancellationToken Token { get; set; }

		/// <summary>
		/// Returns true when the server is running.
		/// </summary>
		public bool IsServerRunning { get; protected set; }

		//Events
		public event MessageReceivedHandler MessageReceived;
		public event CustomHeaderMessageReceivedHandler CustomHeaderReceived;
		public event CommandReceivedHandler CommandReceived;
		public event ObjectReceivedHandler ObjectReceived;
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
			return _clients;
		}

		/// <inheritdoc />
		/// <summary>
		/// Returns all currently connected clients
		/// </summary>
		/// <returns></returns>
		public IDictionary<int, ISocketInfo> GetConnectedClients()
		{
			return _clients.ToDictionary(x => x.Key, x => (ISocketInfo) x.Value);
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

		/// <summary>
		/// Stops the server from listening
		/// </summary>
		public void StopListening()
		{
			TokenSource.Cancel();
			IsServerRunning = false;

			foreach (var id in _clients.Keys.ToList())
			{
				Close(id);
			}

			_listener.Close();
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

		/* Gets a socket from the clients dictionary by his Id. */
		internal ISocketState GetClient(int id)
		{
			ISocketState state;

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


		//End the send and invoke MessageSubmitted event.
		protected abstract void SendCallback(IAsyncResult result);

		//End the send and invoke MessageSubmitted event.
		protected abstract void SendCallbackPartial(IAsyncResult result);

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

		//Converts string to IPAddress
		protected IPAddress GetIp(string ip)
		{
			try
			{
				return Dns.GetHostAddresses(ip).First();
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
		/// Properly dispose the class.
		/// </summary>
		public virtual void Dispose()
		{
			try
			{
				if (!_disposed)
				{
					TokenSource.Cancel();
					TokenSource.Dispose();
					IsServerRunning = false;
					_listener.Dispose();
					_mre.Dispose();
					_keepAliveTimer.Enabled = false;
					_keepAliveTimer.Dispose();

					foreach (var id in _clients.Keys.ToList())
					{
						Close(id);
					}

					_clients = new Dictionary<int, ISocketState>();
					TokenSource.Dispose();
					_disposed = true;
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

		internal void InvokeCommandReceived(int id, string msg)
		{
			CommandReceived?.Invoke(id, msg);
		}

		internal void InvokeObjectReceived(int id, string serializedObject)
		{
			ObjectReceived?.Invoke(id, serializedObject);
		}

		#endregion

	}
}
