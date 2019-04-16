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
using System.Windows.Forms;
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
	public delegate void ClientConnectedHandler(int id, IStateObject clientState);

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

	//Calls the SendBytesAsync Method.
	public delegate void AsyncCallerBroadcast(bool close);

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

			Aes256 = new AES256();
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
		protected IStateObject GetClient(int id)
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
		internal abstract void StartReceiving(IStateObject state, int offset = 0);

		#region Invokes

		protected void ClientDisconnectedInvoke(int id)
		{
			ClientDisconnected?.Invoke(id);
		}

		protected void ClientConnectedInvoke(int id, IStateObject clientState)
		{
			ClientConnected?.Invoke(id,clientState);
		}

		protected void ServerHasStartedInvoke()
		{
			ServerHasStarted?.Invoke();
		}

		/// <summary>
		/// Invokes FileReceived event
		/// </summary>
		/// <param name="id"></param>
		/// <param name="filePath"></param>
		internal void InvokeFileReceived(int id, string filePath)
		{
			FileReceived?.Invoke(id, filePath);
		}

		/// <summary>
		/// Invokes ProgressReceived event
		/// </summary>
		/// <param name="id"></param>
		/// <param name="bytesReceived"></param>
		/// <param name="messageSize"></param>
		internal void InvokeFileTransferProgress(int id, int bytesReceived, int messageSize)
		{
			ProgressFileReceived?.Invoke(id, bytesReceived, messageSize);
		}

		protected void InvokeMessageSubmitted(int id, bool close)
		{
			MessageSubmitted?.Invoke(id,close);
		}

		/// <summary>
		/// Invokes MessageReceived event of the server.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="header"></param>
		/// <param name="text"></param>
		internal void InvokeMessageReceived(int id, string header, string text)
		{
			MessageReceived?.Invoke(id, header, text);
		}

		#endregion

		//Handles messages
		protected abstract void HandleMessage(IAsyncResult result);

		protected override async Task SendFileAsynchronous(string location, string remoteSaveLocation, bool encrypt,bool close, int id = -1)
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
				await BeginSendFile(location, remoteSaveLocation, encrypt, close, FileTransferCompletedCallBack, id);
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


		private void FileTransferCompletedCallBack(bool close,int id)
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

		//End the send and invoke MessageSubmitted event.
		protected abstract void SendCallback(IAsyncResult result);

		//End the send and invoke MessageSubmitted event.
		protected abstract void FileTransferPartialCallback(IAsyncResult result);

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

		#region Broadcasts

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public override void SendMessageToAllClients(string message, bool encryptMessage, bool close)
		{
			var dataBytes = CreateByteMessage(message, encryptMessage);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}

		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public override void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile, bool close)
		{
			var dataBytes = CreateByteFile(fileLocation, remoteSaveLocation, encryptFile, compressFile);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}
		}


		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients asynchronous
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public override async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile,bool close)
		{
			try
			{
				await CreateAsyncFileMessageBroadcast(fileLocation, remoteSaveLocation, compressFile, encryptFile,
					close);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public override void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			var dataBytes = CreateByteFolder(folderLocation, remoteFolderLocation, true);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public override async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			try
			{
				await CreateAsyncFolderMessageBroadcast(folderLocation, remoteFolderLocation, encryptFolder, close);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public override void SendObjectToAllClients(object obj, bool encryptObject, bool close)
		{
			var dataBytes = CreateByteObject(obj, encryptObject);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to all connected clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public override void SendCommandToAllClients(string command, bool encryptCommand, bool close)
		{
			var dataBytes = CreateByteCommand(command, encryptCommand);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}
		}

		#region Broadcast File/folder
		private async Task BeginSendFileBroadcast(string location, string remoteSaveLocation, bool encrypt, bool close, AsyncCallerFile callback)
		{

			List<int> clients = await StreamFileAndSendToAllClients(location, remoteSaveLocation, encrypt);

			foreach (var key in clients)
			{
				callback.Invoke(close, key);
			}

		}

		private async Task SendFileAsyncBroadcast(string location, string remoteSaveLocation, bool encrypt, bool close)
		{

			try
			{
				await BeginSendFileBroadcast(location, remoteSaveLocation, encrypt, close, FileTransferCompletedCallBack);
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
					clientIds.Add(c.Key);
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
								byte[] headerData = AES256.EncryptStringToBytes_Aes(remoteSaveLocation);
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
							SendBytesOfFile(data, key);
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


		protected async Task CreateAsyncFileMessageBroadcast(string fileLocation, string remoteSaveLocation,
			bool compressFile, bool encryptFile, bool close)
		{
			var file = Path.GetFullPath(fileLocation);

			//Compresses the file
			if (compressFile)
			{
				file = await CompressFileAsync(file);
				remoteSaveLocation += GZipCompression.Extension;
			}

			//Encrypts the file and deletes the compressed file
			if (encryptFile)
			{
				//Gets the location before the encryption

				string previousFile = string.Empty;
				if (compressFile)
					previousFile = file;

				file = await EncryptFileAsync(file);
				remoteSaveLocation += AES256.Extension;

				//Deletes the compressed file
				if (previousFile != string.Empty)
					File.Delete(previousFile);

			}

			await SendFileAsyncBroadcast(file, remoteSaveLocation, encryptFile, close);
			//await StreamFileAndSendBytes(file, remoteSaveLocation, encryptFile, close, id);

			//Deletes the compressed file if no encryption occured.
			if (compressFile && !encryptFile)
				File.Delete(file);

		}

		protected async Task CreateAsyncFolderMessageBroadcast(string folderLocation, string remoteFolderLocation,
			bool encryptFolder, bool close)
		{
			//Gets a temp path for the zip file.
			string tempPath = Path.GetTempFileName();

			//Check if the current temp file exists, if so delete it.
			File.Delete(tempPath);

			//Add extension and compress.
			tempPath += ZipCompression.Extension;
			string folderToSend = await CompressFolderAsync(folderLocation, tempPath);
			remoteFolderLocation += ZipCompression.Extension;

			//Check if folder needs to be encrypted.
			if (encryptFolder)
			{
				//Encrypt and adjust file names.
				folderToSend = await EncryptFileAsync(folderToSend);
				remoteFolderLocation += AES256.Extension;
				File.Delete(tempPath);
			}

			await SendFileAsyncBroadcast(folderToSend, remoteFolderLocation, encryptFolder, close);

			//Stream the file in bits and send each time the buffer is full.
			//await StreamFileAndSendBytes(folderToSend, remoteFolderLocation, encryptFolder, close, id);

			//Deletes the compressed folder if not encryption occured.
			if (File.Exists(folderToSend))
				File.Delete(folderToSend);
		}
		#endregion

		#endregion

	}
}
