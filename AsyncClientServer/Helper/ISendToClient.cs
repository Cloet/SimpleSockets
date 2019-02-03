using System;

namespace AsyncClientServer.Helper
{
	/// <summary>
	/// Interface for SendToClient
	/// </summary>
	public interface ISendToClient
	{
		/// <summary>
		/// Used to send a message to a certain the client
		/// </summary>
		/// <param name="id">The client id</param>
		/// <param name="message">The message you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendMessage(int id,string message, bool close);

		/// <summary>
		/// Used to send an object to a certain client. Extend your class you want to send with SerializableObject.
		/// <para>You will also have to use [Serialize] tag in the class.</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendObject(int id,SerializableObject anyObj, bool close);

		/// <summary>
		/// Sends a file to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="fileLocation">The path of the file which you want to send.</param>
		/// <param name="remoteFileLocation">The path where it should be saved on the client</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendFile(int id,string fileLocation, string remoteFileLocation, bool close);

		/// <summary>
		/// Sends a command to the corresponding client.
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="command">The command you want to execute</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendCommand(int id, string command, bool close);

		/// <summary>
		/// Sends a request for filetransfer to corresponding client
		/// </summary>
		/// <param name="id">client id</param>
		/// <param name="path">path of which you request child files</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendFileTransfer(int id, string path, bool close);

		/// <summary>
		/// Sends files to all currently connected clients.
		/// </summary>
		/// <param name="fileLocation">Path of the file you want to send</param>
		/// <param name="remoteSaveLocation">Path where the file should be saved on the client</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool close);

		/// <summary>
		/// Sends message to all currently connected clients.
		/// </summary>
		/// <param name="message">Message string</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendMessageToAllClients(string message, bool close);

		/// <summary>
		/// Sends objects to all clients
		/// </summary>
		/// <param name="obj">The object you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendObjectToAllClients(SerializableObject obj, bool close);

		/// <summary>
		/// Sends a command to all clients
		/// </summary>
		/// <param name="command">The command you want to send</param>
		/// <param name="close">true if the client should be closed after this message</param>
		void SendCommandToAllClients(string command, bool close);

	}
}
