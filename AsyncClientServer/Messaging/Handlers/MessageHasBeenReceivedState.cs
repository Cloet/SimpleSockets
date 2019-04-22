using System;
using System.Text;
using AsyncClientServer.Client;
using AsyncClientServer.Messaging.Metadata;
using AsyncClientServer.Server;

namespace AsyncClientServer.Messaging.Handlers
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
				if (State.Header == "MESSAGE")
					Server.InvokeMessageReceived(State.Id, text);
				else if (State.Header == "COMMAND")
					Server.InvokeCommandReceived(State.Id, text);
				else if (State.Header == "OBJECT")
					Server.InvokeObjectReceived(State.Id, text);
				else
					throw new Exception("Incorrect header received.");


				return;
			}

			if (Server == null)
			{
				if (State.Header == "MESSAGE")
					Client.InvokeMessage(text);
				else if (State.Header == "COMMAND")
					Client.InvokeCommand(text);
				else if (State.Header == "OBJECT")
					Client.InvokeObject(text);
				else
					throw new Exception("Incorrect header received.");


				return;
			}


		}
	}
}
