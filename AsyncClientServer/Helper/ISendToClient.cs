using System;

namespace AsyncClientServer.Helper
{
	/// <summary>
	/// Interface for SendToClient
	/// </summary>
	public interface ISendToClient
	{

		void SendMessage(int id,string message, Boolean close);

		void SendObject(int id,SerializableObject anyObj, Boolean close);

		void SendFile(int id,string FileLocation, string RemoteFileLocation, Boolean Close);

		void SendFileToAllClients(string fileLocation, string remoteSaveLocation, Boolean close);

		void SendMessageToAllClients(string message, Boolean close);

		void SendObjectToAllClients(SerializableObject obj, Boolean close);

	}
}
