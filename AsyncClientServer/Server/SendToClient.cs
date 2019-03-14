using System.Runtime.InteropServices.ComTypes;
using AsyncClientServer.ByteCreator;

namespace AsyncClientServer.Server
{
	/// <summary>
	/// This abstract class has classes to send messages, objects and files to the client.
	/// <para>Extends <see cref="T:AsyncClientServer.ByteCreator.ByteConverter" />, Implements <see cref="T:AsyncClientServer.Server.ISendToClient" /></para>
	/// </summary>
	public abstract class SendToClient: ByteConverter, ISendToClient
	{


		/// <summary>
		/// Sends bytes to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="data"></param>
		/// <param name="close"></param>
		protected abstract void SendBytes(int id, byte[] data, bool close);

		/*==========================================
		*
		*	MESSAGE
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Send a message to corresponding client.
		/// <para>Id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public void SendMessage(int id, string message,bool encryptMessage,bool close)
		{
			byte[] data = CreateByteMessage(message, encryptMessage);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a message to the corresponding client.
		/// <para>This method encrypts the message that will be send.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessage(int id, string message, bool close)
		{
			SendMessage(id, message, true, close);
		}

		/*=============================================
		*
		*	OBJECT
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public void SendObject(int id, object anyObj,bool encryptObject, bool close)
		{
			byte[] data = CreateByteObject(anyObj, encryptObject);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		public void SendObject(int id, object anyObj, bool close)
		{
			SendObject(id, anyObj, true, close);
		}

		/*================================
		*
		*	FILE
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public void SendFile(int id, string fileLocation, string remoteSaveLocation,bool encryptFile,bool compressFile, bool close)
		{
			byte[] data = CreateByteFile(fileLocation, remoteSaveLocation, encryptFile, compressFile);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to corresponding client.
		/// <para>Encrypts and compresses the file before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFile(int id, string fileLocation, string remoteSaveLocation, bool close)
		{
			SendFile(id, fileLocation, remoteSaveLocation, true, true, close);
		}

		/*=================================
		*
		*	FOLDER
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to the corresponding client.
		/// <para>Folder will be compressed to .zip file before being sent.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public void SendFolder(int id, string folderLocation, string remoteFolderLocation,bool encryptFolder, bool close)
		{
			byte[] data = CreateByteFolder(folderLocation, remoteFolderLocation, encryptFolder);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to the corresponding client.
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

		/*=================================
		*
		*	COMMAND
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to the corresponding client and waits for an answer.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id">client id</param>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public void SendCommand(int id, string command,bool encryptCommand, bool close)
		{
			byte[] data = CreateByteCommand(command, encryptCommand);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to the corresponding client and waits for an answer.
		/// <para>Will encrypt the command before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id">client id</param>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommand(int id, string command, bool close)
		{
			SendCommand(id, command, true, close);
		}

		///////////////
		//Broadcasts//
		//////////////

		/*=================================
		*
		*	FILE
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation,bool encryptFile,bool compressFile, bool close)
		{
			var dataBytes = CreateByteFile(fileLocation, remoteSaveLocation, encryptFile, compressFile);
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients
		/// <para>Will encrypt and compress the file before sending.</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool close)
		{
			SendFileToAllClients(fileLocation, remoteSaveLocation, true, true, close);
		}


		/*=================================
		*
		*	FOLDER
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation,bool encryptFolder, bool close)
		{
			var dataBytes = CreateByteFolder(folderLocation, remoteFolderLocation, true);
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients.
		/// <para>Will encrypt and compress the folder before sending.</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool close)
		{
			SendFolderToAllClients(folderLocation, remoteFolderLocation, true, close);
		}

		/*=================================
		*
		*	MESSAGE
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public void SendMessageToAllClients(string message,bool encryptMessage, bool close)
		{
			var dataBytes = CreateByteMessage(message, encryptMessage);
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}

		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessageToAllClients(string message, bool close)
		{
			SendMessageToAllClients(message, true, close);
		}

		/*=================================
		*
		*	OBJECT
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to all clients
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public void SendObjectToAllClients(object obj,bool encryptObject, bool close)
		{
			var dataBytes = CreateByteObject(obj, encryptObject);
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to all clients
		/// <para>Will encrypt the object before sending.</para>
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="close"></param>
		public void SendObjectToAllClients(object obj, bool close)
		{
			SendObjectToAllClients(obj, true, close);
		}

		/*=================================
		*
		*	COMMAND
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to all connected clients
		/// </summary>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public void SendCommandToAllClients(string command,bool encryptCommand, bool close)
		{
			var dataBytes = CreateByteCommand(command, encryptCommand);
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendBytes(c.Key, dataBytes, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to all connected clients
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommandToAllClients(string command, bool close)
		{
			SendCommandToAllClients(command, true, close);
		}

	}
}
