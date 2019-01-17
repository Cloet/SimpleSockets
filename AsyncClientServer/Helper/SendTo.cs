using System;
using System.IO;
using System.Text;

namespace AsyncClientServer.Helper
{

	/// <summary>
	/// Abstract class used to send data to server/client
	/// </summary>
	public abstract class SendTo
	{

		/// <summary>
		/// Handles errors, client doesn't need to handle error in this project so do nothing default.
		/// </summary>
		/// <param name="ex"></param>
		public virtual void ErrorHandler(string ex)
		{
			throw new Exception("Something went wrong..." + ex);
		}

		/// <summary>
		/// Creates an array of bytes
		/// <para>This method sets the location of where the file will be copied to.
		/// It also gets all bytes in a file and writes it to fileData byte array</para>
		/// </summary>
		/// <param name="Filelocation"></param>
		/// <param name="RemoteSaveLocation"></param>
		/// <returns>Byte[]</returns>
		public Byte[] CreateByteArray(string Filelocation, string RemoteSaveLocation)
		{

			try
			{
				byte[] data;


				byte[] fileName = Encoding.UTF8.GetBytes(RemoteSaveLocation); //file name
				byte[] fileData = File.ReadAllBytes(Filelocation); //file
				byte[] fileNameLen = BitConverter.GetBytes(fileName.Length); //length of file name
				data = new byte[4 + fileName.Length + fileData.Length];

				fileNameLen.CopyTo(data, 0);
				fileName.CopyTo(data, 4);
				fileData.CopyTo(data, 4 + fileName.Length);

				return data;
			}
			catch (Exception ex)
			{
				ErrorHandler(ex.ToString());
				return null;
			}
		}

		/// <summary>
		/// Creates an array of bytes
		/// <para>This methods converts a simple message to an byte array.
		/// This way it can be send using sockets</para>
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Byte[]</returns>
		public Byte[] CreateByteArray(string message)
		{
			try
			{
				byte[] data;

				byte[] fileName = Encoding.UTF8.GetBytes("NOFILE"); //not a file
				byte[] fileData = Encoding.UTF8.GetBytes(message); //message
				byte[] fileNameLen = BitConverter.GetBytes(fileName.Length); //length of "filename"
				data = new byte[4 + fileName.Length + fileData.Length];

				fileNameLen.CopyTo(data, 0); // length of filename
				fileName.CopyTo(data, 4);
				fileData.CopyTo(data, 4 + fileName.Length); // instruction

				return data;
			}
			catch (Exception ex)
			{
				ErrorHandler(ex.ToString());
				return null;
			}


		}

		/// <summary>
		/// Creates an array of bytes
		/// <para>This method serializes an object of type "SerializableObject" and converts it to xml.
		/// This xml string will be converted to bytes and send using sockets and deserialized when it arrives.</para>
		/// </summary>
		/// <param name="b"></param>
		/// <returns>Byte[]</returns>
		public Byte[] CreateByteArray(SerializableObject b)
		{
			try
			{


				byte[] data;

				byte[] fileName = Encoding.UTF8.GetBytes("OBJECT"); //not a file
				string message = b.SerializeToXml();
				byte[] fileData = Encoding.UTF8.GetBytes(message); //message
				byte[] fileNameLen = BitConverter.GetBytes(fileName.Length); //length of "filename"
				data = new byte[4 + fileName.Length + fileData.Length];

				fileNameLen.CopyTo(data, 0); // length of filename
				fileName.CopyTo(data, 4);
				fileData.CopyTo(data, 4 + fileName.Length); // instruction

				return data;
			}
			catch (Exception ex)
			{
				ErrorHandler(ex.ToString());
				return null;
			}


		}

	}
}
