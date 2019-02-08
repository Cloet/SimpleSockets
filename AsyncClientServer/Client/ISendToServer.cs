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
		/// <param name="message"></param>
		/// <param name="close"></param>
		void SendMessage(string message, bool close);

		/// <summary>
		/// Sends an object to the server
		/// </summary>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		void SendObject(object anyObj, bool close);

		/// <summary>
		/// Sends a file to the server
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="Close"></param>
		void SendFile(string fileLocation, string remoteFileLocation, bool Close);

		/// <summary>
		/// Sends a command to the server
		/// </summary>
		/// <param name="command">Command that should be executed by the server</param>
		/// <param name="close"></param>
		void SendCommand(string command, bool close);

	}
}
