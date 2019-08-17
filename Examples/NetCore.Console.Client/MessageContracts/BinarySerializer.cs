using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using SimpleSockets.Messaging.MessageContract;

namespace NetCore.Console.Client.MessageContracts
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

		public object DeserializeBytesToObject(byte[] bytes)
		{
			using (var memStream = new MemoryStream(bytes))
			{
				return new BinaryFormatter().Deserialize(memStream);
			}
		}

	}
}
