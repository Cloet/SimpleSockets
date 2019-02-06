using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.ClientState
{
	public class FileHasBeenReceivedState: StateObjectState
	{
		public FileHasBeenReceivedState(IStateObject state) : base(state)
		{
		}

		public FileHasBeenReceivedState(IStateObject state, IAsyncClient client) : base(state, client)
		{
		}

		public override void Receive(int receive)
		{
			if (Client == null)
			{
				AsyncSocketListener.Instance.InvokeFileReceived(State.Id, State.Header);
				return;
			}

			Client.InvokeFileReceived(State.Header);
		}
	}
}
