using System;
using System.CodeDom;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Messaging;


namespace AsyncClientServer.Client
{
	/// <summary>
	/// Implements methods to send messages to the server
	/// <para>Extends <see cref="T:AsyncClientServer.ByteCreator.ByteConverter" />, Implements <see cref="ISendToServer"/></para>
	/// </summary>
	public abstract class SendToServer : MessageFactory, ISendToServer
	{

		/// <summary>
		/// Send bytes to the server
		/// </summary>
		/// <param name="msg">Message as a byte array</param>
		/// <param name="close">if you want to close the client after sending the message.</param>
		protected abstract void SendBytes(byte[] msg, bool close);

		/*=================================
		*
		*	MESSAGE
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Send a message to the server
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public void SendMessage(string message, bool encryptMessage, bool close)
		{
			byte[] data = CreateByteMessage(message, encryptMessage);
			SendBytes(data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send a message to the server
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessage(string message, bool close)
		{
			SendMessage(message,false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send a message to the server asynchronous.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		public async Task SendMessageAsync(string message, bool encryptMessage, bool close)
		{
			await Task.Run(() => SendMessage(message, encryptMessage, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Send a message to the server asynchronous.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>The message will be encrypted before sending.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public async Task SendMessageAsync(string message, bool close)
		{
			await Task.Run(() => SendMessage(message, close));
		}


		/*=================================
		*
		*	OBJECT
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Send an object to server
		/// <para>This object will be serialized using xml</para>
		/// </summary>
		/// <param name="serializedObject"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public void SendObject(string serializedObject, bool encryptObject, bool close)
		{
			byte[] data = CreateByteObject(serializedObject, encryptObject);
			SendBytes(data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send an object to server
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>This object will be serialized using xml</para>
		/// </summary>
		/// <param name="serializedObject"></param>
		/// <param name="close"></param>
		public void SendObject(string serializedObject, bool close)
		{
			SendObject(serializedObject, false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send an object to server asynchronous.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>This object will be serialized using xml</para>
		/// </summary>
		/// <param name="serializedObject"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close"></param>
		public async Task SendObjectAsync(string serializedObject, bool encryptObject, bool close)
		{
			await Task.Run(() => SendObject(serializedObject, encryptObject, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Send an object to server asynchronous.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>The object won't be encrypted before sending.</para>
		/// <para>This object will be serialized using xml</para>
		/// </summary>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		public async Task SendObjectAsync(string serializedObject, bool close)
		{
			await Task.Run(() => SendObject(serializedObject, close));
		}


		/*=================================
		*
		*	FILE
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Send a file to server
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>Simple way of sending large files over sockets</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public void SendFile(string fileLocation, string remoteFileLocation, bool encryptFile, bool compressFile, bool close)
		{
			Task.Run(() => SendFileAsync(fileLocation, remoteFileLocation, encryptFile, compressFile, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Send a file to server
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>Simple way of sending large files over sockets</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="close"></param>
		public void SendFile(string fileLocation, string remoteFileLocation, bool close)
		{
			SendFile(fileLocation, remoteFileLocation, false, true, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send a file to server asynchronous.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>Simple way of sending large files over sockets</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close"></param>
		public async Task SendFileAsync(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile, bool close)
		{
			try
			{
				await CreateAndSendAsyncFileMessage(fileLocation, remoteSaveLocation, compressFile, encryptFile, close);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}


		}

		/// <inheritdoc />
		/// <summary>
		/// Send a file to server asynchronous.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>The file will be compressed before sending.</para>
		/// <para>Simple way of sending large files over sockets</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="close"></param>
		public async Task SendFileAsync(string fileLocation, string remoteFileLocation, bool close)
		{
			await SendFileAsync(fileLocation, remoteFileLocation, false, true, close);
		}

		/*=================================
		*
		*	FOLDER
		*
		*===========================================*/

		/// <summary>
		/// Sends a folder to the server.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>The folder will be compressed to a .zip file before sending.</para>
		/// <para>Simple way of sending a folder over sockets</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public void SendFolder(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{
			Task.Run(() => SendFolderAsync(folderLocation, remoteFolderLocation, encryptFolder, close));
		}

		/// <summary>
		/// Sends a folder to the server.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>The folder will be compressed before it will be sent.</para>
		/// <para>Simple way of sending a folder over sockets</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolder(string folderLocation, string remoteFolderLocation, bool close)
		{
			SendFolder(folderLocation, remoteFolderLocation, false, close);
		}


		/// <summary>
		/// Sends a folder to the server asynchronous.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>The folder will be compressed to a .zip file before sending.</para>
		/// <para>Simple way of sending a folder over sockets</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="encryptFolder"></param>
		/// <param name="close"></param>
		public async Task SendFolderAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close)
		{

			try
			{
				await CreateAndSendAsyncFolderMessage(folderLocation, remoteFolderLocation, encryptFolder, close);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <summary>
		/// Sends a folder to the server asynchronous.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>The folder will be encrypted and compressed before it will be sent.</para>
		/// <para>Simple way of sending a folder over sockets</para>
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public async Task SendFolderAsync(string folderLocation, string remoteFolderLocation, bool close)
		{
			await SendFolderAsync(folderLocation, remoteFolderLocation, false, close);
		}


		/*=================================
		*
		*	COMMAND
		*
		*===========================================*/

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to server
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>Sends a command to server</para>
		/// </summary>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public void SendCommand(string command, bool encryptCommand, bool close)
		{
			byte[] data = CreateByteCommand(command, encryptCommand);
			SendBytes(data, close);
		}


		/// <inheritdoc />
		/// <summary>
		/// Sends a file to server
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>Sends a command to server</para>
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommand(string command, bool close)
		{
			SendCommand(command, false, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to server
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>Sends a command to server</para>
		/// </summary>
		/// <param name="command"></param>
		/// <param name="encryptCommand"></param>
		/// <param name="close"></param>
		public async Task SendCommandAsync(string command, bool encryptCommand, bool close)
		{
			await Task.Run(() => SendCommand(command, encryptCommand, close));
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to server.
		/// <para/>The close parameter indicates if the client should close after sending or not.
		/// <para>Sends a command to server</para>
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public async Task SendCommandAsync(string command, bool close)
		{
			await Task.Run(() => SendCommandAsync(command, close));
		}

		/// <summary>
		/// Sends a message to the server with a custom header.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		public void SendCustomHeaderMessage(string message, string header, bool close)
		{
			SendCustomHeaderMessage(message, header, false, close);
		}

		/// <summary>
		/// Sends a message to the server with a custom header.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <param name="close"></param>
		public void SendCustomHeaderMessage(string message, string header, bool encrypt, bool close)
		{
			byte[] data = CreateByteCustomHeader(message, header, encrypt);
			SendBytes(data, close);
		}

		/// <summary>
		/// Sends a message to the server with a custom header
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		/// <returns></returns>
		public async Task SendCustomHeaderMessageAsync(string message, string header, bool close)
		{
			await Task.Run(() => SendCustomHeaderMessage(message, header, false, close));
		}

		/// <summary>
		/// Sends a message to the server with a custom header
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <param name="close"></param>
		/// <returns></returns>
		public async Task SendCustomHeaderMessageAsync(string message, string header, bool encrypt, bool close)
		{
			await Task.Run(() => SendCustomHeaderMessage(message, header, encrypt, close));
		}

	}
}
