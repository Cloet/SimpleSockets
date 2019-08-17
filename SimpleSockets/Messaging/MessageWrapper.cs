using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets.Messaging
{
	public class MessageWrapper
	{
		public byte[] Data { get; set; }
		public bool Partial { get; set; }
		public ISocketState State { get; set; }

		public MessageWrapper(byte[] data, bool partial)
		{
			Data = data;
			Partial = partial;
		}

		public MessageWrapper(byte[] data, ISocketState state, bool partial)
		{
			Data = data;
			State = state;
			Partial = partial;
		}
	}
}
