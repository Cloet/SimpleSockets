using System;
using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Client;
using AsyncClientServer.StateObject;
using Compression;
using Cryptography;

namespace AsyncClientServer.Messages
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
	public abstract class MessageCreator
	{
		//Calls the SendBytesAsync Method.
		protected delegate void SendBytesAsyncCaller(byte[] data, IStateObject state);

		//Calls the SendBytesAsync Method.
		protected delegate void AsyncCallerFile(bool close, int id);


		//Encrypts a file and returns the new file path.
		private static async Task<string> EncryptFile(string path)
		{
			try
			{
				return await Task.Run(() =>
				{
					AES256.FileEncrypt(Path.GetFullPath(path));
					path += AES256.Extension;
					return path;
				});
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Compresses a file and returns the new path
		private static async Task<string> CompressFile(string path)
		{
			try
			{
				return await Task.Run(() => GZipCompression.Compress(new FileInfo(Path.GetFullPath(path))).FullName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		//Compresses a folder and returns the new path
		private static async Task<string> CompressFolder(string path, string tempPath)
		{
			try
			{
				return await Task.Run(() =>
				{
					ZipCompression.Compress(path, tempPath);
					return tempPath;
				});
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		////Begins sending file to client
		protected abstract Task SendFile(string location, string remoteSaveLocation, bool encrypt,bool close, int id = -1);

		//Gets called to send parts of the file
		protected abstract void SendBytesOfFile(byte[] bytes, int id);

		//Begins sending a file async, when completed the callback will be invoked
		protected async Task BeginSendFile(string location, string remoteSaveLocation, bool encrypt,
			bool close, AsyncCallerFile callback, int id = -1)
		{

			await StreamFileAndSendBytes(location, remoteSaveLocation, encrypt, id);

			callback.Invoke(close, id);

		}


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
		protected async Task CreateAsyncFileMessage(string fileLocation, string remoteSaveLocation,
			bool compressFile, bool encryptFile, bool close, int id = -1)
		{
			var file = Path.GetFullPath(fileLocation);

			//Compresses the file
			if (compressFile)
			{
				file = await CompressFile(file);
				remoteSaveLocation += GZipCompression.Extension;
			}

			//Encrypts the file and deletes the compressed file
			if (encryptFile)
			{
				//Gets the location before the encryption

				string previousFile = string.Empty;
				if (compressFile)
					previousFile = file;

				file = await EncryptFile(file);
				remoteSaveLocation += AES256.Extension;

				//Deletes the compressed file
				if (previousFile != string.Empty)
					File.Delete(previousFile);

			}

			await SendFile(file, remoteSaveLocation, encryptFile, close, id);
			//await StreamFileAndSendBytes(file, remoteSaveLocation, encryptFile, close, id);

			//Deletes the compressed file if no encryption occured.
			if (compressFile && !encryptFile)
				File.Delete(file);

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
		protected async Task CreateAsyncFolderMessage(string folderLocation, string remoteFolderLocation,
			bool encryptFolder, bool close, int id = -1)
		{
			//Gets a temp path for the zip file.
			string tempPath = Path.GetTempFileName();

			//Check if the current temp file exists, if so delete it.
			File.Delete(tempPath);

			//Add extension and compress.
			tempPath += ZipCompression.Extension;
			string folderToSend = await CompressFolder(folderLocation, tempPath);
			remoteFolderLocation += ZipCompression.Extension;

			//Check if folder needs to be encrypted.
			if (encryptFolder)
			{
				//Encrypt and adjust file names.
				folderToSend = await EncryptFile(folderToSend);
				remoteFolderLocation += AES256.Extension;
				File.Delete(tempPath);
			}

			await SendFile(folderToSend, remoteFolderLocation, encryptFolder, close, id);

			//Stream the file in bits and send each time the buffer is full.
			//await StreamFileAndSendBytes(folderToSend, remoteFolderLocation, encryptFolder, close, id);

			//Deletes the compressed folder if not encryption occured.
			if (File.Exists(folderToSend))
				File.Delete(folderToSend);
		}


		//Streams the file and constantly sends bytes to server or client.
		//This method is called in createAsyncFileMessage.
		//Id is an optional parameter with default value of -1.
		protected async Task StreamFileAndSendBytes(string location, string remoteSaveLocation, bool encrypt, int id = -1)
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

						SendBytesOfFile(data, id);

						
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
		private static byte[] CreateByteArray(string message, string header, bool encrypt)
		{
			try
			{
				byte[] messageArray = null;
				byte[] headerArray = null;

				if (encrypt)
				{
					//Header
					byte[] encryptedPrefix = Encoding.UTF8.GetBytes("ENCRYPTED_");
					byte[] encryptedHeader = AES256.EncryptStringToBytes_Aes(header);

					headerArray = new byte[encryptedHeader.Length + encryptedPrefix.Length];

					encryptedPrefix.CopyTo(headerArray, 0);
					encryptedHeader.CopyTo(headerArray, 10);

					messageArray = AES256.EncryptStringToBytes_Aes(message);
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
					fileToSend = GZipCompression.Compress(new FileInfo(fileLocation));
					remoteSaveLocation += GZipCompression.Extension;
				}

				//Check if the file and header have to be encrypted.
				if (encryptFile)
				{
					AES256.FileEncrypt(fileToSend.FullName);
					//Delete compressed file
					if (compressFile)
						File.Delete(fileToSend.FullName);

					fileToSend = new FileInfo(fileToSend.FullName + AES256.Extension);
					remoteSaveLocation += AES256.Extension;

					byte[] encryptedPrefix = Encoding.UTF8.GetBytes("ENCRYPTED_");
					byte[] encryptedHeader = AES256.EncryptStringToBytes_Aes(remoteSaveLocation);

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
				tempPath += ZipCompression.Extension;
				ZipCompression.Compress(folderLocation, tempPath);
				remoteFolderLocation += ZipCompression.Extension;

				//The path to the folder with it current compression extension added.
				string folderToSend = tempPath;

				//Check if the folder has to be encrypted
				if (encryptFolder)
				{
					//Encrypt the file with AES256
					AES256.FileEncrypt(tempPath);

					//Delete compressed file
					File.Delete(tempPath);

					//Change the path with encryption
					folderToSend = tempPath + AES256.Extension;
					remoteFolderLocation += AES256.Extension;

					//The encrypted header
					byte[] encryptedPrefix = Encoding.UTF8.GetBytes("ENCRYPTED_");
					byte[] encryptedHeader = AES256.EncryptStringToBytes_Aes(remoteFolderLocation);

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
		/// Creates an array of bytes to send a command
		/// </summary>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <returns>Byte[]</returns>
		protected byte[] CreateByteCommand(string command, bool encryptCommand)
		{
			return CreateByteArray(command, "COMMAND", encryptCommand);
		}

		/// <summary>
		/// Creates an array of bytes
		/// <para>This method serializes an object of type "SerializableObject" and converts it to xml.
		/// This xml string will be converted to bytes and send using sockets and deserialized when it arrives.</para>
		/// </summary>
		/// <param name="serObj"></param>
		/// <param name="encryptObject"></param>
		/// <returns>Byte[]</returns>
		protected byte[] CreateByteObject(object serObj, bool encryptObject)
		{
			var message = XmlSerialization.XmlSerialization.SerializeToXml(serObj);
			return CreateByteArray(message, "OBJECT", encryptObject);
		}

	}
}
