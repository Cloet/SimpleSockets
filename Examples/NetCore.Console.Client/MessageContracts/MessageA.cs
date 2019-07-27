using System;
using System.Collections.Generic;
using System.Text;
using AsyncClientServer;
using AsyncClientServer.Messaging.MessageContract;

namespace NetCore.Console.Client.MessageContracts
{
	public class MessageA: IMessageContract
	{

		public MessageA(string header)
		{
			MessageHeader = header;
		}

		public string MessageHeader { get; set; }

		public byte[] SerializeToBytes()
		{
			return Encoding.UTF8.GetBytes("This is a MessageContract of the object : MessageA");
		}

		public object DeserializeToObject(byte[] objectBytes)
		{
			return Encoding.UTF8.GetString(objectBytes);
		}

		public event Action<AsyncSocket,int, object, string> OnMessageReceived;
		public void RaiseOnMessageReceived(AsyncSocket socket,int clientId, object message, string header)
		{
			OnMessageReceived?.Invoke(socket,clientId, message, header);
		}
	}
}
