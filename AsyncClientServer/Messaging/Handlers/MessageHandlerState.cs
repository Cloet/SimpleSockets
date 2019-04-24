using System;
using AsyncClientServer.Client;
using AsyncClientServer.Messaging.Metadata;
using AsyncClientServer.Server;

namespace AsyncClientServer.Messaging.Handlers
{
	internal class MessageHandlerState: SocketStateState
	{

		public MessageHandlerState(ISocketState state, TcpClient client, ServerListener listener) : base(state, client,listener)
		{
		}


		private void Write(int receive)
		{

			//Check how much bytes have been received
			byte[] bytes = new byte[receive];

			//Append the bytes that have been read
			State.AppendRead(receive);

			if (State.Read > State.MessageSize)
			{
				//The bytes that are read of a next message
				int extraRead = State.Read - State.MessageSize;

				//bytes of the new message
				byte[] bytes2 = new byte[extraRead];
				Array.Copy(State.Buffer, receive - extraRead, bytes2, 0, extraRead);

				//bytes of current message
				bytes = new byte[receive - extraRead];
				Array.Copy(State.Buffer, 0, bytes, 0, receive - extraRead);

				//Subtract extra bytes and change state buffer to new message beginning
				State.SubtractRead(extraRead);
				State.ChangeBuffer(bytes2);

				//Change flag
				State.Flag = -3;
			}
			else if (State.Read == State.MessageSize)
			{
				//Change flag & bytes
				State.Flag = -2;
				Array.Copy(State.Buffer, 0, bytes, 0, bytes.Length);
			}
			else
			{
				//Get bytes
				bytes = State.Buffer;
			}

			//Append the received bytes to the state object.
			State.AppendBytes(bytes);

		}

		/// <summary>
		/// Writes message data to state stringBuilder
		/// </summary>
		/// <param name="receive"></param>
		public override void Receive(int receive)
		{
			//Write the message to stringBuilder
			Write(receive);

			//Check if message has been received with no extra bytes
			if (State.Flag == -2)
			{
				//Change state to new MessageHasBeenReceivedState invoke the corresponding event, then reset the state object.
				State.CurrentState = new MessageHasBeenReceivedState(State, Client,Server);
				State.CurrentState.Receive(State.Buffer.Length);
				State.Reset();
			}
			//Handle the extra bytes.
			else if (State.Flag == -3)
			{
				//Change state to MessageHasBeenReceivedState and invoke the corresponding event.
				State.CurrentState = new MessageHasBeenReceivedState(State, Client,Server);
				State.CurrentState.Receive(State.Buffer.Length);

				

				//Change state to InitialHandlerState, reset the state and then handle the extra bytes that have been send.
				State.CurrentState = new InitialHandlerState(State, Client,Server);
				State.Reset();
				State.CurrentState.Receive(State.Buffer.Length);
			}
		}
	}
}
