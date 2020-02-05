using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SimpleSockets.Messaging.MessageContracts;
using SimpleSockets.Messaging.Serialization;

namespace MessageTesting
{
	public class BinarySerializer: IObjectSerializer
	{

		public byte[] SerializeObjectToBytes(object anySerializableObject)
		{
			using (var memStream = new MemoryStream())
			{
				new BinaryFormatter().Serialize(memStream, anySerializableObject);
				return memStream.ToArray();
			}
		}

		public object DeserializeBytesToObject(byte[] bytes, Type objType)
		{
			using (var memStream = new MemoryStream(bytes))
			{
				return new BinaryFormatter().Deserialize(memStream);
			}
		}

		public T DeserializeJson<T>(byte[] bytes)
		{
			throw new NotImplementedException();
		}
	}
}
