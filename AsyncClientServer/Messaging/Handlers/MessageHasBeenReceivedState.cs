using System;
using System.Collections.Generic;
using System.Text;
using AsyncClientServer.Client;
using AsyncClientServer.Messaging.Compression.Stream;
using AsyncClientServer.Messaging.MessageContract;
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

			//Decompress message bytes
			if (Client == null)
				receivedMessageBytes = Server.ByteCompressor.DecompressBytes(receivedMessageBytes);
			if (Server == null)
				receivedMessageBytes = Client.ByteCompressor.DecompressBytes(receivedMessageBytes);

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
					Server.InvokeCustomHeaderReceived(State.Id, text, ReplaceHeader(State.Header,"<h>","</h>"));
				else if (State.Header.EndsWith("</MC>") && State.Header.StartsWith("<MC>"))
					HandleMessageBroker(receivedMessageBytes);
				else
					throw new Exception("Incorrect header received.");


				return;
			}
			
			if (Server == null)
			{
				if (State.Header == "MESSAGE")
					Client.InvokeMessage(text);
				else if (State.Header.EndsWith("</h>") && State.Header.StartsWith("<h>"))
					Client.InvokeCustomHeaderReceived(text, ReplaceHeader(State.Header, "<h>", "</h>"));
				else if (State.Header.EndsWith("</MC>") && State.Header.StartsWith("<MC>"))
					HandleMessageBroker(receivedMessageBytes);
				else
					throw new Exception("Incorrect header received.");


				return;
			}


		}

		private void HandleMessageBroker(byte[] receivedBytes)
		{
			var header = ReplaceHeader(State.Header, "<MC>", "</MC>");

			if (Client == null)
			{
				var contract = GetCorrespondingMessageContract(Server.GetMessageContracts(), header);
				contract?.RaiseOnMessageReceived(Server,State.Id,contract.DeserializeToObject(receivedBytes), header);

				return;
			}

			if (Server == null)
			{
				var contract = GetCorrespondingMessageContract(Client.GetMessageContracts(), header);
				contract?.RaiseOnMessageReceived(Client,-1,contract.DeserializeToObject(receivedBytes), header);
			}

		}

		private IMessageContract GetCorrespondingMessageContract(IList<IMessageContract> contracts, string header)
		{
			foreach (var contract in contracts)
			{
				if (contract.MessageHeader == header)
					return contract;
			}

			return null;
		}

		private string ReplaceHeader(string txt, string beginTag, string endTag)
		{
			string header = ReplaceFirst(txt, beginTag, "");
			header = ReplaceLast(header, endTag, "");
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
