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
		void SendMessage(int id,string message,bool encryptMessage, bool close);

		/// <summary>
		/// Used to send a message to a certain the client
		/// </summary>
		/// <param name="id">The client id</param>
		/// <param name="message">The message you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendMessage(int id, string message, bool close);

		/// <summary>
		/// Used to send a message to a certain the client asynchronous.
		/// </summary>
		/// <param name="id">The client id</param>
		/// <param name="message">The message you want to send</param>
		/// <param name="encryptMessage"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendMessageAsync(int id, string message, bool encryptMessage, bool close);

		/// <summary>
		/// Used to send a message to a certain the client asynchronous.
		/// </summary>
		/// <param name="id">The client id</param>
		/// <param name="message">The message you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendMessageAsync(int id, string message, bool close);


		//Object

		/// <summary>
		/// Used to send an object to a certain client.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="serializedObject"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendObject(int id, string serializedObject, bool encryptObject, bool close);

		/// <summary>
		/// Used to send an object to a certain client.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="serializedObject"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendObject(int id,string serializedObject, bool close);

		/// <summary>
		/// Used to send an object to a certain client asynchronous.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="serializedObject"></param>
		/// <param name="encryptObject"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendObjectAsync(int id, string serializedObject, bool encryptObject, bool close);

		/// <summary>
		/// Used to send an object to a certain client asynchronous.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="serializedObject"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendObjectAsync(int id, string serializedObject, bool close);

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
		void SendFile(int id, string fileLocation, string remoteFileLocation,bool encryptFile,bool compressFile, bool close);

		/// <summary>
		/// Sends a file to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="fileLocation">The path of the file which you want to send.</param>
		/// <param name="remoteFileLocation">The path where it should be saved on the client</param>
		/// <param name="close">true if the client should be closed after this message</param>
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
		Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool encryptFile, bool compressFile, bool close);

		/// <summary>
		/// Sends a file to the corresponding client asynchronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="fileLocation">The path of the file which you want to send.</param>
		/// <param name="remoteFileLocation">The path where it should be saved on the client</param>
		/// <param name="close">true if the client should be closed after this message</param>
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
		void SendFolder(int id, string folderLocation, string remoteFolderLocation,bool encryptFolder, bool close);

		/// <summary>
		/// Sends a folder to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="folderLocation">The path of the folder that you want to send.</param>
		/// <param name="remoteFolderLocation">The path where it should be saved on the client.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool close);

		/// <summary>
		/// Sends a folder to the corresponding client asnychronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="folderLocation">The path of the folder that you want to send.</param>
		/// <param name="remoteFolderLocation">The path where it should be saved on the client.</param>
		/// <param name="encryptFolder"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close);

		/// <summary>
		/// Sends a folder to the corresponding client asynchronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="folderLocation">The path of the folder that you want to send.</param>
		/// <param name="remoteFolderLocation">The path where it should be saved on the client.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool close);

		//Command

		/// <summary>
		/// Sends a command to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="command">The command you want to execute</param>
		/// <param name="encryptCommand"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendCommand(int id, string command,bool encryptCommand, bool close);

		/// <summary>
		/// Sends a command to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="command">The command you want to execute</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendCommand(int id, string command, bool close);

		/// <summary>
		/// Sends a command to the corresponding client asynchronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="command">The command you want to execute</param>
		/// <param name="encryptCommand"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendCommandAsync(int id, string command, bool encryptCommand, bool close);

		/// <summary>
		/// Sends a command to the corresponding client asynchronous.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="command">The command you want to execute</param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendCommandAsync(int id, string command, bool close);

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

		//Object//

		/// <summary>
		/// Sends objects to all clients
		/// </summary>
		/// <param name="serializedObject">The object you want to send</param>
		/// <param name="encryptObject"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendObjectToAllClients(string serializedObject, bool encryptObject, bool close);

		/// <summary>
		/// Sends objects to all clients
		/// </summary>
		/// <param name="serializedObject">The object you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendObjectToAllClients(string serializedObject, bool close);

		/// <summary>
		/// Sends objects to all clients asynchronous.
		/// </summary>
		/// <param name="serializedObject">The object you want to send</param>
		/// <param name="encryptObject"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendObjectToAllClientsAsync(string serializedObject, bool encryptObject, bool close);

		/// <summary>
		/// Sends objects to all clients asynchronous.
		/// </summary>
		/// <param name="serializedObject">The object you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendObjectToAllClientsAsync(string serializedObject, bool close);

		//Command//

		/// <summary>
		/// Sends a command to all clients.
		/// </summary>
		/// <param name="command">The command you want to send</param>
		/// <param name="encryptCommand"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendCommandToAllClients(string command, bool encryptCommand, bool close);

		/// <summary>
		/// Sends a command to all clients.
		/// </summary>
		/// <param name="command">The command you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendCommandToAllClients(string command, bool close);

		/// <summary>
		/// Sends a command to all clients asynchronous.
		/// </summary>
		/// <param name="command">The command you want to send</param>
		/// <param name="encryptCommand"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendCommandToAllClientsAsync(string command,bool encryptCommand, bool close);

		/// <summary>
		/// Sends a command to all clients asynchronous.
		/// </summary>
		/// <param name="command">The command you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		Task SendCommandToAllClientsAsync(string command, bool close);

	}
}
