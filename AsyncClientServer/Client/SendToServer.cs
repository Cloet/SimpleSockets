using AsyncClientServer.ByteCreator;

namespace AsyncClientServer.Client
{
	/// <summary>
	/// Implements methods to send messages to the server
	/// <para>Extends <see cref="T:AsyncClientServer.ByteCreator.ByteConverter" />, Implements <see cref="ISendToServer"/></para>
	/// </summary>
	public abstract class SendToServer: ByteConverter,ISendToServer
	{

		/// <inheritdoc />
		/// <summary>
		/// Send a message to the server
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		public void SendMessage(string message, bool close)
		{
			byte[] data = CreateByteMessage(message);

			SendBytes(data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send an object to server
		/// <para>This object will be serialized using xml</para>
		/// <para>If you want to send your own objects use "SerializableObject" wrapper</para>
		/// </summary>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		public void SendObject(object anyObj, bool close)
		{
			byte[] data = CreateByteObject(anyObj);
			SendBytes(data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Send a file to server
		/// <para>Simple way of sending large files over sockets</para>
		/// </summary>
		/// <param name="FileLocation"></param>
		/// <param name="RemoteFileLocation"></param>
		/// <param name="close"></param>
		public void SendFile(string FileLocation, string RemoteFileLocation, bool close)
		{
			byte[] data = CreateByteFile(FileLocation, RemoteFileLocation);
			SendBytes(data, close);
		}

		/// <summary>
		/// Send fileTransfer data to server
		/// </summary>
		/// <param name="path"></param>
		/// <param name="close"></param>
		public void SendFileTransfer(string path, bool close)
		{
			byte[] data = CreateByteFileTransfer(path);
			SendBytes(data, close);
		}

		/// <inheritdoc />
		/// <summary>
		/// Sends a file to server
		/// <para>Sends a command to server</para>
		/// </summary>
		/// <param name="command"></param>
		/// <param name="close"></param>
		public void SendCommand(string command, bool close)
		{
			byte[] data = CreateByteCommand(command);
			SendBytes(data, close);
		}

		/// <summary>
		/// Send bytes to the server
		/// </summary>
		/// <param name="msg">Message as a byte array</param>
		/// <param name="close">if you want to close the client after sending the message.</param>
		protected abstract void SendBytes(byte[] msg, bool close);

	}
}
