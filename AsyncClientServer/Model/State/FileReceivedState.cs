using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.State
{
	public class FileReceivedState : ClientState
	{
		public FileReceivedState(AsyncClient client) : base(client)
		{
		}

		public override void Receive(IStateObject state, int receive)
		{
			if (state.Flag == -2)
			{
				Client.InvokeAndReset(state);
				Client.ChangeState(new InitReceiveState(Client));
				Client.CState.Receive(state, receive);
			}
		}
	}
}
