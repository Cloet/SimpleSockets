using System;
using System.Text;
using AsyncClientServer.Client;
using AsyncClientServer.Messaging.Metadata;
using AsyncClientServer.Server;

namespace AsyncClientServer.Messaging.Handlers
{
	internal class MessageHasBeenReceivedState: SocketStateState
	{

		public MessageHasBeenReceivedState(ISocketState state, SocketClient client, ServerListener listener) : base(state, client, listener)
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
				else if (State.Header.EndsWith("</h>") && State.Header.StartsWith("<h>"))
					Server.InvokeCustomHeaderReceived(State.Id, text, ReplaceHeader(State.Header));
				else
					throw new Exception("Incorrect header received.");


				return;
			}

			if (Server == null)
			{
				if (State.Header == "MESSAGE")
					Client.InvokeMessage(text);
				else if (State.Header.EndsWith("</h>") && State.Header.StartsWith("<h>"))
					Client.InvokeCustomHeaderReceived(text, ReplaceHeader(State.Header));
				else
					throw new Exception("Incorrect header received.");


				return;
			}


		}


		private string ReplaceHeader(string txt)
		{
			string header = ReplaceFirst(txt, "<h>", "");
			header = ReplaceLast(header, "</h>", "");
			return header;
		}

		private string ReplaceLast(string text, string search, string replace)
		{
			int pos = text.LastIndexOf(search, StringComparison.Ordinal);
			if (pos < 0)
			{
				throw new Exception("Search value does not exist.");
			}
			return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
		}

		public string ReplaceFirst(string text, string search, string replace)
		{
			int pos = text.IndexOf(search, StringComparison.Ordinal);
			if (pos < 0)
			{
				throw new Exception("Search value does not exist.");
			}
			return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
		}
	}
}
