using SimpleSockets;
using SimpleSockets.Messaging.MessageContracts;
using SimpleSockets.Messaging.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Test.Sockets.Utils
{
	public class MessageContractImpl : IMessageContract
	{
		public string MessageHeader { get ; set ; }
		public string Message { get; set; }

		public MessageContractImpl() {
			MessageHeader = "MessageContractTest";
		}

		public event Action<SimpleSocket, IClientInfo, object, string> OnMessageReceived;

		public object DeserializeToObject(byte[] objectBytes)
		{
			Message = Encoding.UTF8.GetString(objectBytes);
			return Message;
		}

		public void RaiseOnMessageReceived(SimpleSocket socket, IClientInfo client, object message, string header)
		{
			OnMessageReceived?.Invoke(socket, client, message, header);
		}

		public byte[] SerializeToBytes()
		{
			return Encoding.UTF8.GetBytes(Message);
		}
	}
}
