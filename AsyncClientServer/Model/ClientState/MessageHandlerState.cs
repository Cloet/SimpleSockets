using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.ClientState
{
	public class MessageHandlerState: StateObjectState
	{
		public MessageHandlerState(IStateObject state) : base(state,null)
		{
		}

		public MessageHandlerState(IStateObject state, IAsyncClient client) : base(state, client)
		{
		}

		public void Write(int receive)
		{

			//Check how much bytes have been received
			byte[] bytes = new byte[receive];

			State.AppendRead(receive);

			if (State.Read > State.MessageSize)
			{
				int extraRead = State.Read - State.MessageSize;

				byte[] bytes2 = new byte[extraRead];
				Array.Copy(State.Buffer, receive - extraRead, bytes2, 0, extraRead);

				bytes = new byte[receive - extraRead];
				Array.Copy(State.Buffer, 0, bytes, 0, receive - extraRead);

				State.SubtractRead(extraRead);
				State.ChangeBuffer(bytes2);

				State.Flag = -3;
			}
			else if (State.Read == State.MessageSize)
			{
				State.Flag = -2;
				bytes = State.Buffer;
			}
			else
			{
				bytes = State.Buffer;
			}

			string msg = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			State.Append(msg);

		}


		public override void Receive(int receive)
		{
			Write(receive);
			if (State.Flag == -2)
			{
				State.CurrentState = new MessageHasBeenReceivedState(State, Client);
				State.CurrentState.Receive(State.Buffer.Length);
				State.Reset();
			}
			else if (State.Flag == -3)
			{
				State.CurrentState = new MessageHasBeenReceivedState(State, Client);
				State.CurrentState.Receive(State.Buffer.Length);
				State.CurrentState = new InitialHandlerState(State, Client);
				State.Reset();
				State.CurrentState.Receive(State.Buffer.Length);
			}
		}
	}
}
