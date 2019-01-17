using System;

namespace AsyncClientServer.Helper
{

	/// <summary>
	/// Interface for sending data to server
	/// </summary>
	public interface ISendToServer
	{

		void SendMessage(string message, Boolean close);

		void SendObject(SerializableObject anyObj, Boolean close);

		void SendFile(string FileLocation, string RemoteFileLocation, Boolean Close);

	}
}
