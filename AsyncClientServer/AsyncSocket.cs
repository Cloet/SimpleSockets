using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncClientServer.Messaging;
using AsyncClientServer.Messaging.Compression;
using AsyncClientServer.Messaging.Cryptography;
using AsyncClientServer.Messaging.Metadata;

namespace AsyncClientServer
{
	/// <summary>
	/// Abstract class used to send data to server/client
	/// <para>Used to set different types of headers.</para>
	/// <para>The byte array consists of:
	/// 1. HeaderLength
	/// 2. Header
	/// 3. MessageLength
	/// 4. Message
	/// </para>
	/// <para>Check CreateByteArray method for more info.</para>
	/// </summary>
	public abstract class AsyncSocket: IDisposable
	{

		#region MessageCreation

		//Triggered when file/folder is done sending.
		protected abstract void FileTransferCompleted(bool close, int id);

		//Gets called to send parts of the file
		protected abstract void SendBytesPartial(byte[] bytes, int id);

		// This method Sends a file asynchronous.
		// It checks if the file has to be compressed and/or encrypted before being sent.
		// It reads the file with a default buffer of 10Mb. Whenever a part of the buffer is read it will send bytes.
		protected async Task CreateAndSendAsyncFileMessage(string fileLocation, string remoteSaveLocation, bool compressFile, bool encryptFile, bool close, int id = -1)
		{

			try
			{
				var file = Path.GetFullPath(fileLocation);

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

				//await SendFileAsynchronous(file, remoteSaveLocation, encryptFile, close, id);
				await StreamFileAndSendBytesAsync(file, remoteSaveLocation, encryptFile, id);

				//Deletes the compressed file if no encryption occured.
				if (compressFile && !encryptFile)
					File.Delete(file);

				FileTransferCompleted(close, id);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		// This methods Sends a folder asynchronous.
		// It checks if the folder has to be encrypted before being sent.
		// The folder will always be compressed as a .ZIP file to make transfer easier.
		protected async Task CreateAndSendAsyncFolderMessage(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close, int id = -1)
		{

			try
			{
				//Gets a temp path for the zip file.
				string tempPath = TempPath + Path.GetFileName(Path.GetTempFileName());

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

				await StreamFileAndSendBytesAsync(folderToSend, remoteFolderLocation, encryptFolder, id);

				//Deletes the compressed folder if not encryption occured.
				if (File.Exists(folderToSend))
					File.Delete(folderToSend);

				FileTransferCompleted(close, id);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Streams the file and constantly sends bytes to server or client.
		//This method is called in createAsyncFileMessage.
		//Id is an optional parameter with default value of -1)
		protected async Task StreamFileAndSendBytesAsync(string location, string remoteSaveLocation, bool encrypt, int id = -1)
		{
			try
			{
				var file = location;
				var buffer = new byte[4096]; //Any buffer bigger then 85 000 bytes get allocated in LOH => bad for memory usage!
				//var buffer = new byte[10485760]; //10 MB buffer
				bool firstRead = true;
				MemoryStream memStream = null;

				//Stream that reads the file and sends bits to the server.
				using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length,true))
				{
					//How much bytes that have been read
					int read = 0;

					while ((read = await stream.ReadAsync(buffer, 0, buffer.Length,Token)) > 0)
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

						//Use memorystream as temp buffer
						//otherwise there will be too much separate message sent.
						//And if buffer is to big the data will end up in the LOH.
						if (memStream == null)
						{
							memStream = new MemoryStream();
						}

						memStream.Write(data, 0, data.Length);
						

						if (memStream.Length >= buffer.Length * 2560)
						{
							SendBytesPartial(memStream.ToArray(), id);
							memStream.Close();
							memStream = null;
						}
					}

				}

				if (memStream != null)
				{
					SendBytesPartial(memStream.ToArray(), id);
					memStream.Close();
					memStream = null;
				}



				//Delete encrypted file after it has been read.
				if (encrypt)
					File.Delete(file);


			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		
		#region Byte Array Creation

		//Writes a message to byte array
		private byte[] CreateByteArray(string message, string header, bool encrypt)
		{
			try
			{
				byte[] messageArray = null;
				byte[] headerArray = null;

				if (encrypt)
				{
					//Header
					byte[] encryptedPrefix = Encoding.UTF8.GetBytes("ENCRYPTED_");
					byte[] encryptedHeader = MessageEncryption.EncryptStringToBytes(header);

					headerArray = new byte[encryptedHeader.Length + encryptedPrefix.Length];

					encryptedPrefix.CopyTo(headerArray, 0);
					encryptedHeader.CopyTo(headerArray, 10);

					messageArray = MessageEncryption.EncryptStringToBytes(message);
				}
				else
				{
					headerArray = Encoding.UTF8.GetBytes(header);
					messageArray = Encoding.UTF8.GetBytes(message);
				}


				//Message
				byte[] messageData = messageArray;
				byte[] headerBytes = headerArray;
				byte[] headerLen = BitConverter.GetBytes(headerBytes.Length);
				byte[] messageLength = BitConverter.GetBytes(messageData.Length);


				var data = new byte[4 + 4 + headerBytes.Length + messageData.Length];

				messageLength.CopyTo(data, 0);
				headerLen.CopyTo(data, 4);
				headerBytes.CopyTo(data, 8);
				messageData.CopyTo(data, 8 + headerBytes.Length);

				return data;

			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
		}

		/// <summary>
		/// Creates an array of bytes
		/// <para>This methods converts a simple message to an byte array.
		/// This way it can be send using sockets</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage">True if the message has to be encrypted.</param>
		/// <returns>Byte[]</returns>
		protected byte[] CreateByteMessage(string message, bool encryptMessage)
		{
			return CreateByteArray(message, "MESSAGE", encryptMessage);
		}

		/// <summary>
		/// Creates an array of bytes
		/// <para>You can use your own custom header for this message.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <returns></returns>
		protected byte[] CreateByteCustomHeader(string message, string header, bool encrypt)
		{
			return CreateByteArray(message, "<h>" + header + "</h>", encrypt);
		}


		#endregion

		#endregion

		#region Encryption/Compression


		//Encrypts a file and returns the new file path.
		protected async Task<string> EncryptFileAsync(string path)
		{
			try
			{
				return await Task.Run(() =>
				{
					MessageEncryption.FileEncrypt(Path.GetFullPath(path));
					path += MessageEncryption.Extension;
					return path;
				}, Token);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Compresses a file and returns the new path
		protected async Task<string> CompressFileAsync(string path)
		{
			try
			{
				return await Task.Run(() => 
					FileCompressor.Compress(new FileInfo(Path.GetFullPath(path)), TempPath).FullName
					, Token);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Compresses a folder and returns the new path
		protected async Task<string> CompressFolderAsync(string path, string tempPath)
		{
			try
			{
				return await Task.Run(() =>
				{
					FolderCompressor.Compress(Path.GetFullPath(path), Path.GetFullPath(tempPath));
					return tempPath;
				}, Token);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		#endregion

		#region Vars

		//Cancellationtokens
		protected CancellationTokenSource TokenSource { get; set; }
		protected CancellationToken Token { get; set; }

		//Contains messages
		protected BlockingQueue<Message> BlockingMessageQueue = new BlockingQueue<Message>();

		private string _tempPath;
		private MessageEncryption _messageEncryption;
		private FileCompression _fileCompressor;
		private FolderCompression _folderCompressor;

		/// <summary>
		/// The socket will not accept files from other sockets when this value is False.
		/// Defaults to False.
		/// </summary>
		public bool AllowReceivingFiles { get; set; }

		/// <summary>
		/// The path where files will be stored for extraction, compression, encryption end decryption.
		/// if the given value is invalid it will default to %TEMP%
		/// </summary>
		public string TempPath {
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

					throw new ArgumentException("Invalid path was given.");

				}
				catch (Exception ex)
				{
					throw new ArgumentException(ex.Message, TempPath, ex);
				}
			}

		}

		/// <summary>
		/// Used to compress files before sending
		/// </summary>
		public FileCompression FileCompressor
		{
			internal get => _fileCompressor;
			set
			{
				if (IsRunning)
					throw new Exception("The file compressor cannot be changed while the socket is running.");

				_fileCompressor = value ?? throw new ArgumentNullException(nameof(value));
			}

		}

		/// <summary>
		/// Used to encrypt files/folders
		/// </summary>
		public MessageEncryption MessageEncryption
		{
			internal get => _messageEncryption;
			set
			{
				if (IsRunning)
					throw new Exception("The Encrypter cannot be changed while the socket is running.");

				_messageEncryption = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		/// <summary>
		/// Used to compress folder before sending
		/// </summary>
		public FolderCompression FolderCompressor
		{
			internal get => _folderCompressor;
			set
			{
				if (IsRunning)
					throw new Exception("The Folder compressor cannot be changed while the socket is running.");

				_folderCompressor = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		/// <summary>
		/// Get the port used to start the server
		/// </summary>
		public int Port { get; protected set; }

		/// <summary>
		/// Get the ip on which the server is running
		/// </summary>
		public string Ip { get; protected set; }

		/// <summary>
		/// Indicates if the server or client is running.
		/// </summary>
		public bool IsRunning { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Dispose of the socket
		/// </summary>
		public abstract void Dispose();

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

		//When client receives message
		protected abstract void ReceiveCallback(IAsyncResult result);

		/// <summary>
		/// Start receiving bytes from server
		/// </summary>
		/// <param name="state"></param>
		/// <param name="offset"></param>
		internal abstract void StartReceiving(ISocketState state, int offset = 0);

		//Handle a message
		protected abstract void HandleMessage(IAsyncResult result);
		
		#endregion

	}
}
