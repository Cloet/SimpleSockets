using AsyncClientServer.Model;

namespace AsyncClientServer.Helper
{
	public abstract class SerializableObject
	{

		/// <summary>
		/// Serializes the object to xml string.
		/// </summary>
		/// <returns>String in xml format.</returns>
		public virtual string Serialize()
		{
			return SerializeObject.SerializeToXml(this);
		}

		/// <summary>
		/// Converts an xml string back to a generic object.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns>An Object</returns>
		public virtual object Deserialize(string xml)
		{
			return SerializeObject.DeserializeToObject(xml, this);
		}

	}
}
