using AsyncClientServer.Model;

namespace AsyncClientServer.Helper
{
	public abstract class SerializableObject
	{

		public virtual string Serialize()
		{
			return SerializeObject.SerializeToXml(this);
		}

		public virtual object Deserialize(string xml)
		{
			return SerializeObject.DeserializeToObject(xml, this);
		}

	}
}
