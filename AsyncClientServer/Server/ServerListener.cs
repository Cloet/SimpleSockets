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
using AsyncClientServer.Messaging;
using AsyncClientServer.Messaging.Compression;
using AsyncClientServer.Messaging.Cryptography;
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
	public abstract class ServerListener : AsyncSocket
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
		internal IDictionary<int, ISocketState> GetClients()
		{
			return ConnectedClients;
		}

		/// <summary>
		/// Returns all currently connected clients
		/// </summary>
		/// <returns></returns>
		public IDictionary<int, ISocketInfo> GetConnectedClients()
		{
			return ConnectedClients.ToDictionary(x => x.Key, x => (ISocketInfo) x.Value);
		}

		/// <summary>
		/// Server will only accept IP Addresses that are in the whitelist.
		/// If the whitelist is empty all IPs will be accepted unless they are blacklisted.
		/// </summary>
		public IList<IPAddress> WhiteList { get; set; }

		/// <summary>
		/// The server will not accept connections from these IPAddresses.
		/// Whitelist has priority over Blacklist meaning that if an IPAddress is in the whitelist and blacklist
		/// It will still be added.
		/// </summary>
		public IList<IPAddress> BlackList { get; set; }
		

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
			WhiteList = new List<IPAddress>();
			BlackList = new List<IPAddress>();

			IsRunning = false;

			MessageEncryption = new Aes256();
			FileCompressor = new GZipCompression();
			FolderCompressor = new ZipCompression();
		}

		/// <summary>
		/// Add a socket to the clients dictionary.
		/// Lock clients temporary to handle multiple access.
		/// ReceiveCallback raise an event, after the message receiving is complete.
		/// </summary>
		/// <param name="result"></param>
		protected abstract void OnClientConnect(IAsyncResult result);

		//Converts string to IPAddress
		protected IPAddress DetermineListenerIp(string ip)
		{
			try
			{
				if (string.IsNullOrEmpty(ip))
				{
					var ipAdr = IPAddress.Any;
					Ip = ipAdr.ToString();
					return ipAdr;
				}

				//Try to parse the ip string to a valid IPAddress
				return IPAddress.Parse(ip);

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

		/// <summary>
		/// Export Connected Clients to a DSV (Delimiter separated values)  File
		/// </summary>
		/// <param name="path"></param>
		/// <param name="delimiter"></param>
		public void ExportConnectedClientsToDsv(string path, string delimiter = ";")
		{

			using (StreamWriter sw = new StreamWriter(path))
			{
				var last = GetConnectedClients().Last();
				foreach (var client in GetConnectedClients())
				{
					if (client.Key != last.Key)
						sw.Write(client.Value.RemoteIPv4 + delimiter);
					else
						sw.Write(client.Value.RemoteIPv4);

				}
			}

		}

		//Check if the server should allow the client that is attempting to connect.
		internal bool IsConnectionAllowed(ISocketState state)
		{
			if (WhiteList.Count > 0)
			{
				return CheckWhitelist(state.RemoteIPv4) || CheckWhitelist(state.RemoteIPv6);
			}

			if (BlackList.Count > 0)
			{
				return !CheckBlacklist(state.RemoteIPv4) && !CheckBlacklist(state.RemoteIPv6);
			}
			
			return true;
		}

		//Checks if an ip is in the whitelist
		protected bool CheckWhitelist(string ip)
		{
			var address = IPAddress.Parse(ip);
			return WhiteList.Any(x => Equals(x, address));
		}

		//Checks if an ip is in the blacklist
		protected bool CheckBlacklist(string ip)
		{
			var address = IPAddress.Parse(ip);
			return BlackList.Any(x => Equals(x, address));
		}

		//Timer that checks client every x seconds
		private void KeepAlive(object source, ElapsedEventArgs e)
		{
			CheckAllClients();
		}

		// Gets a socket from the clients dictionary by his Id.
		internal ISocketState GetClient(int id)
		{
			return ConnectedClients.TryGetValue(id, out var state) ? state : null;
		}

		#region Public Methods

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
		/// Starts listening on all possible interfaces.
		/// Safest option to start the server.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="limit"></param>
		public void StartListening(int port, int limit = 500)
		{
			StartListening(null, port, limit);
		}


		/// <summary>
		/// Stops the server from listening
		/// </summary>
		public void StopListening()
		{
			TokenSource.Cancel();
			IsRunning = false;

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
			if (IsRunning)
				throw new Exception("The server is already running.");

			if (string.IsNullOrEmpty(Ip))
				throw new ArgumentException("This method should only be used after using 'StopListening()'");
			if (Port == 0)
				throw new ArgumentException("This method should only be used after using 'StopListening()'");

			StartListening(Ip, Port, Limit);
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

		/// <inheritdoc />
		/// <summary>
		/// Properly dispose the class.
		/// </summary>
		public override void Dispose()
		{
			try
			{
				if (!Disposed)
				{
					TokenSource.Cancel();
					TokenSource.Dispose();
					IsRunning = false;
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
		protected override void ReceiveCallback(IAsyncResult result)
		{
			try
			{
				HandleMessage(result);
			}
			catch (Exception ex)
			{
                this.InvokeErrorThrown(ex.Message);
			}
		}

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
			IsRunning = true;
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


		#region Messaging

		/// <summary>
		/// Sends bytes to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="data"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		protected abstract void SendBytes(int id, byte[] data, bool close);


		#region Message

		/*==========================================
		*
		*	MESSAGE
		*
		*===========================================*/

		/// <summary>
		/// Send a message to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		public void SendMessage(int id, string message, bool encryptMessage, bool close)
		{
			byte[] data = CreateByteMessage(message, encryptMessage);
			SendBytes(id, data, close);
		}

		/// <summary>
		/// Sends a message to the corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>This method encrypts the message that will be send.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		public void SendMessage(int id, string message, bool close)
		{
			SendMessage(id, message, true, close);
		}

		/// <summary>
		/// Send a message to corresponding client asynchronous.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		public async Task SendMessageAsync(int id, string message, bool encryptMessage, bool close)
		{
			await Task.Run(() => SendMessage(id, message, encryptMessage, close));
		}

		/// <summary>
		/// Sends a message to the corresponding client asynchronous.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>This method encrypts the message that will be send.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		public async Task SendMessageAsync(int id, string message, bool close)
		{
			await Task.Run(() => SendMessage(id, message, close));
		}


		#endregion

		#region File

		
		/*================================
		*
		*	FILE
		*
		*===========================================*/

		/// <summary>
		/// Sends a file to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		public void SendFile(int id, string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile, bool close)
		{
			try
			{
				Task.Run(() => SendFileAsync(id, fileLocation, remoteSaveLocation, encryptFile, compressFile, close));
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <summary>
		/// Sends a file to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Encrypts and compresses the file before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFile(int id, string fileLocation, string remoteSaveLocation, bool close)
		{
			SendFile(id, fileLocation, remoteSaveLocation, false, true, close);
		}

		/// <summary>
		/// Sends a file to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool encryptFile, bool compressFile, bool close)
		{
			await CreateAndSendAsyncFileMessage(fileLocation, remoteFileLocation, compressFile, encryptFile, close, id);
		}

		/// <summary>
		/// Sends a file to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Encrypts and compresses the file before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="close"></param>
		public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool close)
		{
			await SendFileAsync(id, fileLocation, remoteFileLocation, false, true, close);
		}


		#endregion

		#region Folder

		/*=================================
		*
		*	FOLDER
		*
		*===========================================*/

		/// <summary>
		/// Sends a folder to the corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Folder will be compressed to .zip file before being sent.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			try
			{
				Task.Run(() => SendFolderAsync(id, folderLocation, remoteFolderLocation, encryptFolder, close));
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <summary>
		/// Sends a folder to the corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Folder will be compressed to a .zip file and encrypted.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool close)
		{
			SendFolder(id, folderLocation, remoteFolderLocation, true, close);
		}

		/// <summary>
		/// Sends a folder to the corresponding client asynchronous.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Folder will be compressed to .zip file before being sent.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public async Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			try
			{
				await CreateAndSendAsyncFolderMessage(folderLocation, remoteFolderLocation, encryptFolder, close, id);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}


		/// <summary>
		/// Sends a folder to the corresponding client asynchronous.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Folder will be compressed to a .zip file and encrypted.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		public async Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool close)
		{
			await SendFolderAsync(id, folderLocation, remoteFolderLocation, false, close);
		}

		#endregion

		#region Custom Header
		
		/// <summary>
		/// Sends a message to the client with a custom header.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		public void SendCustomHeaderMessage(int id, string message, string header, bool close)
		{
			SendCustomHeaderMessage(id, message, header, false, close);
		}

		/// <summary>
		/// Sends a message to the client with a custom header.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <param name="close"></param>
		public void SendCustomHeaderMessage(int id, string message, string header, bool encrypt, bool close)
		{
			byte[] data = CreateByteCustomHeader(message, header, encrypt);
			SendBytes(id, data, close);
		}

		/// <summary>
		/// Sends a message to the client with a custom header
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		public async Task SendCustomHeaderMessageAsync(int id, string message, string header, bool close)
		{
			await Task.Run(() => SendCustomHeaderMessage(id, message, header, false, close));
		}

		/// <summary>
		/// Sends a message to the client with a custom header
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <param name="close"></param>
		public async Task SendCustomHeaderMessageAsync(int id, string message, string header, bool encrypt, bool close)
		{
			await Task.Run(() => SendCustomHeaderMessage(id, message, header, encrypt, close));
		}

		#endregion

		#region Broadcast
		
		///////////////
		//Broadcasts//
		//////////////

		/*=================================
		*
		*	FILE
		*
		*===========================================*/

		/// <summary>
		/// Sends a file to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool encryptFile,bool compressFile, bool close)
		{
			var data = CreateByteFile(fileLocation, remoteSaveLocation, encryptFile, compressFile);
			foreach (var c in GetClients())
			{
				SendBytes(c.Value.Id, data, close);
			}
		}

		/// <summary>
		/// Sends a file to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt and compress the file before sending.</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool close)
		{
			SendFileToAllClients(fileLocation, remoteSaveLocation, false, true, close);
		}

		/// <summary>
		/// Sends a file to all clients asynchronous
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool encryptFile,bool compressFile,bool close)
		{
			await CreateAsyncFileMessageBroadcast(fileLocation, remoteSaveLocation, compressFile, encryptFile, close);
		}

		/// <summary>
		/// Sends a file to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt and compress the file before sending.</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool close)
		{
			await SendFileToAllClientsAsync(fileLocation, remoteSaveLocation, false, true, close);
		}


		/*=================================
		*
		*	FOLDER
		*
		*===========================================*/

		/// <summary>
		/// Sends a folder to all clients.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			var data = CreateByteFolder(folderLocation, remoteFolderLocation, encryptFolder);
			foreach (var c in GetClients())
			{
				SendBytes(c.Value.Id, data, close);
			}
		}

		/// <summary>
		/// Sends a folder to all clients.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt and compress the folder before sending.</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool close)
		{
			SendFolderToAllClients(folderLocation, remoteFolderLocation, false, close);
		}

		/// <summary>
		/// Sends a folder to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			await CreateAsyncFolderMessageBroadcast(folderLocation, remoteFolderLocation, encryptFolder, close);
		}

		/// <summary>
		/// Sends a folder to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt and compress the folder before sending.</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool close)
		{
			await SendFolderToAllClientsAsync(folderLocation, remoteFolderLocation, false, close);
		}

		/*=================================
		*
		*	Custom Header
		*
		*===========================================*/

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public void SendCustomHeaderToAllClients(string message, string header, bool encryptMessage, bool close)
		{
			var data = CreateByteCustomHeader(message, header, encryptMessage);
			foreach (var c in GetClients())
			{
				SendBytes(c.Value.Id, data, close);
			}
		}

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		public void SendCustomHeaderToAllClients(string message, string header, bool close)
		{
			SendCustomHeaderToAllClients(message, header, false, close);
		}

		/// <summary>
		/// Sends a Message to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public async Task SendCustomHeaderToAllClientsAsync(string message, string header, bool encryptMessage, bool close)
		{
			await Task.Run(() => SendCustomHeaderToAllClients(message, header, encryptMessage, close));
		}

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		public async Task SendCustomHeaderToAllClientsAsync(string message, string header, bool close)
		{
			await Task.Run(() => SendCustomHeaderToAllClients(message, header, false, close));
		}



		/*=================================
		*
		*	MESSAGE
		*
		*===========================================*/

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public void SendMessageToAllClients(string message, bool encryptMessage, bool close)
		{
			var data = CreateByteMessage(message, encryptMessage);
			foreach (var c in GetClients())
			{
				SendBytes(c.Value.Id, data, close);
			}
		}

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessageToAllClients(string message, bool close)
		{
			SendMessageToAllClients(message, false, close);
		}

		/// <summary>
		/// Sends a Message to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public async Task SendMessageToAllClientsAsync(string message, bool encryptMessage, bool close)
		{
			await Task.Run(() => SendMessageToAllClients(message, false, close));
		}

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public async Task SendMessageToAllClientsAsync(string message, bool close)
		{
			await Task.Run(() => SendMessageToAllClients(message, close));
		}


		#region Broadcast File/folder

		protected async Task<List<int>> StreamFileAndSendToAllClients(string location, string remoteSaveLocation, bool encrypt)
		{
			try
			{
				var file = location;
				var buffer = new byte[10485760];
				bool firstRead = true;
				List<int> clientIds = new List<int>();

				foreach (var c in GetClients())
				{
					clientIds.Add(c.Value.Id);
				}

				//Stream that reads the file and sends bits to the server.
				using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true))
				{
					//How much bytes that have been read
					int read = 0;


					while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
					{
						//data bytes
						byte[] data = null;

						//The message
						byte[] message = new byte[read];
						Array.Copy(buffer, 0, message, 0, read);


						//Checks if it is the first read of the file
						if (firstRead)
						{

							byte[] header = null;

							if (encrypt)
							{
								byte[] prefix = Encoding.UTF8.GetBytes("ENCRYPTED_");
								byte[] headerData = MessageEncryption.EncryptStringToBytes(remoteSaveLocation);
								header = new byte[prefix.Length + headerData.Length];
								prefix.CopyTo(header, 0);
								headerData.CopyTo(header, 10);
							}
							else
							{
								header = Encoding.UTF8.GetBytes(remoteSaveLocation);
							}

							//Message
							byte[] messageData = message; //Message part
							byte[] headerBytes = header; //Header
							byte[] headerLen = BitConverter.GetBytes(headerBytes.Length); //Length of the header
							byte[] messageLength = BitConverter.GetBytes(stream.Length); //Total bytes in the file

							data = new byte[4 + 4 + headerBytes.Length + messageData.Length];

							messageLength.CopyTo(data, 0);
							headerLen.CopyTo(data, 4);
							headerBytes.CopyTo(data, 8);
							messageData.CopyTo(data, 8 + headerBytes.Length);

							firstRead = false;

						}
						else
						{
							data = message;
						}


						foreach (var key in clientIds)
						{
							SendBytesPartial(data, key);
						}

					}

				}

				//Delete encrypted file after it has been read.
				if (encrypt)
					File.Delete(file);

				return clientIds;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}


		protected async Task CreateAsyncFileMessageBroadcast(string fileLocation, string remoteSaveLocation, bool compressFile, bool encryptFile, bool close)
		{
			var file = Path.GetFullPath(fileLocation);

			IList<int> clients;

			//Compresses the file
			if (compressFile)
			{
				file = await CompressFileAsync(file);
				remoteSaveLocation += FileCompressor.Extension;
			}

			//Encrypts the file and deletes the compressed file
			if (encryptFile)
			{
				//Gets the location before the encryption

				string previousFile = string.Empty;
				if (compressFile)
					previousFile = file;

				file = await EncryptFileAsync(file);
				remoteSaveLocation += MessageEncryption.Extension;

				//Deletes the compressed file
				if (previousFile != string.Empty)
					File.Delete(previousFile);

			}

			clients = await StreamFileAndSendToAllClients(file, remoteSaveLocation, encryptFile);

			//Deletes the compressed file if no encryption occured.
			if (compressFile && !encryptFile)
				File.Delete(file);


			//Invoke completed for each client that should have received the file
			foreach (var client in clients)
			{
				FileTransferCompleted(close, client);
			}

		}

		protected async Task CreateAsyncFolderMessageBroadcast(string folderLocation, string remoteFolderLocation, bool encryptFolder,bool close)
		{

			IList<int> clients;

			//Gets a temp path for the zip file.
			string tempPath = Path.GetTempFileName();

			//Check if the current temp file exists, if so delete it.
			File.Delete(tempPath);

			//Add extension and compress.
			tempPath += FolderCompressor.Extension;
			string folderToSend = await CompressFolderAsync(folderLocation, tempPath);
			remoteFolderLocation += FolderCompressor.Extension;

			//Check if folder needs to be encrypted.
			if (encryptFolder)
			{
				//Encrypt and adjust file names.
				folderToSend = await EncryptFileAsync(folderToSend);
				remoteFolderLocation += MessageEncryption.Extension;
				File.Delete(tempPath);
			}

			clients = await StreamFileAndSendToAllClients(folderToSend, remoteFolderLocation, encryptFolder);

			//Deletes the compressed folder if not encryption occured.
			if (File.Exists(folderToSend))
				File.Delete(folderToSend);

			//Invoke completed for each client that should have received the file
			foreach (var client in clients)
			{
				FileTransferCompleted(close, client);
			}

		}

		#endregion

		#endregion

		#endregion

	}
}
