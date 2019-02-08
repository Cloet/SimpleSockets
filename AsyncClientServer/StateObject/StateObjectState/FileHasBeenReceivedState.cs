using AsyncClientServer.Client;
using AsyncClientServer.Server;

namespace AsyncClientServer.StateObject.StateObjectState
{
	public class FileHasBeenReceivedState: StateObjectState
	{
		public FileHasBeenReceivedState(IStateObject state) : base(state)
		{
		}

		public FileHasBeenReceivedState(IStateObject state, IAsyncClient client) : base(state, client)
		{
		}

		/// <summary>
		/// Invokes the file or message that has been received.
		/// </summary>
		/// <param name="receive"></param>
		public override void Receive(int receive)
		{
			//If client == null then the file is send to the server so invoke server event else do client event.
			if (Client == null)
			{
				AsyncSocketListener.Instance.InvokeFileReceived(State.Id, State.Header);
				return;
			}

			Client.InvokeFileReceived(State.Header);
		}
	}
}
