using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.ClientState
{
	public abstract class StateObjectState
	{

		protected IStateObject State;
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

		public abstract void Receive(int receive);

	}
}
