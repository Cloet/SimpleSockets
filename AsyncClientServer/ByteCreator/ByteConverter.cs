using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Compression;
using Cryptography;

namespace AsyncClientServer.ByteCreator
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
	public abstract class ByteConverter
	{

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
		/// Creates an array of bytes
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
					remoteSaveLocation += ".CGz";
				}

				//Check if the file and header have to be encrypted.
				if (encryptFile)
				{
					AES256.FileEncrypt(fileToSend.FullName);
					//Delete compressed file
					if (compressFile)
						File.Delete(fileToSend.FullName);

					fileToSend = new FileInfo(fileToSend.FullName + ".aes");
					remoteSaveLocation += ".aes";

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
		/// Creates an array of bytes of a folder
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
				tempPath += ".CZip";
				ZipCompression.Compress(folderLocation, tempPath);
				remoteFolderLocation += ".CZip";

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
					folderToSend = tempPath + ".aes";
					remoteFolderLocation += ".aes";

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
