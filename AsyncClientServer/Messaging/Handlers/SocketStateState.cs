using AsyncClientServer.Client;
using AsyncClientServer.Messaging.Compression;
using AsyncClientServer.Messaging.Cryptography;
using AsyncClientServer.Messaging.Metadata;
using AsyncClientServer.Server;

namespace AsyncClientServer.Messaging.Handlers
{
	internal abstract class SocketStateState
	{

		protected ISocketState State;
		//Client is used to invoke message/file received event (not necessary when using on the server side.)
		protected SocketClient Client = null;
		protected ServerListener Server = null;
		protected MessageEncryption Encrypter;
		protected FileCompression FileCompressor;
		protected FolderCompression FolderCompressor;

		protected SocketStateState(ISocketState state, SocketClient client, ServerListener listener)
		{
			State = state;
			Client = client;
			Server = listener;

			if (client == null)
			{
				Encrypter = Server.MessageEncryption;
				FileCompressor = Server.FileCompressor;
				FolderCompressor = Server.FolderCompressor;
			}

			if (Server == null)
			{
				Encrypter = client.MessageEncryption;
				FileCompressor = client.FileCompressor;
				FolderCompressor = client.FolderCompressor;
			}

		}

		/// <summary>
		/// Handles the current state where the state object is currently in.
		/// </summary>
		/// <param name="receive">How much bytes have been received from the client/server</param>
		public abstract void Receive(int receive);

	}
}
