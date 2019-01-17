using System;

namespace AsyncClientServer.Helper
{
	/// <summary>
	/// Interface for SerializableObject
	/// </summary>
	public interface ISerializableObject
	{

		string SerializeToXml();

		Object DeserializeToObject(string xml);
	}
}
