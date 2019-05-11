using System.Threading.Tasks;

namespace AsyncClientServer.Client
{

	/// <summary>
	/// Interface for sending data to server
	/// </summary>
	public interface ISendToServer
	{

		/// <summary>
		/// Sends a message to the server
		/// </summary>
		/// <param name="message">The message that will be send.</param>
		/// <param name="encryptMessage"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendMessage(string message,bool encryptMessage, bool close);

		/// <summary>
		/// Sends a message to the server
		/// </summary>
		/// <param name="message">The message that will be send.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendMessage(string message, bool close);

		/// <summary>
		/// Sends a message to the server
		/// </summary>
		/// <param name="message">The message that will be send.</param>
		/// <param name="encryptMessage"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendMessageAsync(string message, bool encryptMessage, bool close);

		/// <summary>
		/// Sends a message to the server
		/// <para>Encryptes the message on default</para>
		/// </summary>
		/// <param name="message">The message that will be send.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendMessageAsync(string message, bool close);

		/// <summary>
		/// Sends a file to the server
		/// </summary>
		/// <param name="fileLocation">Location of the file that will be send on the local machine.</param>
		/// <param name="remoteFileLocation">The location the file will be saved on the remote machine.</param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendFile(string fileLocation, string remoteFileLocation,bool encryptFile,bool compressFile, bool close);

		/// <summary>
		/// Sends a file to the server
		/// </summary>
		/// <param name="fileLocation">Location of the file that will be send on the local machine.</param>
		/// <param name="remoteFileLocation">The location the file will be saved on the remote machine.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendFile(string fileLocation, string remoteFileLocation, bool close);


		/// <summary>
		/// Sends a file to the server
		/// </summary>
		/// <param name="fileLocation">Location of the file that will be send on the local machine.</param>
		/// <param name="remoteFileLocation">The location the file will be saved on the remote machine.</param>
		/// <param name="encryptFile"></param>
		/// <param name="compressFile"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendFileAsync(string fileLocation, string remoteFileLocation, bool encryptFile, bool compressFile, bool close);

		/// <summary>
		/// Sends a file to the server
		/// </summary>
		/// <param name="fileLocation">Location of the file that will be send on the local machine.</param>
		/// <param name="remoteFileLocation">The location the file will be saved on the remote machine.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendFileAsync(string fileLocation, string remoteFileLocation, bool close);



		/// <summary>
		/// Sends a folder to the server
		/// </summary>
		/// <param name="folderLocation">Location of the folder you want to send on the local machine</param>
		/// <param name="remoteFolderLocation">The location of the folder where the folder will be saved on the remote machine.</param>
		/// <param name="encryptFolder"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendFolder(string folderLocation, string remoteFolderLocation,bool encryptFolder, bool close);

		/// <summary>
		/// Sends a folder to the server
		/// </summary>
		/// <param name="folderLocation">Location of the folder you want to send on the local machine</param>
		/// <param name="remoteFolderLocation">The location of the folder where the folder will be saved on the remote machine.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendFolder(string folderLocation, string remoteFolderLocation, bool close);

		/// <summary>
		/// Sends a folder to the server
		/// </summary>
		/// <param name="folderLocation">Location of the folder you want to send on the local machine</param>
		/// <param name="remoteFolderLocation">The location of the folder where the folder will be saved on the remote machine.</param>
		/// <param name="encryptFolder"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendFolderAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close);

		/// <summary>
		/// Sends a folder to the server
		/// </summary>
		/// <param name="folderLocation">Location of the folder you want to send on the local machine</param>
		/// <param name="remoteFolderLocation">The location of the folder where the folder will be saved on the remote machine.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendFolderAsync(string folderLocation, string remoteFolderLocation, bool close);

		/// <summary>
		/// Sends a message to the server with a custom header.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		void SendCustomHeaderMessage(string message, string header, bool close);

		/// <summary>
		/// Sends a message to the server with a custom header.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <param name="close"></param>
		void SendCustomHeaderMessage(string message, string header, bool encrypt, bool close);

		/// <summary>
		/// Sends a message to the server with a custom header
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="close"></param>
		/// <returns></returns>
		Task SendCustomHeaderMessageAsync(string message, string header, bool close);

		/// <summary>
		/// Sends a message to the server with a custom header
		/// </summary>
		/// <param name="message"></param>
		/// <param name="header"></param>
		/// <param name="encrypt"></param>
		/// <param name="close"></param>
		/// <returns></returns>
		Task SendCustomHeaderMessageAsync(string message, string header, bool encrypt, bool close);


	}
}
