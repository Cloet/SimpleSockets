using System.Text;
using AsyncClientServer.Client;
using AsyncClientServer.Server;

namespace AsyncClientServer.StateObject.MessageHandlerState
{
	internal class MessageHasBeenReceivedState: SocketStateState
	{

		public MessageHasBeenReceivedState(ISocketState state, TcpClient client, ServerListener listener) : base(state, client, listener)
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
				text = Encrypter.DecryptStringFromBytes(receivedMessageBytes);
			else
				text = Encoding.UTF8.GetString(receivedMessageBytes);

			if (Client == null)
			{
				Server.InvokeMessageReceived(State.Id, State.Header, text);
				return;
			}

			Client.InvokeMessage(State.Header, text);

		}
	}
}
