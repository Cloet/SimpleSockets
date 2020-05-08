using System;
using System.Text;
using Newtonsoft.Json;

namespace SimpleSockets.Helpers.Serialization {
    
    internal static class SerializationHelper {


        internal static T DeserializeJson<T>(byte[] bytes) {
			if (bytes.Length == 0 || bytes == null)
				throw new ArgumentNullException(nameof(bytes));

			return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
		}

		internal static object DeserializeBytesToObject(byte[] bytes, Type objType)
		{
			if (bytes.Length == 0 || bytes == null)
				throw new ArgumentNullException(nameof(bytes));

			return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), objType);
		}

		internal static byte[] SerializeObjectToBytes(object anySerializableObject)
		{
			if (anySerializableObject== null)
				return null;

			var json = JsonConvert.SerializeObject(anySerializableObject, Formatting.Indented, 
				new JsonSerializerSettings { 
				NullValueHandling = NullValueHandling.Ignore, 
				DateTimeZoneHandling = DateTimeZoneHandling.Utc,
				TypeNameHandling = TypeNameHandling.All
				});

			return Encoding.UTF8.GetBytes(json);

		}

    }
}