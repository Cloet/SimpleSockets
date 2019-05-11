using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Messaging.Metadata;

namespace AsyncClientServer.Messaging
{
	public class Message
	{

		public MessageType MessageType { get; set; }
		public byte[] MessageBytes { get; set; }
		internal ISocketState SocketState { get; set; }

		public Message(byte[] bytes, MessageType type)
		{
			MessageType = type;
			MessageBytes = bytes;
		}

		internal Message(byte[] bytes, MessageType type, ISocketState state)
		{
			MessageType = type;
			MessageBytes = bytes;
			SocketState = state;
		}

	}
}
