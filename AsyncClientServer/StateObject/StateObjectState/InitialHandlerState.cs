using System;
using System.Linq;
using System.Text;
using AsyncClientServer.Client;
using Cryptography;

namespace AsyncClientServer.StateObject.StateObjectState
{
	public class InitialHandlerState: StateObjectState
	{
		//The types of messages that can be send.
		private readonly string[] _messageTypes = { "COMMAND", "MESSAGE", "OBJECT" };

		public InitialHandlerState(IStateObject state) : base(state, null)
		{
		}

		public InitialHandlerState(IStateObject state, IAsyncClient client) : base(state, client)
		{
		}

		/// <summary>
		/// The first check when a message is received.
		/// </summary>
		/// <param name="receive"></param>
		public override void Receive(int receive)
		{

			//TODO Check if received message has enough bytes.

			//First check
			if (State.Flag == 0)
			{
				//Get the size of the message, the header size and the header string and save it to the StateObject.
				State.MessageSize = BitConverter.ToInt32(State.Buffer, 0);
				State.HeaderSize = BitConverter.ToInt32(State.Buffer, 4);

				byte[] headerbytes =  new byte[State.HeaderSize];
				Array.Copy(State.Buffer, 8, headerbytes, 0, State.HeaderSize);

				State.Header = AES256.DecryptStringFromBytes_Aes(headerbytes);
				//State.Header = Encoding.UTF8.GetString(State.Buffer, 8, State.HeaderSize);

				//Get the bytes without the header and MessageSize and copy to new byte array.
				byte[] bytes = new byte[receive - (8 + State.HeaderSize)];
				Array.Copy(State.Buffer, 8 + State.HeaderSize, bytes, 0, receive - (8 + State.HeaderSize));
				State.ChangeBuffer(bytes);

				//Increment flag
				State.Flag++;
			}

			//If it is a message set state to new MessageHandlerState.
			if (_messageTypes.Contains(State.Header))
			{
				State.CurrentState = new MessageHandlerState(State,Client);
				State.CurrentState.Receive(receive - 8 - State.HeaderSize);
				return;
			}

			//If it's a file then set state to new FileHandlerState.
			State.CurrentState = new FileHandlerState(State, Client);
			State.CurrentState.Receive(receive - 8 - State.HeaderSize);

		}

	}
}
