using AsyncClientServer.Client;
using AsyncClientServer.Cryptography;
using AsyncClientServer.Server;

namespace AsyncClientServer.StateObject.MessageHandlerState
{
	public abstract class SocketStateState
	{

		protected ISocketState State;
		//Client is used to invoke message/file received event (not necessary when using on the server side.)
		protected TcpClient Client = null;
		protected ServerListener Server = null;
		protected AES256 Aes265;

		protected SocketStateState(ISocketState state, TcpClient client, ServerListener listener)
		{
			State = state;
			Client = client;
			Server = listener;

			if (client == null)
				Aes265 = Server.Aes256;
			if (Server == null)
				Aes265 = client.Aes256;
		}

		/// <summary>
		/// Handles the current state where the state object is currently in.
		/// </summary>
		/// <param name="receive">How much bytes have been received from the client/server</param>
		public abstract void Receive(int receive);

	}
}
