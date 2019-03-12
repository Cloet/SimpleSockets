using AsyncClientServer.ByteCreator;

namespace AsyncClientServer.Server
{
	/// <summary>
	/// This abstract class has classes to send messages, objects and files to the client.
	/// <para>Extends <see cref="T:AsyncClientServer.ByteCreator.ByteConverter" />, Implements <see cref="T:AsyncClientServer.Server.ISendToClient" /></para>
	/// </summary>
	public abstract class SendToClient: ByteConverter, ISendToClient
	{

		/// <inheritdoc />
		/// <summary>
		/// Send a message to corresponding client.
		/// <para>Id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessage(int id, string message,bool close)
		{
			byte[] data = CreateByteMessage(message);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		public void SendObject(int id, object anyObj, bool close)
		{
			byte[] data = CreateByteObject(anyObj);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFile(int id, string fileLocation, string remoteSaveLocation, bool close)
		{
			byte[] data = CreateByteFile(fileLocation, remoteSaveLocation);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to the corresponding client.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool close)
		{
			byte[] data = CreateByteFolder(folderLocation, remoteFolderLocation);
			SendBytes(id, data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to the corresponding client and waits for an answer.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id">client id</param>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommand(int id, string command, bool close)
		{
			byte[] data = CreateByteCommand(command);
			SendBytes(id, data, close);
		}

		/// <summary>
		/// Sends bytes to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="data"></param>
		/// <param name="close"></param>
		protected abstract void SendBytes(int id, byte[] data, bool close);

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to all clients
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool close)
		{

			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendFile(c.Key, fileLocation, remoteSaveLocation, close);
			}

		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a folder to all clients.
		/// </summary>
		/// <param name="folderLocation"></param>
		/// <param name="remoteFolderLocation"></param>
		/// <param name="close"></param>
		public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool close)
		{
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendFolder(c.Key, folderLocation, remoteFolderLocation, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a Message to all clients
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessageToAllClients(string message, bool close)
		{
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendMessage(c.Key, message, close);
			}

		}

		/// <inheritdoc />
		/// <summary>
		/// Sends an object to all clients
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="close"></param>
		public void SendObjectToAllClients(object obj, bool close)
		{
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendObject(c.Key, obj, close);
			}
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a command to all connected clients
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommandToAllClients(string command, bool close)
		{
			foreach (var c in AsyncSocketListener.Instance.GetClients())
			{
				SendCommand(c.Key, command, close);
			}
		}

	}
}
