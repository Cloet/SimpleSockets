using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace AsyncClientServer.Helper
{

	/// <summary>
	/// Abstract class used to send data to server/client
	/// </summary>
	public abstract class SendTo
	{

		private byte[] CreateByteArray(string message, string header)
		{
			try
			{

				//Message
				byte[] messageData = Encoding.UTF8.GetBytes(message);
				byte[] headerBytes = Encoding.UTF8.GetBytes(header);
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
		public byte[] CreateByteFile(string fileLocation, string remoteSaveLocation)
		{

			try
			{

				//Message
				byte[] messageData = File.ReadAllBytes(fileLocation);
				byte[] headerBytes = Encoding.UTF8.GetBytes(remoteSaveLocation);
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
		/// <returns>Byte[]</returns>
		public byte[] CreateByteMessage(string message)
		{
			return CreateByteArray(message, "MESSAGE");
		}

		/// <summary>
		/// Creates an array of bytes to send a command
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public byte[] CreateByteCommand(string command)
		{
			return CreateByteArray(command, "COMMAND");
		}

		/// <summary>
		/// Creates an array of bytes to send a command
		/// </summary>
		/// <param name="newPath"></param>
		/// <returns></returns>
		public byte[] CreateByteFileTransfer(string newPath)
		{
			return CreateByteArray(newPath, "FILETRANSFER");
		}

		/// <summary>
		/// Creates an array of bytes
		/// <para>This method serializes an object of type "SerializableObject" and converts it to xml.
		/// This xml string will be converted to bytes and send using sockets and deserialized when it arrives.</para>
		/// </summary>
		/// <param name="serObj"></param>
		/// <returns>Byte[]</returns>
		public byte[] CreateByteObject(SerializableObject serObj)
		{

			string message = serObj.Serialize();
			return CreateByteArray(message, "OBJECT");

		}

	}
}
