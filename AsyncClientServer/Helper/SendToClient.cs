using System;
using AsyncClientServer.Model;

namespace AsyncClientServer.Helper
{
	public abstract class SendToClient: SendTo, ISendToClient
	{

		/// <summary>
		/// Send a message to corresponding client.
		/// <para>Id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessage(int id, string message, Boolean close)
		{
			byte[] data = CreateByteArray(message);
			SendBytes(id, data, false);
		}

		/// <summary>
		/// Sends an object to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		public void SendObject(int id, SerializableObject anyObj, bool close)
		{
			byte[] data = CreateByteArray(anyObj);
			SendBytes(id, data, false);
		}

		/// <summary>
		/// Sends a file to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="Filelocation"></param>
		/// <param name="RemoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFile(int id, string Filelocation, string RemoteSaveLocation, Boolean close)
		{
			byte[] data = CreateByteArray(Filelocation, RemoteSaveLocation);
			SendBytes(id, data, false);
		}

		/// <summary>
		/// Sends bytes to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="data"></param>
		/// <param name="close"></param>
		protected abstract void SendBytes(int id, byte[] data, bool close);

		/// <summary>
		/// Sends a file to all clients
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, Boolean close)
		{

			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendFile(c.Key, fileLocation, remoteSaveLocation, close);
			}

		}

		/// <summary>
		/// Sends a Message to all clients
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessageToAllClients(string message, Boolean close)
		{
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendMessage(c.Key, message, close);
			}

		}

		/// <summary>
		/// Sends an object to all clients
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="close"></param>
		public void SendObjectToAllClients(SerializableObject obj, Boolean close)
		{
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendObject(c.Key, obj, close);
			}
		}

	}
}
