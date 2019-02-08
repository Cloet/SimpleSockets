using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AsyncClientServer.XmlSerialization
{

	public static class XmlSerialization
	{

		/// <summary>
		/// Serializes the object and returns a string
		/// </summary>
		/// <returns>String</returns>
		public static string SerializeToXml(object obj)
		{
			try
			{
				var xmlSer = new XmlSerializer(obj.GetType());

				using (var sww = new StringWriter())
				{
					using (var writer = XmlWriter.Create(sww))
					{
						xmlSer.Serialize(writer, obj);
						return sww.ToString();
					}
				}
			}
			catch (Exception)
			{
				throw new Exception("Unable to serialize the object of type " + obj.GetType() + " to an xml string.");
			}

		}

		/// <summary>
		/// Converts the xml string to an generic Object
		/// <para>You will have to cast to the corresponding object</para>
		/// </summary>
		/// <param name="xml"></param>
		/// <returns>Object</returns>
		public static T DeserializeTo<T>(string xml)
		{
			try
			{
				var xmlSer = new XmlSerializer(typeof(T));
				var stringReader = new StringReader(xml);
				return (T) xmlSer.Deserialize(stringReader);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to convert xml string back to an object of type " + typeof(T) + ".\n" + ex.ToString());
			}
		}


		/// <summary>
		/// Converts object to xml and writes it to a file at a given location.
		/// <para>The object needs to contain the [Serializable] tag.</para>
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="path"></param>
		public static void SerializeAndWrite(object obj, string path)
		{
			try
			{

				string xml = SerializeToXml(obj);

				if (File.Exists(path))
				{
					File.Delete(path);
				}

				using (var writer = new StreamWriter(File.Open(path, FileMode.CreateNew)))
				{
					writer.Write(xml);
					writer.Close();
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to serialize and write xml to file with object of type " + obj.GetType() +
				                    ".\n" + ex);
			}
		}

		/// <summary>
		/// Get all text in a file and converts it to an object of a certain type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <returns></returns>
		public static T DeserializeFromFileTo<T>(string path)
		{

			try
			{
				var xml = File.ReadAllText(path);
				return DeserializeTo<T>(xml);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to deserialize xml from file to object of type " + typeof(T) + ".\n" +
				                    ex);
			}
		}

	}
}
