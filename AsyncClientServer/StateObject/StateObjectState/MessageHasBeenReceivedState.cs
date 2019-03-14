using System;
using System.Linq;
using System.Text;
using AsyncClientServer.Client;
using AsyncClientServer.Server;
using Cryptography;

namespace AsyncClientServer.StateObject.StateObjectState
{
	public class MessageHasBeenReceivedState: StateObjectState
	{

		public MessageHasBeenReceivedState(IStateObject state) : base(state,null)
		{
		}

		public MessageHasBeenReceivedState(IStateObject state, IAsyncClient client) : base(state, client)
		{
		}

		/// <summary>
		/// Invokes MessageReceived when a message has been fully received.
		/// </summary>
		/// <param name="receive"></param>
		public override void Receive(int receive)
		{
			//Decode the received message, decrypt when necessary.
			var text = string.Empty;

			byte[] receivedMessageBytes = State.ReceivedBytes;

			//Check if the bytes are encrypted or not.
			if (State.Encrypted)
				text = AES256.DecryptStringFromBytes_Aes(receivedMessageBytes);
			else
				text = Encoding.UTF8.GetString(receivedMessageBytes);

			if (Client == null)
			{
				AsyncSocketListener.Instance.InvokeMessageReceived(State.Id,State.Header,text);
				return;
			}

			Client.InvokeMessage(State.Header, text);

		}
	}
}
