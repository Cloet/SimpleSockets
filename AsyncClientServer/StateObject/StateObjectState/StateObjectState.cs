using AsyncClientServer.Client;
using AsyncClientServer.Server;

namespace AsyncClientServer.StateObject.StateObjectState
{
	public abstract class StateObjectState
	{

		protected IStateObject State;
		//Client is used to invoke message/file received event (not necessary when using on the server side.)
		protected ITcpClient Client = null;
		protected IServerListener Server = null;

		protected StateObjectState(IStateObject state, ITcpClient client, IServerListener listener)
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
