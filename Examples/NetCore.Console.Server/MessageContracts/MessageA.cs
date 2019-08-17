using System;
using System.Text;
using SimpleSockets;
using SimpleSockets.Messaging.MessageContract;

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

		public event Action<SimpleSocket,int, object, string> OnMessageReceived;
		public void RaiseOnMessageReceived(SimpleSocket socket,int clientId,object message, string header)
		{
			OnMessageReceived?.Invoke(socket,clientId, message, header);
		}
	}
}
