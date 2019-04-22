using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Compression;
using AsyncClientServer.Cryptography;
using AsyncClientServer.Messages;
using AsyncClientServer.StateObject;

namespace AsyncClientServer.Server
{
	/// <summary>
	/// This abstract class has classes to send messages, objects and files to the client.
	/// <para>Extends <see cref="T:AsyncClientServer.ByteCreator.ByteConverter" />, Implements <see cref="T:AsyncClientServer.Server.ISendToClient" /></para>
	/// </summary>
	public abstract class SendToClient : MessageFactory, ISendToClient
	{


		/// <summary>
		/// Sends bytes to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="data"></param>
		/// <param name="close"></param>
		protected abstract void SendBytes(int id, byte[] data, bool close);

		public abstract IDictionary<int, ISocketState> GetClients();

		/*==========================================
		*
		*	MESSAGE
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Send a message to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		public void SendObject(int id, object anyObj, bool close)
		{
			SendObject(id, anyObj, false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to corresponding client asynchronous.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
			try
			{
				Task.Run(() => SendFileAsync(id, fileLocation, remoteSaveLocation, encryptFile, compressFile, close));
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Encrypts and compresses the file before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFile(int id, string fileLocation, string remoteSaveLocation, bool close)
		{
			SendFile(id, fileLocation, remoteSaveLocation,false, true, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
			await CreateAndSendAsyncFileMessage(fileLocation, remoteFileLocation, compressFile, encryptFile, close, id);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Encrypts and compresses the file before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="close"></param>
		public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool close)
		{
			await SendFileAsync(id, fileLocation, remoteFileLocation, false, true, close);
		}

		/*=================================
		*
		*	FOLDER
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to the corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Folder will be compressed to .zip file before being sent.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			try
			{
				Task.Run(() => SendFolderAsync(id, folderLocation, remoteFolderLocation, encryptFolder, close));
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to the corresponding client.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
				await CreateAndSendAsyncFolderMessage(folderLocation, remoteFolderLocation, encryptFolder, close, id);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}


		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to the corresponding client asynchronous.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Folder will be compressed to a .zip file and encrypted.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public async Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool close)
		{
			await SendFolderAsync(id, folderLocation, remoteFolderLocation, false, close);
		}

		/*=================================
		*
		*	COMMAND
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to the corresponding client and waits for an answer.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
		/// <para>Will encrypt the command before sending.</para>
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id">client id</param>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommand(int id, string command, bool close)
		{
			SendCommand(id, command, false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to the corresponding client asynchronous.
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if the client should close after the server has sent a message or not.
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
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool encryptFile,
			bool compressFile, bool close)
		{
			var data = CreateByteFile(fileLocation, remoteSaveLocation, encryptFile, compressFile);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, data, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt and compress the file before sending.</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool close)
		{
			SendFileToAllClients(fileLocation, remoteSaveLocation, false, true, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients asynchronous
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool encryptFile,
			bool compressFile,
			bool close)
		{
			await CreateAsyncFileMessageBroadcast(fileLocation, remoteSaveLocation, compressFile, encryptFile, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt and compress the file before sending.</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool close)
		{
			await SendFileToAllClientsAsync(fileLocation, remoteSaveLocation, false, true, close);
		}


		/*=================================
		*
		*	FOLDER
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool encryptFolder,bool close)
		{
			var data = CreateByteFolder(folderLocation, remoteFolderLocation, encryptFolder);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, data, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt and compress the folder before sending.</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool close)
		{
			SendFolderToAllClients(folderLocation, remoteFolderLocation, false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder,bool close)
		{
			await CreateAsyncFolderMessageBroadcast(folderLocation, remoteFolderLocation, encryptFolder, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt and compress the folder before sending.</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool close)
		{
			await SendFolderToAllClientsAsync(folderLocation, remoteFolderLocation, false, close);
		}

		/*=================================
		*
		*	MESSAGE
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public void SendMessageToAllClients(string message, bool encryptMessage, bool close)
		{
			var data = CreateByteMessage(message, encryptMessage);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, data, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessageToAllClients(string message, bool close)
		{
			SendMessageToAllClients(message, false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public async Task SendMessageToAllClientsAsync(string message, bool encryptMessage, bool close)
		{
			await Task.Run(() => SendMessageToAllClients(message, false, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
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
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public void SendObjectToAllClients(object obj, bool encryptObject, bool close)
		{
			var data = CreateByteObject(obj, encryptObject);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, data, close);
			}

		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt the object before sending.</para>
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="close"></param>
		public void SendObjectToAllClients(object obj, bool close)
		{
			SendObjectToAllClients(obj, false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
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
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
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
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public void SendCommandToAllClients(string command, bool encryptCommand, bool close)
		{
			var data = CreateByteCommand(command, encryptCommand);
			foreach (var c in GetClients())
			{
				SendBytes(c.Key, data, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to all connected clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommandToAllClients(string command, bool close)
		{
			SendCommandToAllClients(command, false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to all connected clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
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
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public async Task SendCommandToAllClientsAsync(string command, bool close)
		{
			await Task.Run(() => SendCommandToAllClients(command, close));
		}


		#region Broadcast File/folder

		protected async Task<List<int>> StreamFileAndSendToAllClients(string location, string remoteSaveLocation, bool encrypt)
		{
			try
			{
				var file = location;
				var buffer = new byte[10485760];
				bool firstRead = true;
				List<int> clientIds = new List<int>();

				foreach (var c in GetClients())
				{
					clientIds.Add(c.Key);
				}

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


						foreach (var key in clientIds)
						{
							SendBytesPartial(data, key);
						}

					}

				}

				//Delete encrypted file after it has been read.
				if (encrypt)
					File.Delete(file);

				return clientIds;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}


		protected async Task CreateAsyncFileMessageBroadcast(string fileLocation, string remoteSaveLocation,bool compressFile, bool encryptFile, bool close)
		{
			var file = Path.GetFullPath(fileLocation);

			IList<int> clients;

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

			clients = await StreamFileAndSendToAllClients(file, remoteSaveLocation, encryptFile);

			//Deletes the compressed file if no encryption occured.
			if (compressFile && !encryptFile)
				File.Delete(file);


			//Invoke completed for each client that should have received the file
			foreach (var client in clients)
			{
				FileTransferCompleted(close, client);
			}

		}

		protected async Task CreateAsyncFolderMessageBroadcast(string folderLocation, string remoteFolderLocation,bool encryptFolder, 
			bool close)
		{

			IList<int> clients;

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

			clients = await StreamFileAndSendToAllClients(folderToSend, remoteFolderLocation, encryptFolder);

			//Deletes the compressed folder if not encryption occured.
			if (File.Exists(folderToSend))
				File.Delete(folderToSend);

			//Invoke completed for each client that should have received the file
			foreach (var client in clients)
			{
				FileTransferCompleted(close, client);
			}

		}
		#endregion


	}
}
