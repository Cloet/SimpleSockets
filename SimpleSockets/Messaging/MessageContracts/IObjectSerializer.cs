using System;

namespace SimpleSockets.Messaging.MessageContracts
{

	/// <summary>
	/// Interface used to send objects to connected sockets.
	/// The client and server need to have the implementation of the "Serialization" and "Deserialization" and a class with the same type.
	/// </summary>
	public interface IObjectSerializer
	{
		/// <summary>
		/// Serialize an object to bytes.
		/// e.g. you could serialize an object using json or xml
		/// </summary>
		/// <returns></returns>
		byte[] SerializeObjectToBytes(object anySerializableObject);

		/// <summary>
		/// Deserialize bytes to an object.
		/// you have to deserialize the outputted bytes from <seealso cref="SerializeObjectToBytes"/>
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="objType"></param>
		/// <returns></returns>
		object DeserializeBytesToObject(byte[] bytes, Type objType);

	}
}
