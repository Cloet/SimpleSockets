using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.ClientState
{
	public class InitialHandlerState: StateObjectState
	{
		private readonly string[] _messageTypes = { "FILETRANSFER", "COMMAND", "MESSAGE", "OBJECT" };

		public InitialHandlerState(IStateObject state) : base(state, null)
		{
		}

		public InitialHandlerState(IStateObject state, IAsyncClient client) : base(state, client)
		{
		}

		public override void Receive(int receive)
		{
			if (State.Flag == 0)
			{
				State.MessageSize = BitConverter.ToInt32(State.Buffer, 0);
				State.HeaderSize = BitConverter.ToInt32(State.Buffer, 4);
				State.Header = Encoding.UTF8.GetString(State.Buffer, 8, State.HeaderSize);

				byte[] bytes = new byte[receive - (8 + State.HeaderSize)];
				Array.Copy(State.Buffer, 8 + State.HeaderSize, bytes, 0, receive - (8 + State.HeaderSize));
				State.ChangeBuffer(bytes);
				State.Flag++;
			}

			if (_messageTypes.Contains(State.Header))
			{
				State.CurrentState = new MessageHandlerState(State,Client);
				State.CurrentState.Receive(receive - 8 - State.HeaderSize);
				return;
			}

			State.CurrentState = new FileHandlerState(State, Client);
			State.CurrentState.Receive(receive - 8 - State.HeaderSize);

		}



	}
}
