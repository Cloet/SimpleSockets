using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AsyncClientServer.Messages;

namespace AsyncClientServer.Server
{
	/// <summary>
	/// This abstract class has classes to send messages, objects and files to the client.
	/// <para>Extends <see cref="T:AsyncClientServer.ByteCreator.ByteConverter" />, Implements <see cref="T:AsyncClientServer.Server.ISendToClient" /></para>
	/// </summary>
	public abstract class SendToClient : MessageCreator, ISendToClient
	{


		/// <summary>
		/// Sends bytes to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="data"></param>
		/// <param name="close"></param>
		protected abstract void SendBytes(int id, byte[] data, bool close);

		/// <inheritdoc />
		/// <summary>
		/// Gets called async
		/// </summary>
		/// <param name="data"></param>
		/// <param name="close"></param>
		/// <param name="id"></param>
		protected override void SendBytesAsync(byte[] data, bool close, int id)
		{
			SendBytes(id, data, close);
		}

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
		public void SendMessage(int id, string message, bool encryptMessage, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Send a message to corresponding client asynchronous.
		/// <para>Id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public async Task SendMessageAsync(int id, string message, bool encryptMessage, bool close)
		{
			await Task.Run(() => SendMessage(id, message, encryptMessage, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a message to the corresponding client asynchronous.
		/// <para>This method encrypts the message that will be send.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public async Task SendMessageAsync(int id, string message, bool close)
		{
			await Task.Run(() => SendMessage(id, message, close));
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
		public void SendObject(int id, object anyObj, bool encryptObject, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to corresponding client asynchronous.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public async Task SendObjectAsync(int id, object anyObj, bool encryptObject, bool close)
		{
			await Task.Run(() => SendObject(id, anyObj, encryptObject, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to corresponding client asynchronous.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		public async Task SendObjectAsync(int id, object anyObj, bool close)
		{
			await Task.Run(() => SendObject(id, anyObj, close));
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
		public void SendFile(int id, string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool encryptFile, bool compressFile, bool close)
		{
			await CreateAsyncFileMessage(fileLocation, remoteFileLocation, compressFile, encryptFile, close, id);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to corresponding client.
		/// <para>Encrypts and compresses the file before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="close"></param>
		public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool close)
		{
			await SendFileAsync(id, fileLocation, remoteFileLocation, true, true, close);
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
		public void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to the corresponding client asynchronous.
		/// <para>Folder will be compressed to .zip file before being sent.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public async Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			try
			{
				await CreateAsyncFolderMessage(folderLocation, remoteFolderLocation, encryptFolder, close, id);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}


		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to the corresponding client asynchronous.
		/// <para>Folder will be compressed to a .zip file and encrypted.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public async Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool close)
		{
			await SendFolderAsync(id, folderLocation, remoteFolderLocation, true, close);
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
		public void SendCommand(int id, string command, bool encryptCommand, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to the corresponding client asynchronous.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id">client id</param>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public async Task SendCommandAsync(int id, string command, bool encryptCommand, bool close)
		{
			await Task.Run(() => SendCommand(id, command, encryptCommand, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to the corresponding client asynchronous.
		/// <para>Will encrypt the command before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id">client id</param>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public async Task SendCommandAsync(int id, string command, bool close)
		{
			await Task.Run(() => SendCommand(id, command, close));
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
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients asynchronous
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile,
			bool close)
		{
			try
			{
				foreach (var c in AsyncSocketListener.Instance.GetClients())
				{
					await CreateAsyncFileMessage(fileLocation, remoteSaveLocation, encryptFile, compressFile, close, c.Key);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients asynchronous.
		/// <para>Will encrypt and compress the file before sending.</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool close)
		{
			await SendFileToAllClientsAsync(fileLocation, remoteSaveLocation, true, true, close);
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
		public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients asynchronous.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			try
			{
				foreach (var c in AsyncSocketListener.Instance.GetClients())
				{
					await CreateAsyncFolderMessage(folderLocation, remoteFolderLocation, encryptFolder, close, c.Key);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients asynchronous.
		/// <para>Will encrypt and compress the folder before sending.</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool close)
		{
			await SendFolderToAllClientsAsync(folderLocation, remoteFolderLocation, true, close);
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
		public void SendMessageToAllClients(string message, bool encryptMessage, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients asynchronous.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public async Task SendMessageToAllClientsAsync(string message, bool encryptMessage, bool close)
		{
			await Task.Run(() => SendMessageToAllClients(message, encryptMessage, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public async Task SendMessageToAllClientsAsync(string message, bool close)
		{
			await Task.Run(() => SendMessageToAllClients(message, close));
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
		public void SendObjectToAllClients(object obj, bool encryptObject, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to all clients asynchronous.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public async Task SendObjectToAllClientsAsync(object obj, bool encryptObject, bool close)
		{
			await Task.Run(() => SendObjectToAllClients(obj, encryptObject, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to all clients asynchronous.
		/// <para>Will encrypt the object before sending.</para>
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="close"></param>
		public async Task SendObjectToAllClientsAsync(object obj, bool close)
		{
			await Task.Run(() => SendObjectToAllClients(obj, close));
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
		public void SendCommandToAllClients(string command, bool encryptCommand, bool close)
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

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to all connected clients asynchronous.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public async Task SendCommandToAllClientsAsync(string command, bool encryptCommand, bool close)
		{
			await Task.Run(() => SendCommandToAllClients(command, encryptCommand, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to all connected clients asynchronous.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public async Task SendCommandToAllClientsAsync(string command, bool close)
		{
			await Task.Run(() => SendCommandToAllClients(command, close));
		}

	}
}
