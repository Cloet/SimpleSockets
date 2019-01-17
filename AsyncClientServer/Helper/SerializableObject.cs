using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AsyncClientServer.Helper
{
	/// <summary>
	/// Wrapper class for class Object
	/// <para>This class adds the ability to serialize and deserialize to xml and from xml to object</para>
	/// <para>Extend you class you want to serialize with "SerializableObject"</para>
	/// </summary>
	public abstract class SerializableObject : Object, ISerializableObject
	{

		/// <summary>
		/// Serializes the object and returns a string
		/// </summary>
		/// <returns>String</returns>
		public string SerializeToXml()
		{
			try
			{
				XmlSerializer xmlSer = new XmlSerializer(this.GetType());

				using (var sww = new StringWriter())
				{
					using (XmlWriter writer = XmlWriter.Create(sww))
					{
						xmlSer.Serialize(writer, this);
						return sww.ToString();
					}
				}
			}
			catch (Exception ex)
			{
				return null;
			}

		}

		/// <summary>
		/// Converts the xml string to an generic Object
		/// <para>You will have to cast to the corresponding object</para>
		/// </summary>
		/// <param name="xml"></param>
		/// <returns>Object</returns>
		public Object DeserializeToObject(string xml)
		{
			try
			{
				XmlSerializer xmlSer = new XmlSerializer(this.GetType());
				StringReader stringreader = new StringReader(xml);
				return xmlSer.Deserialize(stringreader);
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}
}
