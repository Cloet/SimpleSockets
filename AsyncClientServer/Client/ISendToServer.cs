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
		/// Sends an object to the server
		/// </summary>
		/// <param name="anyObj">The object that will be serialized and send.</param>
		/// <param name="encryptObject"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendObject(object anyObj,bool encryptObject, bool close);

		/// <summary>
		/// Sends an object to the server
		/// </summary>
		/// <param name="anyObj">The object that will be serialized and send.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendObject(object anyObj, bool close);


		/// <summary>
		/// Sends an object to the server
		/// </summary>
		/// <param name="anyObj">The object that will be serialized and send.</param>
		/// <param name="encryptObject"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendObjectAsync(object anyObj, bool encryptObject, bool close);

		/// <summary>
		/// Sends an object to the server
		/// <para>This will encrypt the object</para>
		/// </summary>
		/// <param name="anyObj">The object that will be serialized and send.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendObjectAsync(object anyObj, bool close);



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
		/// Sends a command to the server
		/// </summary>
		/// <param name="command">Command that should be executed by the server.</param>
		/// <param name="encryptCommand"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendCommand(string command,bool encryptCommand, bool close);

		/// <summary>
		/// Sends a command to the server
		/// </summary>
		/// <param name="command">Command that should be executed by the server.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendCommand(string command, bool close);

		/// <summary>
		/// Sends a command to the server
		/// </summary>
		/// <param name="command">Command that should be executed by the server.</param>
		/// <param name="encryptCommand"></param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendCommandAsync(string command, bool encryptCommand, bool close);

		/// <summary>
		/// Sends a command to the server
		/// <para>Will encrypt the message.</para>
		/// </summary>
		/// <param name="command">Command that should be executed by the server.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		Task SendCommandAsync(string command, bool close);





	}
}
