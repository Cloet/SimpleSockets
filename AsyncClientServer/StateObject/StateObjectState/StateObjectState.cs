using AsyncClientServer.Client;
using AsyncClientServer.Server;

namespace AsyncClientServer.StateObject.StateObjectState
{
	public abstract class StateObjectState
	{

		protected IStateObject State;
		//Client is used to invoke message/file received event (not necessary when using on the server side.)
		protected TcpClient Client = null;
		protected ServerListener Server = null;

		protected StateObjectState(IStateObject state, TcpClient client, ServerListener listener)
		{
			State = state;
			Client = client;
			Server = listener;
		}

		/// <summary>
		/// Handles the current state where the state object is currently in.
		/// </summary>
		/// <param name="receive">How much bytes have been received from the client/server</param>
		public abstract void Receive(int receive);

	}
}
