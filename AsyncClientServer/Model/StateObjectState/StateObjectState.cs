using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.StateObjectState
{
	public abstract class StateObjectState
	{

		protected IStateObject State;
		//Client is used to invoke message/file received event (not necessary when using on the server side.)
		protected IAsyncClient Client = null;

		protected StateObjectState(IStateObject state)
		{
			State = state;
		}

		protected StateObjectState(IStateObject state, IAsyncClient client)
		{
			State = state;
			Client = client;
		}

		/// <summary>
		/// Handles the current state where the state object is currently in.
		/// </summary>
		/// <param name="receive">How much bytes have been received from the client/server</param>
		public abstract void Receive(int receive);

	}
}
