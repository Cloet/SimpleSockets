using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model.State
{
	public class ReceiveMessageState : ClientState
	{
		public ReceiveMessageState(IAsyncClient client) : base(client)
		{
		}

		public ReceiveMessageState(IAsyncSocketListener server) : base(server)
		{
		}

		public void FirstWrite(IStateObject state, int receive)
		{

			//Creates a new byte array and copy data to it
			string msg = "";
			if (receive - 8 - state.HeaderSize > state.MessageSize)
			{
				msg = Encoding.UTF8.GetString(state.Buffer, 8 + state.HeaderSize, state.MessageSize);
				if (Client != null)
				{
					Client.ChangeState(new MessageReceivedState(Client));
				}
				if (Server != null)
				{
					Server.CurrentState = new MessageReceivedState(Server);
				}

				state.Flag = -3;
			}
			else
			{
				msg = Encoding.UTF8.GetString(state.Buffer, 8 + state.HeaderSize, receive - (8 + state.HeaderSize));
			}

			state.Append(msg);
			state.AppendRead(msg.Length);

			//Reset the buffer length
			if (state.Buffer.Length < state.BufferSize)
			{
				state.ChangeBuffer(new byte[state.BufferSize]);
			}

			if (receive < state.BufferSize)
			{
				Server.CurrentState = new MessageReceivedState(Server);
			}

			//Increment flag
			state.Flag++;


		}

		public void NormalWrite(IStateObject state, int receive)
		{
			string msg = "";
			state.AppendRead(receive);

			if (state.Read > state.MessageSize)
			{
				state.Flag = -3;
				msg = Encoding.UTF8.GetString(state.Buffer, 0, state.MessageSize);
				if (Client != null)
				{
					Client.ChangeState(new MessageReceivedState(Client));
				}
				if (Server != null)
				{
					Server.CurrentState = new MessageReceivedState(Server);
				}

				state.SubtractRead(state.Read - state.MessageSize);
			}
			else
			{
				msg = Encoding.UTF8.GetString(state.Buffer, 0, receive);
			}
			state.Append(msg);
			

		}



		public override void Receive(IStateObject state, int receive)
		{
			if (state.Flag == 1)
			{
				FirstWrite(state, receive);
			}
			else
			{
				NormalWrite(state, receive);
			}

			if (state.Flag == -3)
			{
				if (Client != null)
				{
					Client.CState.Receive(state, receive);
				}

				if (Server != null)
				{
					Server.CurrentState.Receive(state, receive);
				}


			}
		}

	}
}
