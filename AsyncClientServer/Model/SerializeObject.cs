using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AsyncClientServer.Model
{

	public static class SerializeObject
	{

		/// <summary>
		/// Serializes the object and returns a string
		/// </summary>
		/// <returns>String</returns>
		public static string SerializeToXml(object obj)
		{
			try
			{
				XmlSerializer xmlSer = new XmlSerializer(obj.GetType());

				using (var sww = new StringWriter())
				{
					using (XmlWriter writer = XmlWriter.Create(sww))
					{
						xmlSer.Serialize(writer, obj);
						return sww.ToString();
					}
				}
			}
			catch (Exception)
			{
				return null;
			}

		}

		/// <summary>
		/// Converts the xml string to an generic Object
		/// <para>You will have to cast to the corresponding object</para>
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="obj"></param>
		/// <returns>Object</returns>
		public static object DeserializeToObject(string xml,object obj)
		{
			try
			{
				XmlSerializer xmlSer = new XmlSerializer(obj.GetType());
				StringReader stringReader = new StringReader(xml);
				return xmlSer.Deserialize(stringReader);
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}
}
