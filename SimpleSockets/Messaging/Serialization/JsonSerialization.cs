using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleSockets.Messaging.Serialization
{
	internal class JsonSerialization: IObjectSerializer
	{

		public T DeserializeJson<T>(byte[] bytes) {
			if (bytes.Length == 0 || bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			
			return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
		}

		public object DeserializeBytesToObject(byte[] bytes, Type objType)
		{
			if (bytes.Length == 0 || bytes == null)
				throw new ArgumentNullException(nameof(bytes));

			return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), objType);
		}

		public byte[] SerializeObjectToBytes(object anySerializableObject)
		{
			if (anySerializableObject== null)
				return null;

			var json = JsonConvert.SerializeObject(anySerializableObject, Formatting.Indented, 
				new JsonSerializerSettings { 
				NullValueHandling = NullValueHandling.Ignore, 
				DateTimeZoneHandling = DateTimeZoneHandling.Utc 
				});

			return Encoding.UTF8.GetBytes(json);

		}

	}
}
