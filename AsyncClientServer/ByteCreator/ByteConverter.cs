using System;
using System.IO;
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
		private static byte[] CreateByteArray(string message, string header)
		{
			try
			{
				var encryptedHeader = AES256.EncryptStringToBytes_Aes(header);
				var encryptedMessage = AES256.EncryptStringToBytes_Aes(message);

				//Message
				byte[] messageData = encryptedMessage;
				byte[] headerBytes = encryptedHeader;
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
		/// <returns>Byte[]</returns>
		protected byte[] CreateByteFile(string fileLocation, string remoteSaveLocation)
		{

			try
			{
				FileInfo fileToSend = GZipCompression.Compress(new FileInfo(fileLocation));
				remoteSaveLocation += ".CGz";

				AES256.FileEncrypt(fileToSend.FullName);

				//Delete compressed file
				File.Delete(fileToSend.FullName);

				fileToSend = new FileInfo(fileToSend.FullName + ".aes");
				remoteSaveLocation += ".aes";

				var encryptedHeader = AES256.EncryptStringToBytes_Aes(remoteSaveLocation);

				//Message
				byte[] messageData = File.ReadAllBytes(fileToSend.FullName);
				byte[] headerBytes = encryptedHeader;
				byte[] headerLen = BitConverter.GetBytes(headerBytes.Length);
				byte[] messageLength = BitConverter.GetBytes(messageData.Length);

				//Delete encrypted file after it has been read.
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
		/// <returns></returns>
		protected byte[] CreateByteFolder(string folderLocation, string remoteFolderLocation)
		{

			try
			{

				string tempPath = Path.GetTempFileName();
				File.Delete(tempPath);
				tempPath += ".CZip";

				ZipCompression.Compress(folderLocation, tempPath);
				remoteFolderLocation += ".CZip";

				AES256.FileEncrypt(tempPath);

				//Delete compressed file
				File.Delete(tempPath);

				string folderToSend = tempPath + ".aes";
				remoteFolderLocation += ".aes";

				var encryptedHeader = AES256.EncryptStringToBytes_Aes(remoteFolderLocation);

				//Message
				byte[] messageData = File.ReadAllBytes(folderToSend);
				byte[] headerBytes = encryptedHeader;
				byte[] headerLen = BitConverter.GetBytes(headerBytes.Length);
				byte[] messageLength = BitConverter.GetBytes(messageData.Length);

				//Delete encrypted file after it has been read.
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
		/// <returns>Byte[]</returns>
		protected byte[] CreateByteMessage(string message)
		{
			return CreateByteArray(message, "MESSAGE");
		}

		/// <summary>
		/// Creates an array of bytes to send a command
		/// </summary>
		/// <param name="command"></param>
		/// <returns>Byte[]</returns>
		protected byte[] CreateByteCommand(string command)
		{
			return CreateByteArray(command, "COMMAND");
		}

		/// <summary>
		/// Creates an array of bytes
		/// <para>This method serializes an object of type "SerializableObject" and converts it to xml.
		/// This xml string will be converted to bytes and send using sockets and deserialized when it arrives.</para>
		/// </summary>
		/// <param name="serObj"></param>
		/// <returns>Byte[]</returns>
		protected byte[] CreateByteObject(object serObj)
		{
			var message = XmlSerialization.XmlSerialization.SerializeToXml(serObj);
			return CreateByteArray(message, "OBJECT");

		}

	}
}
