using System;
using System.Linq;
using System.Text;
using AsyncClientServer.Client;
using AsyncClientServer.Server;

namespace AsyncClientServer.StateObject.StateObjectState
{
	public class InitialHandlerState : StateObjectState
	{
		//The types of messages that can be send.
		private readonly string[] _messageTypes = { "COMMAND", "MESSAGE", "OBJECT" };

		public InitialHandlerState(IStateObject state, TcpClient client, ServerListener listener) : base(state, client,listener)
		{
		}

		private static int prevRead = 0;

		/// <summary>
		/// The first check when a message is received.
		/// </summary>
		/// <param name="receive"></param>
		public override void Receive(int receive)
		{
			if(receive < 10)
			{

				prevRead = receive;
				if (Server != null)
				{
					Server.StartReceiving(State, receive);
					return;
				}

				if (Client != null)
				{
					Client.StartReceiving(State, receive);
					return;
				}

			}

			receive += prevRead;
			prevRead = 0;


			//First check
			if (State.Flag == 0)
			{
				//Get the size of the message, the header size and the header string and save it to the StateObject.
				State.MessageSize = BitConverter.ToInt32(State.Buffer, 0);
				State.HeaderSize = BitConverter.ToInt32(State.Buffer, 4);

				//Check if kthe message/file is encrypted and sets the header
				CheckIfEncryptedAndSetHeader();

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
				State.CurrentState = new MessageHandlerState(State, Client,Server);
				State.CurrentState.Receive(receive - 8 - State.HeaderSize);
				return;
			}

			//If it's a file then set state to new FileHandlerState.
			State.CurrentState = new FileHandlerState(State, Client,Server);
			State.CurrentState.Receive(receive - 8 - State.HeaderSize);

		}




		//Checks if the message/file is encrypted and sets the header.
		private void CheckIfEncryptedAndSetHeader()
		{
			byte[] headerBytes = new byte[State.HeaderSize];
			Array.Copy(State.Buffer, 8, headerBytes, 0, State.HeaderSize);


			//Copy the first 10 bytes of the header to a new byte array.
			//byte[] prefixBytes = new byte[4];
			//Array.Copy(headerBytes, 0, prefixBytes, 0, 4);

			if (headerBytes.Length > 10)
			{
				//Copy the first 10 bytes of the header to a new byte array.
				byte[] encryptedBytes = new byte[10];
				Array.Copy(headerBytes, 0, encryptedBytes, 0, 10);

				//The first 10 bytes of the header are unecrypted in a encrypted message and read "ENCRYPTED_".
				if (Encoding.UTF8.GetString(encryptedBytes) == "ENCRYPTED_")
				{
					//Get the header without the "ENCRYPTED_" String, then set the state to Encrypted.
					byte[] newHeader = new byte[headerBytes.Length - 10];
					Array.Copy(headerBytes, 10, newHeader, 0, newHeader.Length);
					State.Header = Aes265.DecryptStringFromBytes_Aes(newHeader);
					State.Encrypted = true;
				}
			}


			//If the message is not encrypted, convert the bytes to string
			if (State.Header == string.Empty)
				State.Header = Encoding.UTF8.GetString(headerBytes);

		}

	}
}
