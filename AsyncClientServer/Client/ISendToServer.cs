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
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendMessage(string message, bool close);

		/// <summary>
		/// Sends an object to the server
		/// </summary>
		/// <param name="anyObj">The object that will be serialized and send.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendObject(object anyObj, bool close);

		/// <summary>
		/// Sends a file to the server
		/// </summary>
		/// <param name="fileLocation">Location of the file that will be send on the local machine.</param>
		/// <param name="remoteFileLocation">The location the file will be saved on the remote machine.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendFile(string fileLocation, string remoteFileLocation, bool close);

		/// <summary>
		/// Sends a folder to the server
		/// </summary>
		/// <param name="folderLocation">Location of the folder you want to send on the local machine</param>
		/// <param name="remoteFolderLocation">The location of the folder where the folder will be saved on the remote machine.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendFolder(string folderLocation, string remoteFolderLocation, bool close);

		/// <summary>
		/// Sends a command to the server
		/// </summary>
		/// <param name="command">Command that should be executed by the server.</param>
		/// <param name="close">True if the client should be closed after this message.</param>
		void SendCommand(string command, bool close);

	}
}
