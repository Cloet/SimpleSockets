using System.Threading.Tasks;

namespace AsyncClientServer.Server
{
	/// <summary>
	/// Interface for SendToClient
	/// </summary>
	public interface ISendToClient
	{
		//Message

		/// <summary>
		/// Used to send a message to a certain the client
		/// </summary>
		/// <param name="id">The client id</param>
		/// <param name="message">The message you want to send</param>
		/// <param name="encryptMessage"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		void SendMessage(int id,string message,bool encryptMessage, bool close);

		/// <summary>
		/// Used to send a message to a certain the client
		/// </summary>
		/// <param name="id">The client id</param>
		/// <param name="message">The message you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		void SendMessage(int id, string message, bool close);

		/// <summary>
		/// Used to send a message to a certain the client asynchronous.
		/// </summary>
		/// <param name="id">The client id</param>
		/// <param name="message">The message you want to send</param>
		/// <param name="encryptMessage"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		Task SendMessageAsync(int id, string message, bool encryptMessage, bool close);

		/// <summary>
		/// Used to send a message to a certain the client asynchronous.
		/// </summary>
		/// <param name="id">The client id</param>
		/// <param name="message">The message you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		Task SendMessageAsync(int id, string message, bool close);

		//File

		/// <summary>
		/// Sends a file to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="fileLocation">The path of the file which you want to send.</param>
		/// <param name="remoteFileLocation">The path where it should be saved on the client</param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		void SendFile(int id, string fileLocation, string remoteFileLocation,bool encryptFile,bool compressFile, bool close);

		/// <summary>
		/// Sends a file to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="fileLocation">The path of the file which you want to send.</param>
		/// <param name="remoteFileLocation">The path where it should be saved on the client</param>
		/// <param name="close">true if the client should be closed after this message</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		void SendFile(int id,string fileLocation, string remoteFileLocation, bool close);

		/// <summary>
		/// Sends a file to the corresponding client asynchronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="fileLocation">The path of the file which you want to send.</param>
		/// <param name="remoteFileLocation">The path where it should be saved on the client</param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool encryptFile, bool compressFile, bool close);

		/// <summary>
		/// Sends a file to the corresponding client asynchronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="fileLocation">The path of the file which you want to send.</param>
		/// <param name="remoteFileLocation">The path where it should be saved on the client</param>
		/// <param name="close">true if the client should be closed after this message</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool close);

		//Folder

		/// <summary>
		/// Sends a folder to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="folderLocation">The path of the folder that you want to send.</param>
		/// <param name="remoteFolderLocation">The path where it should be saved on the client.</param>
		/// <param name="encryptFolder"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		void SendFolder(int id, string folderLocation, string remoteFolderLocation,bool encryptFolder, bool close);

		/// <summary>
		/// Sends a folder to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="folderLocation">The path of the folder that you want to send.</param>
		/// <param name="remoteFolderLocation">The path where it should be saved on the client.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool close);

		/// <summary>
		/// Sends a folder to the corresponding client asynchronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="folderLocation">The path of the folder that you want to send.</param>
		/// <param name="remoteFolderLocation">The path where it should be saved on the client.</param>
		/// <param name="encryptFolder"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close);

		/// <summary>
		/// Sends a folder to the corresponding client asynchronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="folderLocation">The path of the folder that you want to send.</param>
		/// <param name="remoteFolderLocation">The path where it should be saved on the client.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool close);

		//Custom Header//

		/// <summary>
		/// Sends a message to the client with a custom header.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		void SendCustomHeaderMessage(int id, string message, string header, bool close);

		/// <summary>
		/// Sends a message to the client with a custom header.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		void SendCustomHeaderMessage(int id, string message, string header, bool encrypt, bool close);

		/// <summary>
		/// Sends a message to the client with a custom header
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		Task SendCustomHeaderMessageAsync(int id, string message, string header, bool close);

		/// <summary>
		/// Sends a message to the client with a custom header
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <param name="close"></param>
		/// <returns>Boolean, True when the message was sent successfully, False when an Error Occurred. (Errors will be invoked to MessageFailed.)</returns>
		Task SendCustomHeaderMessageAsync(int id, string message, string header, bool encrypt, bool close);

		//////////////
		//Broadcasts//
		//////////////

		//File//

		/// <summary>
		/// Sends files to all currently connected clients.
		/// </summary>
		/// <param name="fileLocation">Path of the file you want to send</param>
		/// <param name="remoteSaveLocation">Path where the file should be saved on the client</param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendFileToAllClients(string fileLocation, string remoteSaveLocation,bool encryptFile,bool compressFile, bool close);

		/// <summary>
		/// Sends files to all currently connected clients.
		/// </summary>
		/// <param name="fileLocation">Path of the file you want to send</param>
		/// <param name="remoteSaveLocation">Path where the file should be saved on the client</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool close);

		/// <summary>
		/// Sends files to all currently connected clients asynchronous.
		/// </summary>
		/// <param name="fileLocation">Path of the file you want to send</param>
		/// <param name="remoteSaveLocation">Path where the file should be saved on the client</param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile, bool close);

		/// <summary>
		/// Sends files to all currently connected clients asynchronous.
		/// </summary>
		/// <param name="fileLocation">Path of the file you want to send</param>
		/// <param name="remoteSaveLocation">Path where the file should be saved on the client</param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool close);

		//Folder//

		/// <summary>
		/// Sends a folder to all the currently connected clients.
		/// </summary>
		/// <param name="folderLocation">Path of the folder you want to send</param>
		/// <param name="remoteFolderLocation">Path where the file should be saved on the client</param>
		/// <param name="encryptFolder"></param>
		/// <param name="close">True if the client should be closed after the message.</param>
		void SendFolderToAllClients(string folderLocation, string remoteFolderLocation,bool encryptFolder, bool close);

		/// <summary>
		/// Sends a folder to all the currently connected clients.
		/// </summary>
		/// <param name="folderLocation">Path of the folder you want to send</param>
		/// <param name="remoteFolderLocation">Path where the file should be saved on the client</param>
		/// <param name="close">True if the client should be closed after the message.</param>
		void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool close);

		/// <summary>
		/// Sends a folder to all the currently connected clients asynchronous.
		/// </summary>
		/// <param name="folderLocation">Path of the folder you want to send</param>
		/// <param name="remoteFolderLocation">Path where the file should be saved on the client</param>
		/// <param name="encryptFolder"></param>
		/// <param name="close">True if the client should be closed after the message.</param>
		Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close);

		/// <summary>
		/// Sends a folder to all the currently connected clients asynchronous.
		/// </summary>
		/// <param name="folderLocation">Path of the folder you want to send</param>
		/// <param name="remoteFolderLocation">Path where the file should be saved on the client</param>
		/// <param name="close">True if the client should be closed after the message.</param>
		Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool close);

		//Message//

		/// <summary>
		/// Sends message to all currently connected clients.
		/// </summary>
		/// <param name="message">Message string</param>
		/// <param name="encryptMessage"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendMessageToAllClients(string message,bool encryptMessage, bool close);

		/// <summary>
		/// Sends message to all currently connected clients.
		/// </summary>
		/// <param name="message">Message string</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendMessageToAllClients(string message, bool close);

		/// <summary>
		/// Sends message to all currently connected clients asynchronous.
		/// </summary>
		/// <param name="message">Message string</param>
		/// <param name="encryptMessage"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendMessageToAllClientsAsync(string message, bool encryptMessage, bool close);

		/// <summary>
		/// Sends message to all currently connected clients asynchronous.
		/// </summary>
		/// <param name="message">Message string</param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendMessageToAllClientsAsync(string message, bool close);

		//Custom Header//

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		void SendCustomHeaderToAllClients(string message, string header, bool encryptMessage, bool close);

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		void SendCustomHeaderToAllClients(string message, string header, bool close);

		/// <summary>
		/// Sends a Message to all clients asynchronous.
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encryptMessage"></param>
		/// <param name="close"></param>
		Task SendCustomHeaderToAllClientsAsync(string message, string header, bool encryptMessage,bool close);

		/// <summary>
		/// Sends a Message to all clients
		/// <para/>The close parameter indicates if all the clients should close after the server has sent the message or not.
		/// <para>Will encrypt the message before it is sent.</para>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		Task SendCustomHeaderToAllClientsAsync(string message, string header, bool close);


	}
}
