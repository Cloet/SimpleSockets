using System;
using SimpleSockets.Messaging.Metadata;

namespace SimpleSockets.Messaging.MessageContract
{

	public interface IMessageContract
	{
		/// <summary>
		/// Has to be unique, the header is used to match the message with a delegate.
		/// </summary>
		string MessageHeader { get; set; }

		/// <summary>
		/// The output of this method will be sent to the matching socket.
		/// </summary>
		/// <returns></returns>
		byte[] SerializeToBytes();

		/// <summary>
		/// This method will be used to convert the output of <seealso cref="SerializeToBytes"/> to an object of choice.
		/// </summary>
		/// <param name="objectBytes"></param>
		/// <returns></returns>
		object DeserializeToObject(byte[] objectBytes);

		/// <summary>
		/// OnMessageReceived will handle receiving messages.
		/// Format = Socket:Client,MessageObject:Header
		/// </summary>
		event Action<SimpleSocket,IClientInfo, object, string> OnMessageReceived;

		/// <summary>
		/// This needs to invoke 'OnMessageReceived'
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="client"></param>
		/// <param name="message"></param>
		/// <param name="header"></param>
		void RaiseOnMessageReceived(SimpleSocket socket,IClientInfo client, object message, string header);
	}
}
