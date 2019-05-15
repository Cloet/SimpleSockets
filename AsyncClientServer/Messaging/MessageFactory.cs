using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Compression;
using AsyncClientServer.Cryptography;

namespace AsyncClientServer.Messaging
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
	public abstract class MessageFactory
	{
		/// <summary>
		/// Used for encryption and decryption of data.
		/// Created in ServerListener and TcpClient
		/// </summary>
		public Encryption Encrypter { get; protected set; }

		/// <summary>
		/// Used to compress and decompress files.
		/// </summary>
		public FileCompression FileCompressor { get; set; }

		/// <summary>
		/// Class used to compress and extract folders.
		/// </summary>
		public FolderCompression FolderCompressor { get; set; }



		//Encrypts a file and returns the new file path.
		protected async Task<string> EncryptFileAsync(string path)
		{
			try
			{
				return await Task.Run(() =>
				{
					Encrypter.FileEncrypt(Path.GetFullPath(path));
					path += Encrypter.Extension;
					return path;
				});
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
				return await Task.Run(() => FileCompressor.Compress(new FileInfo(Path.GetFullPath(path))).FullName);
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
				});
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Triggered when file/folder is done sending.
		protected abstract void FileTransferCompleted(bool close, int id);

		//Gets called to send parts of the file
		protected abstract void SendBytesPartial(byte[] bytes, int id);


		/// <summary>
		/// This method Sends a file asynchronous.
		/// <para>It checks if the file has to be compressed and/or encrypted before being sent.
		/// It reads the file with a default buffer of 10Mb. Whenever a part of the buffer is read it will send bytes.</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="compressFile"></param>
		/// <param name="encryptFile"></param>
		/// <param name="close"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		protected async Task CreateAndSendAsyncFileMessage(string fileLocation, string remoteSaveLocation,
			bool compressFile, bool encryptFile, bool close, int id = -1)
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
					remoteSaveLocation += Encrypter.Extension;

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
				throw new Exception("Unable to Send a file.", ex);
			}
		}

		/// <summary>
		/// This methods Sends a folder asynchronous.
		/// <para>It checks if the folder has to be encrypted before being sent.
		/// The folder will always be compressed as a .ZIP file to make transfer easier. Uses the extension "CZip".</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		protected async Task CreateAndSendAsyncFolderMessage(string folderLocation, string remoteFolderLocation,bool encryptFolder, bool close, int id = -1)
		{

			try
			{
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
					remoteFolderLocation += Encrypter.Extension;
					File.Delete(tempPath);
				}


				await StreamFileAndSendBytesAsync(folderToSend, remoteFolderLocation, encryptFolder, id);
				//await SendFileAsynchronous(folderToSend, remoteFolderLocation, encryptFolder, close, id);

				//Deletes the compressed folder if not encryption occured.
				if (File.Exists(folderToSend))
					File.Delete(folderToSend);

				FileTransferCompleted(close, id);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to Send folder async.", ex);
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
				var buffer = new byte[10485760];
				bool firstRead = true;

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
								byte[] headerData = Encrypter.EncryptStringToBytes(remoteSaveLocation);
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

						SendBytesPartial(data, id);

						
					}

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
					byte[] encryptedHeader = Encrypter.EncryptStringToBytes(header);

					headerArray = new byte[encryptedHeader.Length + encryptedPrefix.Length];

					encryptedPrefix.CopyTo(headerArray, 0);
					encryptedHeader.CopyTo(headerArray, 10);

					messageArray = Encrypter.EncryptStringToBytes(message);
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



		/////////*************************************DEPRECATED************************************************//////////////
		/// <summary>
		/// Creates an array of bytes. DEPRECATED.
		/// <para>This method sets the location of where the file will be copied to.
		/// It also gets all bytes in a file and writes it to fileData byte array</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		///	<param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <returns>Byte[]</returns>
		protected byte[] CreateByteFile(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile)
		{

			try
			{

				FileInfo fileToSend = new FileInfo(fileLocation);
				byte[] header = null;

				if (compressFile)
				{
					fileToSend = FileCompressor.Compress(new FileInfo(fileLocation));
					remoteSaveLocation += FileCompressor.Extension;
				}

				//Check if the file and header have to be encrypted.
				if (encryptFile)
				{
					Encrypter.FileEncrypt(fileToSend.FullName);
					//Delete compressed file
					if (compressFile)
						File.Delete(fileToSend.FullName);

					fileToSend = new FileInfo(fileToSend.FullName + Encrypter.Extension);
					remoteSaveLocation += Encrypter.Extension;

					byte[] encryptedPrefix = Encoding.UTF8.GetBytes("ENCRYPTED_");
					byte[] encryptedHeader = Encrypter.EncryptStringToBytes(remoteSaveLocation);

					header = new byte[encryptedHeader.Length + encryptedPrefix.Length];

					encryptedPrefix.CopyTo(header, 0);
					encryptedHeader.CopyTo(header, 10);
				}
				else
					header = Encoding.UTF8.GetBytes(remoteSaveLocation);

				//Message
				byte[] messageData = File.ReadAllBytes(fileToSend.FullName);
				byte[] headerBytes = header;
				byte[] headerLen = BitConverter.GetBytes(headerBytes.Length);
				byte[] messageLength = BitConverter.GetBytes(messageData.Length);

				//Delete encrypted file after it has been read.
				if (encryptFile)
					File.Delete(fileToSend.FullName);


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
		/// Creates an array of bytes of a folder. DEPRECATED.
		/// <para>This method zips all files in a folder encrypts it with aes and sends the files.</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <returns></returns>
		protected byte[] CreateByteFolder(string folderLocation, string remoteFolderLocation, bool encryptFolder)
		{

			try
			{

				string tempPath = Path.GetTempFileName();
				byte[] header = null;

				//If this particular temp file exists delete it. Then start compression.
				File.Delete(tempPath);
				tempPath += FolderCompressor.Extension;
				FolderCompressor.Compress(folderLocation, tempPath);
				remoteFolderLocation += FolderCompressor.Extension;

				//The path to the folder with it current compression extension added.
				string folderToSend = tempPath;

				//Check if the folder has to be encrypted
				if (encryptFolder)
				{
					//Encrypt the file with AES256
					Encrypter.FileEncrypt(tempPath);

					//Delete compressed file
					File.Delete(tempPath);

					//Change the path with encryption
					folderToSend = tempPath + Encrypter.Extension;
					remoteFolderLocation += Encrypter.Extension;

					//The encrypted header
					byte[] encryptedPrefix = Encoding.UTF8.GetBytes("ENCRYPTED_");
					byte[] encryptedHeader = Encrypter.EncryptStringToBytes(remoteFolderLocation);

					header = new byte[encryptedHeader.Length + encryptedPrefix.Length];

					encryptedPrefix.CopyTo(header, 0);
					encryptedHeader.CopyTo(header, 10);

				}
				else
					header = Encoding.UTF8.GetBytes(remoteFolderLocation);


				//Message
				byte[] messageData = File.ReadAllBytes(folderToSend);
				byte[] headerBytes = header;
				byte[] headerLen = BitConverter.GetBytes(headerBytes.Length);
				byte[] messageLength = BitConverter.GetBytes(messageData.Length);

				//Will delete the remaining file at temp path.
				File.Delete(folderToSend);


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
		/////////*************************************END DEPRECATED************************************************//////////////

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

	}
}
