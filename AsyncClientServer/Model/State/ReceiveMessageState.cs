using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.State
{
	public class ReceiveMessageState : ClientState
	{
		public ReceiveMessageState(AsyncClient client) : base(client)
		{
		}

		public override void Receive(IStateObject state, int receive)
		{
			if (state.Flag == 1)
			{
				string msg = Encoding.UTF8.GetString(state.Buffer, 8 + state.HeaderSize,
					receive - (8 + state.HeaderSize));
				state.Append(msg);
				state.AppendRead(msg.Length);
			}
			else
			{
				string msg = Encoding.UTF8.GetString(state.Buffer, 0, receive);
				state.Append(msg);
				state.AppendRead(msg.Length);
			}

		}

	}
}
