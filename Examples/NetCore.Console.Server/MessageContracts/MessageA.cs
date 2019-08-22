using System;
using System.Text;
using SimpleSockets;
using SimpleSockets.Messaging.MessageContract;
using SimpleSockets.Messaging.Metadata;

namespace NetCore.Console.Server.MessageContracts
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

		public event Action<SimpleSocket,IClientInfo, object, string> OnMessageReceived;
		public void RaiseOnMessageReceived(SimpleSocket socket,IClientInfo client,object message, string header)
		{
			OnMessageReceived?.Invoke(socket,client, message, header);
		}
	}
}
