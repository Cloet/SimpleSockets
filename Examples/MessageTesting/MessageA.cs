using System;
using System.IO;
using System.Text;
using SimpleSockets;
using SimpleSockets.Messaging.MessageContracts;
using SimpleSockets.Messaging.Metadata;

namespace MessageTesting
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
