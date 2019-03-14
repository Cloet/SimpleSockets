using AsyncClientServer.ByteCreator;

namespace AsyncClientServer.Client
{
	/// <summary>
	/// Implements methods to send messages to the server
	/// <para>Extends <see cref="T:AsyncClientServer.ByteCreator.ByteConverter" />, Implements <see cref="ISendToServer"/></para>
	/// </summary>
	public abstract class SendToServer: ByteConverter,ISendToServer
	{

		/// <summary>
		/// Send bytes to the server
		/// </summary>
		/// <param name="msg">Message as a byte array</param>
		/// <param name="close">if you want to close the client after sending the message.</param>
		protected abstract void SendBytes(byte[] msg, bool close);



		/// <inheritdoc />
		/// <summary>
		/// Send a message to the server
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public void SendMessage(string message,bool encryptMessage, bool close)
		{
			byte[] data = CreateByteMessage(message,encryptMessage);
			SendBytes(data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send a message to the server
		/// <para>The message will be encrypted before sending.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessage(string message, bool close)
		{
			SendMessage(message, true, close);
		}



		/// <inheritdoc />
		/// <summary>
		/// Send an object to server
		/// <para>This object will be serialized using xml</para>
		/// </summary>
		/// <param name="anyObj"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public void SendObject(object anyObj,bool encryptObject, bool close)
		{
			byte[] data = CreateByteObject(anyObj,encryptObject);
			SendBytes(data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send an object to server
		/// <para>The object will be encrypted before sending.</para>
		/// <para>This object will be serialized using xml</para>
		/// </summary>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		public void SendObject(object anyObj, bool close)
		{
			SendObject(anyObj, true, close);
		}




		/// <inheritdoc />
		/// <summary>
		/// Send a file to server
		/// <para>Simple way of sending large files over sockets</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public void SendFile(string fileLocation, string remoteFileLocation,bool encryptFile,bool compressFile, bool close)
		{
			byte[] data = CreateByteFile(fileLocation, remoteFileLocation, encryptFile, compressFile);
			SendBytes(data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send a file to server
		/// <para>The file will be encrypted and compressed before sending.</para>
		/// <para>Simple way of sending large files over sockets</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="close"></param>
		public void SendFile(string fileLocation, string remoteFileLocation, bool close)
		{
			SendFile(fileLocation, remoteFileLocation, true, true, close);
		}


		/// <summary>
		/// Sends a folder to the server.
		/// <para>The folder will be compressed to a .zip file before sending.</para>
		/// <para>Simple way of sending a folder over sockets</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolder(string folderLocation, string remoteFolderLocation,bool encryptFolder, bool close)
		{
			byte[] data = CreateByteFolder(folderLocation, remoteFolderLocation,encryptFolder);
			SendBytes(data, close);
		}

		/// <summary>
		/// Sends a folder to the server.
		/// <para>The folder will be encrypted and compressed before it will be sent.</para>
		/// <para>Simple way of sending a folder over sockets</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolder(string folderLocation, string remoteFolderLocation, bool close)
		{
			SendFolder(folderLocation, remoteFolderLocation, true, close);
		}




		/// <inheritdoc />
		/// <summary>
		/// Sends a file to server
		/// <para>Sends a command to server</para>
		/// </summary>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public void SendCommand(string command,bool encryptCommand, bool close)
		{
			byte[] data = CreateByteCommand(command,encryptCommand);
			SendBytes(data, close);
		}


		/// <inheritdoc />
		/// <summary>
		/// Sends a file to server
		/// <para>The command will be encrypted before sending.</para>
		/// <para>Sends a command to server</para>
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommand(string command, bool close)
		{
			SendCommand(command, true, close);
		}



	}
}
