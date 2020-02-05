using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Client;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Compression.File;
using SimpleSockets.Messaging.Compression.Folder;
using SimpleSockets.Messaging.Compression.Stream;
using SimpleSockets.Messaging.Cryptography;
using SimpleSockets.Messaging.MessageContracts;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Messaging.Serialization;
using SimpleSockets.Server;

namespace SimpleSockets
{
	public abstract class SimpleSocket: IDisposable
	{

		#region Variables

		//--Protected
		#region Protected

		//CancellationToken used with Async Sockets
		protected CancellationTokenSource TokenSource { get; set; }
		protected CancellationToken Token { get; set; }

		//File Buffer
		protected int FileBuffer { get; set; }

		//Contains messages
		internal BlockingQueue<MessageWrapper> BlockingMessageQueue = new BlockingQueue<MessageWrapper>();

		//Message-queue
		//

		#endregion

		//--Private
		#region Private

		private string _tempPath;
		private IMessageEncryption _messageEncryption;
		private IFileCompression _fileCompressor;
		private IFolderCompression _folderCompressor;
		private IByteCompression _byteCompressor;
		private IObjectSerializer _objectSerializer;

		#endregion

		//--Public
		#region Public

		/// <summary>
		/// The socket will not accept files from other sockets when this value is False.
		/// Defaults to False.
		/// </summary>
		public bool AllowReceivingFiles { get; set; }

		/// <summary>
		/// When true additional messages will be logged.
		/// </summary>
		public bool Debug { get; set; }

		/// <summary>
		/// Get the port used to start the server
		/// </summary>
		public int Port { get; protected set; }

		/// <summary>
		/// Get the ip on which the server is running
		/// </summary>
		public string Ip { get; protected set; }

		/// <summary>
		/// Indicates if the server or client is running and accepting connections.
		/// </summary>
		public bool IsRunning { get; protected set; }

		/// <summary>
		/// Gets if there are still messages pending for transmission.
		/// </summary>
		public bool PendingMessages => (BlockingMessageQueue.Count > 0);

		/// <summary>
		/// The path where files will be stored for extraction, compression, encryption and decryption.
		/// if the value is invalid or none is entered it defaults to %TEMP%
		/// </summary>
		public string TempPath
		{
			get => string.IsNullOrEmpty(_tempPath) ? Path.GetTempPath() : _tempPath;
			set
			{
				try
				{
					var temp = new FileInfo(value);
					if (temp.Directory != null)
					{
						_tempPath = temp.Directory.FullName + Path.DirectorySeparatorChar;
						if (!Directory.Exists(_tempPath))
							Directory.CreateDirectory(_tempPath);

						return;
					}

					throw new ArgumentException("'" + value + "' is an invalid path.");
				}
				catch (Exception ex)
				{
					throw new Exception(ex.Message, ex);
				}
			}
		}

		/// <summary>
		/// Used to compress/decompress bytes before sending
		/// </summary>
		public IByteCompression ByteCompressor
		{
			protected internal get => _byteCompressor;
			set
			{
				if (IsRunning)
					throw new Exception("The byte compressor cannot be changed while the socket is running.");

				_byteCompressor = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		/// <summary>
		/// Used to compress files before sending
		/// </summary>
		public IFileCompression FileCompressor
		{
			protected internal get => _fileCompressor;
			set
			{
				if (IsRunning)
					throw new Exception("The file compressor cannot be changed while the socket is running.");

				if (string.IsNullOrEmpty(value.Extension))
					throw new ArgumentNullException(nameof(value));

				if (!value.Extension.StartsWith("."))
					throw new Exception("An extension has to begin with a '.'");

				_fileCompressor = value;
			}

		}

		/// <summary>
		/// Used to encrypt files/folders
		/// </summary>
		public IMessageEncryption MessageEncryption
		{
			protected internal get => _messageEncryption;
			set
			{
				if (IsRunning)
					throw new Exception("The Encryption cannot be changed while the socket is running.");

				if (string.IsNullOrEmpty(value.Extension))
					throw new ArgumentNullException(nameof(value));

				if (!value.Extension.StartsWith("."))
					throw new Exception("An extension has to begin with a '.'");

				_messageEncryption = value;
			}
		}

		/// <summary>
		/// Used to compress folder before sending
		/// </summary>
		public IFolderCompression FolderCompressor
		{
			protected internal get => _folderCompressor;
			set
			{
				if (IsRunning)
					throw new Exception("The Folder compressor cannot be changed while the socket is running.");

				if (string.IsNullOrEmpty(value.Extension))
					throw new ArgumentNullException(nameof(value));

				if (!value.Extension.StartsWith("."))
					throw new Exception("An extension has to begin with a '.'");

				_folderCompressor = value;
			}
		}

		/// <summary>
		/// Class used to Serialize and deserialize objects.
		/// </summary>
		public IObjectSerializer ObjectSerializer
		{
			get => _objectSerializer;
			set
			{
				if (IsRunning)
					throw new Exception("Cannot change the ObjectSerializer when the socket is running.");

				_objectSerializer = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		#endregion

		//--Internal
		#region Internal
		/// <summary>
		/// MessageContracts
		/// </summary>
		internal IDictionary<string, IMessageContract> MessageContracts { get; }
		#endregion

		#endregion

		#region Methods

		#region Public-Methods
		
		/// <summary>
		/// Disposes of the socket
		/// </summary>
		public abstract void Dispose();

		public static SimpleSocketListener CreateServer()
		{
			return new SimpleSocketTcpListener();
		}

		public static SimpleSocketClient CreateClient()
		{
			return new SimpleSocketTcpClient();
		}

		/// <summary>
		/// Creates a new Ssl Listener
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="tlsProtocol"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		/// <returns></returns>
		public static SimpleSocketTcpSslListener CreateSslServer(X509Certificate2 certificate, TlsProtocol tlsProtocol = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false)
		{
			return new SimpleSocketTcpSslListener(certificate, tlsProtocol, acceptInvalidCertificates, mutualAuth);
		}

		/// <summary>
		/// Creates a new Ssl Listener
		/// </summary>
		/// <param name="certPath"></param>
		/// <param name="certPass"></param>
		/// <param name="tlsProtocol"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		/// <returns></returns>
		public static SimpleSocketTcpSslListener CreateSslServer(string certPath, string certPass, TlsProtocol tlsProtocol = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false)
		{
			return new SimpleSocketTcpSslListener(certPath, certPass, tlsProtocol, acceptInvalidCertificates, mutualAuth);
		}

		/// <summary>
		/// Creates a new Ssl Client
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="tlsProtocol"></param>
		/// <param name="acceptInvalidCertificates"></param>
		/// <param name="mutualAuth"></param>
		/// <returns></returns>
		public static SimpleSocketTcpSslClient CreateSslClient(X509Certificate2 certificate, TlsProtocol tlsProtocol = TlsProtocol.Tls12, bool acceptInvalidCertificates = true, bool mutualAuth = false)
		{
			return new SimpleSocketTcpSslClient(certificate, tlsProtocol, acceptInvalidCertificates, mutualAuth);
		}

		/// <summary>
		/// Creates a new Ssl Client
		/// </summary>
		/// <param name="certPath"></param>
		/// <param name="certPass"></param>
		/// <param name="tlsProtocol"></param>
		/// <param name="acceptInvalidCerts"></param>
		/// <param name="mutualAuth"></param>
		/// <returns></returns>
		public static SimpleSocketTcpSslListener CreateSslClient(string certPath, string certPass, TlsProtocol tlsProtocol = TlsProtocol.Tls12, bool acceptInvalidCerts = true, bool mutualAuth = false)
		{
			return new SimpleSocketTcpSslListener(certPath,certPass,tlsProtocol,acceptInvalidCerts,mutualAuth);
		}
		
		//MessageContract
		#region IMessageContract


		/// <summary>
		/// Adds a new MessageContract Handler to the socket.
		/// Remember: you have to add the IMessageContract to the Server and Client if you want to use it.
		/// </summary>
		/// <param name="contract"></param>
		public void AddMessageContract(IMessageContract contract)
		{
			if (string.IsNullOrEmpty(contract.MessageHeader))
				throw new ArgumentNullException(contract.MessageHeader);

			var contractExists = MessageContracts.TryGetValue(contract.MessageHeader, out var c);

			if (contractExists)
				throw new ArgumentException("A contract with the header '" + contract.MessageHeader + "' already exists. The MessageHeader has to be unique.");

			MessageContracts.Add(contract.MessageHeader, contract);

		}

		/// <summary>
		/// Gets an existing MessageContract
		/// </summary>
		/// <param name="contractHeader"></param>
		/// <returns></returns>
		public IMessageContract GetMessageContract(string contractHeader)
		{
			if (string.IsNullOrEmpty(contractHeader))
				throw new ArgumentNullException(contractHeader);

			var exists = MessageContracts.TryGetValue(contractHeader, out var contract);

			if (exists) return contract;

			throw new ArgumentException("There does not exist a contract with the header '" + contractHeader + "'", nameof(contractHeader));
		}

		/// <summary>
		/// Tries to get an existing MessageContract.
		/// Returns False if none is found.
		/// </summary>
		/// <param name="contractHeader"></param>
		/// <param name="contract"></param>
		/// <returns></returns>
		public bool TryGetMessageContract(string contractHeader, out IMessageContract contract)
		{
			return MessageContracts.TryGetValue(contractHeader, out contract);
		}

		/// <summary>
		/// Tries to remove a MessageContract.
		/// Returns True if contract is removed.
		/// </summary>
		/// <param name="contractHeader"></param>
		/// <returns></returns>
		public bool RemoveMessageContract(string contractHeader)
		{
			return MessageContracts.Remove(contractHeader);
		}

		#endregion

		/// <summary>
		/// Change the buffer size of the server.
		/// </summary>
		/// <param name="bufferSize"></param>
		public void ChangeSocketBufferSize(int bufferSize)
		{
			if (bufferSize < 1024)
				throw new ArgumentException("The buffer size cannot be less then 1024 bytes.");
			if (bufferSize >= 85000)
				RaiseLog("A buffer size larger then 85 000 bytes will allocate bytes to Large Object Heap. This will cause higher memory usage.");

			ClientMetadata.ChangeBufferSize(bufferSize);
		}

		#endregion

		/// <summary>
		/// Start receiving bytes from server
		/// </summary>
		/// <param name="state"></param>
		/// <param name="offset"></param>
		protected internal abstract void Receive(IClientMetadata state, int offset = 0);

		//Handles messages the server receives
		protected abstract void ReceiveCallback(IAsyncResult result);

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		protected SimpleSocket()
		{
			IsRunning = false;
			AllowReceivingFiles = false;
			Debug = true;
			FileBuffer = 4096;
			MessageContracts = new Dictionary<string, IMessageContract>();

			ObjectSerializer = new JsonSerialization();
			ByteCompressor = new DeflateByteCompression();
			MessageEncryption = new Aes256();
			FileCompressor = new GZipCompression();
			FolderCompressor = new ZipCompression();
		}

		#endregion

		#region Cryptography/Compression

		#region Compression

		#region Bytes

		protected internal byte[] CompressBytes(byte[] plainText)
		{
			return ByteCompressor.CompressBytes(plainText);
		}

		protected internal byte[] DecompressBytes(byte[] cipherText)
		{
			return ByteCompressor.DecompressBytes(cipherText);
		}

		protected internal async Task<byte[]> CompressBytesAsync(byte[] plainBytes)
		{
			return await Task.Run(() => CompressBytes(plainBytes), Token);
		}

		protected internal async Task<byte[]> DecompressBytesAsync(byte[] cipherBytes)
		{
			return await Task.Run(() => DecompressBytes(cipherBytes), Token);
		}

		#endregion

		#region File

		protected internal FileInfo CompressFile(FileInfo file)
		{
			return FileCompressor.Compress(file, new FileInfo(TempPath + Path.GetRandomFileName() + FileCompressor.Extension));
		}

		protected internal FileInfo DecompressFile(FileInfo file, string outputPath)
		{
			var output = new FileInfo(outputPath);
			return FileCompressor.Decompress(file, output);
		}

		protected internal async Task<FileInfo> CompressFileAsync(FileInfo file)
		{
			return await Task.Run(() => CompressFile(file), Token);
		}

		protected internal async Task<FileInfo> DecompressFileAsync(FileInfo file, string outputPath)
		{
			return await Task.Run(() => DecompressFile(file, outputPath), Token);
		}

		#endregion

		#region Folder

		protected internal FileInfo CompressFolder(string sourceDir)
		{
			return FolderCompressor.Compress(sourceDir, TempPath + Path.GetRandomFileName() + FolderCompressor.Extension);
		}

		protected internal string ExtractToFolder(string zip, string targetDir)
		{
			return FolderCompressor.Extract(zip, targetDir);
		}

		internal async Task<FileInfo> CompressFolderAsync(string sourceDir)
		{
			return await Task.Run(() => CompressFolder(sourceDir), Token);
		}

		internal async Task<string> ExtractToFolderAsync(string zip, string targetDir)
		{
			return await Task.Run(() => ExtractToFolder(zip, targetDir), Token);
		}

		#endregion

		#endregion

		#region Cryptography

		#region Bytes

		protected internal byte[] EncryptBytes(byte[] plainText)
		{
			return MessageEncryption.EncryptBytes(plainText);
		}

		protected internal byte[] DecryptBytes(byte[] cipherText)
		{
			return MessageEncryption.DecryptBytes(cipherText);
		}

		protected internal async Task<byte[]> EncryptBytesAsync(byte[] plainText)
		{
			return await Task.Run(() => EncryptBytes(plainText), Token);
		}

		protected internal async Task<byte[]> DecryptBytesAsync(byte[] cipherText)
		{
			return await Task.Run(() => DecryptBytes(cipherText), Token);
		}

		#endregion
		
		#region File

		protected internal FileInfo EncryptFile(string input)
		{
			return MessageEncryption.FileEncrypt(input, TempPath + Path.GetRandomFileName() + MessageEncryption.Extension);
		}

		protected internal FileInfo DecryptFile(string input, string output)
		{
			return MessageEncryption.FileDecrypt(input, output);
		}

		protected internal async Task<FileInfo> EncryptFileAsync(string input)
		{
			return await Task.Run(() => EncryptFile(input), Token);
		}

		protected internal async Task<FileInfo> DecryptFileAsync(string input, string output)
		{
			return await Task.Run(() => DecryptFile(input, output), Token);
		}

		#endregion

		#endregion

		#endregion

		#region Event-Raisers

		protected internal abstract void RaiseMessageReceived(IClientInfo clientInfo, string message);

		protected internal abstract void RaiseMessageContractReceived(IClientInfo clientInfo, IMessageContract contract, byte[] data);

		protected internal abstract void RaiseMessageWithMetaDataReceived(IClientInfo clientInfo, object message, IDictionary<object,object> metadata);

		protected internal abstract void RaiseBytesReceived(IClientInfo clientInfo, byte[] data);

		protected internal abstract void RaiseFileReceiver(IClientInfo clientInfo, int currentPart, int totalParts, string partPath, MessageState status);

		protected internal abstract void RaiseFolderReceiver(IClientInfo clientInfo, int currentPart, int totalParts, string partPath, MessageState status);

		protected internal abstract void RaiseObjectReceived(IClientInfo clientInfo, object obj, Type objectType);

		protected internal abstract void RaiseMessageUpdateStateFileTransfer(IClientInfo clientInfo, string origin,string remoteSaveLoc,double percentageDone, MessageState state);

		protected internal abstract void RaiseMessageUpdate(IClientInfo clientInfo, string msg, string header, MessageType msgType,MessageState state);

		protected internal abstract void RaiseMessageFailed(IClientInfo clientInfo, byte[] payLoad, Exception ex);

		protected internal abstract void RaiseLog(string message);

		protected internal abstract void RaiseLog(Exception ex);

		protected internal abstract void RaiseErrorThrown(Exception ex);

		#endregion

		#region Debugging

		protected internal void Log(string log)
		{
			RaiseLog(log);
			//Console.WriteLine(log);
		}

		protected internal void Log(Exception ex)
		{
			RaiseLog(ex);
			//Console.WriteLine(ex.Message);
		}

		#endregion

		#region Send-Data

		/// <summary>
		/// Sends Data from client -> server or uses the ID to send from server -> specific client
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="close"></param>
		/// <param name="partial"></param>
		/// <param name="id"></param>
		protected abstract void SendToSocket(byte[] bytes, bool close, bool partial = false, int id = -1);

		/// <summary>
		/// Uses a FileStream with a buffer to send a file, this way there won't be a lot of memory usage.
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encrypt"></param>
		/// <param name="compress"></param>
		/// <param name="close"></param>
		/// <param name="msgType"></param>
		/// <param name="client"></param>
		/// <returns></returns>
		protected async Task StreamFileFolderAsync(string fileLocation, string remoteSaveLocation, bool encrypt, bool compress, bool close, MessageType msgType, IClientInfo client = null)
		{
			try
			{
				
				RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.Beginning);
				Log("Beginning sending file from path: " + fileLocation);

				var fileInfo = new FileInfo(Path.GetFullPath(fileLocation));
				var msgBuilder = new SimpleMessage(msgType, this, Debug)
					.CompressMessage(compress)
					.EncryptMessage(encrypt)
					.SetHeaderString(remoteSaveLocation)
					.SetSendClient(client);

				if (compress || msgType == MessageType.Folder)
				{
					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0,MessageState.Compressing);
					Log("Compressing file from path: " + fileLocation + " to TempPath: " + TempPath);

					if (msgType == MessageType.Folder)
					{
						fileInfo = await CompressFolderAsync(fileInfo.FullName);
					}
					else if (msgType == MessageType.File)
					{
						fileInfo = await CompressFileAsync(fileInfo);
					}

					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.CompressingDone);
					Log("File/Folder compressed to TempPath: " + TempPath);

				}

				if (encrypt)
				{
					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.Encrypting);
					Log("Encrypting file to TempPath: " + TempPath);

					var prev = fileInfo.FullName;
					fileInfo = await EncryptFileAsync(fileInfo.FullName);

					if (compress || msgType == MessageType.Folder)
					{
						File.Delete(prev); //Clean compressed file
						Log("Deleting compressed file at path: " + prev);
					}

					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.EncryptingDone);
					Log("File/Folder compressed to TempPath: " + TempPath);
				}


				//Vars
				var file = fileInfo.FullName;
				var fileStreamBuffer = new byte[4096]; //When this buffer exceeds 85000 bytes -> buffer will be stored in LOH -> bad for memory usage.
				var buffer = FileBuffer; //10 Mb buffer

				MemoryStream memoryStream = null;

				using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, fileStreamBuffer.Length, true))
				{
					var read = 0;
					var currentPart = 0;
					var totalLength = fileStream.Length;
					int totalParts = (int)Math.Ceiling((double)(totalLength / buffer));

					msgBuilder = msgBuilder
						.SetTotalParts(totalParts);

					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.Transmitting);

					while ((read = await fileStream.ReadAsync(fileStreamBuffer, 0, fileStreamBuffer.Length, Token)) > 0)
					{
						totalLength -= read;
						var data = new byte[read];
						if (read == fileStreamBuffer.Length)
							data = fileStreamBuffer;
						else
							Array.Copy(fileStreamBuffer, 0, data, 0, read);

						if (memoryStream == null)
							memoryStream = new MemoryStream();

						memoryStream.Write(data, 0, data.Length);

						if (memoryStream.Length == buffer)
						{
							await msgBuilder
								.SetPartNumber(currentPart)
								.SetBytes(memoryStream.ToArray())
								.SetHeaderString(remoteSaveLocation)
								.BuildAsync();

							currentPart++;
							SendToSocket(msgBuilder.PayLoad, false, totalLength != 0, client?.Id ?? -1);
							Log("Sending part " + currentPart + " of a total of " + totalParts + " of file/folder: " + fileLocation);

							memoryStream.Close();
							memoryStream = null;
						}
					}

					if (memoryStream != null && memoryStream.Length > 0)
					{
						await msgBuilder
							.SetPartNumber(currentPart)
							.SetBytes(memoryStream.ToArray())
							.SetHeaderString(remoteSaveLocation)
							.BuildAsync();

						SendToSocket(msgBuilder.PayLoad, false, false, client?.Id ?? -1);
						Log("Sending part " + currentPart + " of a total of " + totalParts + " of file/folder: " + fileLocation);
						memoryStream.Close();
						memoryStream = null;
					}

				}

				if (compress || encrypt || msgType == MessageType.Folder)
				{
					File.Delete(file);
					Log("Deleting temp file at location " + file);

				}
					

				RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 100, MessageState.Completed);
			}
			catch (Exception ex)
			{
				Log(ex);
				RaiseErrorThrown(ex);
			}
		}

		/// <summary>
		/// Uses a FileStream with a buffer to send a file, this way there won't be a lot of memory usage.
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encrypt"></param>
		/// <param name="compress"></param>
		/// <param name="close"></param>
		/// <param name="msgType"></param>
		/// <param name="client"></param>
		protected void StreamFileFolder(string fileLocation, string remoteSaveLocation, bool encrypt, bool compress, bool close, MessageType msgType, IClientInfo client = null)
		{
			try
			{
				RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.Beginning);
				Log("Beginning sending file from path: " + fileLocation);

				var fileInfo = new FileInfo(Path.GetFullPath(fileLocation));
				var msgBuilder = new SimpleMessage(msgType, this, Debug)
					.CompressMessage(compress)
					.EncryptMessage(encrypt)
					.SetHeaderString(remoteSaveLocation)
					.SetSendClient(client);

				if (compress || msgType == MessageType.Folder)
				{
					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.Compressing);
					Log("Compressing file from path: " + fileLocation + " to TempPath: " + TempPath);

					if (msgType == MessageType.Folder)
					{
						fileInfo = CompressFolder(fileInfo.FullName);
					}
					else if (msgType == MessageType.File)
					{
						fileInfo = CompressFile(fileInfo);
					}

					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.CompressingDone);
					Log("File/Folder compressed to TempPath: " + TempPath);

				}

				if (encrypt)
				{
					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.Encrypting);
					Log("Encrypting file to TempPath: " + TempPath);

					var prev = fileInfo.FullName;
					fileInfo = EncryptFile(fileInfo.FullName);

					if (compress || msgType == MessageType.Folder)
					{
						File.Delete(prev); //Clean compressed file
						Log("Deleting compressed file at path: " + prev);
					}

					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.EncryptingDone);
					Log("File/Folder compressed to TempPath: " + TempPath);
				}


				//Vars
				var file = fileInfo.FullName;
				var fileStreamBuffer = new byte[4096]; //When this buffer exceeds 85000 bytes -> buffer will be stored in LOH -> bad for memory usage.
				var buffer = FileBuffer; //10 Mb buffer

				MemoryStream memoryStream = null;

				using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, fileStreamBuffer.Length, true))
				{
					var read = 0;
					var currentPart = 0;
					var totalLength = fileStream.Length;
					int totalParts = (int)Math.Ceiling((double)(totalLength / buffer));

					msgBuilder = msgBuilder
						.SetTotalParts(totalParts);

					RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.Transmitting);

					while ((read = fileStream.Read(fileStreamBuffer, 0, fileStreamBuffer.Length)) > 0)
					{
						totalLength -= read;
						var data = new byte[read];
						if (read == fileStreamBuffer.Length)
							data = fileStreamBuffer;
						else
							Array.Copy(fileStreamBuffer, 0, data, 0, read);

						if (memoryStream == null)
							memoryStream = new MemoryStream();

						memoryStream.Write(data, 0, data.Length);

						if (memoryStream.Length == buffer)
						{
							msgBuilder
								.SetPartNumber(currentPart)
								.SetBytes(memoryStream.ToArray())
								.SetHeaderString(remoteSaveLocation)
								.Build();

							currentPart++;
							SendToSocket(msgBuilder.PayLoad, false, totalLength != 0, client?.Id ?? -1);
							Log("Sending part " + currentPart + " of a total of " + totalParts + " of file/folder: " + fileLocation);

							memoryStream.Close();
							memoryStream = null;
						}
					}

					if (memoryStream != null && memoryStream.Length > 0)
					{
						msgBuilder
							.SetPartNumber(currentPart)
							.SetBytes(memoryStream.ToArray())
							.SetHeaderString(remoteSaveLocation)
							.Build();

						SendToSocket(msgBuilder.PayLoad, false, false, client?.Id ?? -1);
						Log("Sending part " + currentPart + " of a total of " + totalParts + " of file/folder: " + fileLocation);
						memoryStream.Close();
						memoryStream = null;
					}

				}

				if (compress || encrypt || msgType == MessageType.Folder)
				{
					File.Delete(file);
					Log("Deleting temp file at location " + file);

				}


				RaiseMessageUpdateStateFileTransfer(client, fileLocation, remoteSaveLocation, 0, MessageState.Completed);
			}
			catch (Exception ex)
			{
				Log(ex);
				RaiseErrorThrown(ex);
			}
		}

		#endregion


	}
}
