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
			//Decrypt the received message
			string text = AES256.DecryptStringFromBytes_Aes(State.ReceivedBytes);

			if (Client == null)
			{
				AsyncSocketListener.Instance.InvokeMessageReceived(State.Id,State.Header,text);
				return;
			}

			Client.InvokeMessage(State.Header, text);

		}
	}
}
