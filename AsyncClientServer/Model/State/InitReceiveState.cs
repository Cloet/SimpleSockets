using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.State
{
	public class InitReceiveState : ClientState
	{

		private readonly string[] _messageTypes = { "FILETRANSFER", "COMMAND", "MESSAGE", "OBJECT" };

		public InitReceiveState(AsyncClient client) : base(client)
		{
		}

		public override void Receive(IStateObject state, int receive)
		{

			if (state.Flag == 0)
			{
				state.MessageSize = BitConverter.ToInt32(state.Buffer, 0);
				state.HeaderSize = BitConverter.ToInt32(state.Buffer, 4);
				state.Header = Encoding.UTF8.GetString(state.Buffer, 8, state.HeaderSize);
				state.Flag++;
			}

			if (_messageTypes.Contains(state.Header))
			{
				Client.ChangeState(new ReceiveMessageState(Client));
				Client.CState.Receive(state, receive);
				return;
			}

			Client.ChangeState(new ReceiveFileState(Client));
			Client.CState.Receive(state, receive);

		}
	}
}
