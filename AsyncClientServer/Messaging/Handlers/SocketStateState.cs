using AsyncClientServer.Client;
using AsyncClientServer.Compression;
using AsyncClientServer.Cryptography;
using AsyncClientServer.Messaging.Metadata;
using AsyncClientServer.Server;

namespace AsyncClientServer.Messaging.Handlers
{
	internal abstract class SocketStateState
	{

		protected ISocketState State;
		//Client is used to invoke message/file received event (not necessary when using on the server side.)
		protected TcpClient Client = null;
		protected ServerListener Server = null;
		protected Encryption Encrypter;
		protected FileCompression FileEncrypter;
		protected FolderCompression FolderEncrypter;

		protected SocketStateState(ISocketState state, TcpClient client, ServerListener listener)
		{
			State = state;
			Client = client;
			Server = listener;

			if (client == null)
			{
				Encrypter = Server.Encrypter;
				FileEncrypter = Server.FileCompressor;
				FolderEncrypter = Server.FolderCompressor;
			}

			if (Server == null)
			{
				Encrypter = client.Encrypter;
				FileEncrypter = client.FileCompressor;
				FolderEncrypter = client.FolderCompressor;
			}

		}

		/// <summary>
		/// Handles the current state where the state object is currently in.
		/// </summary>
		/// <param name="receive">How much bytes have been received from the client/server</param>
		public abstract void Receive(int receive);

	}
}
