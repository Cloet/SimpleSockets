using SimpleSockets.Messaging;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleSockets {

    internal static class PacketHelper {

		// Keys used in internalinfo
		internal static string CALLBACK => "DynamicCallback";

		internal static string OBJECTTYPE => "Type";

		internal static string GUID => "Guid";

		internal static string PACKETPART => "Part";

		internal static string TOTALPACKET => "TotalPart";

		internal static string DESTPATH => "DestinationPath";

		internal static string REQPATH => "RequestPath";

		internal static string REQUEST => "Request";

		internal static string EXP => "ExpirationDate";

		internal static string RESGUID => "ResGUID";

		internal static string RESPONSE => "Response";

		internal static string EXCEPTION => "Error";

		// Escape byte
		internal static byte ESCAPE = 0xBC;

		// End of message byte
		internal static byte EOF = 0x00;

		/// <summary>
		/// Encodes a byte array.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		internal static byte[] EncodeByteArray(byte[] array) {
			var output = new byte[0];

			if (array == null || array.Length == 0)
				return output;

			using (var memStream = new MemoryStream()) {
				for (var i = 0; i < array.Length; i++)
				{
					if (array[i] == ESCAPE)
					{
						memStream.WriteByte(ESCAPE);
						memStream.WriteByte(ESCAPE);
					}
					else
						memStream.WriteByte(array[i]);
				}
				memStream.WriteByte(ESCAPE);
				memStream.WriteByte(EOF);
				output = memStream.ToArray();
			}

			return output;
		}

		/// <summary>
		/// Merges 2 byte arrays into 1
		/// </summary>
		/// <param name="arr1"></param>
		/// <param name="arr2"></param>
		/// <returns></returns>
        internal static byte[] MergeByteArrays(byte[] arr1 , byte[] arr2) {

            if (arr1 == null && arr2 == null)
                return new byte[0];

            byte[] newArray = new byte[ (arr1 == null ? 0 : arr1.Length) + (arr2 == null ? 0 : arr2.Length)];
            arr1?.CopyTo(newArray,0);
            arr2?.CopyTo(newArray, (arr1 == null ? 0 : arr1.Length));
            return newArray;
        }

		public static string ByteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}

		public static byte[] StringToByteArray(String hex)
		{
			int NumberChars = hex.Length;
			byte[] bytes = new byte[NumberChars / 2];
			for (int i = 0; i < NumberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}

		/// <summary>
		/// Maps the source object to target object.
		/// </summary>
		/// <typeparam name="T">Type of target object.</typeparam>
		/// <typeparam name="TU">Type of source object.</typeparam>
		/// <param name="target">Target object.</param>
		/// <param name="source">Source object.</param>
		/// <returns>Updated target object.</returns>
		internal static T Map<T>(this T target, Packet source) where T: Packet
		{
			// get property list of the target object.
			// this is a reflection extension which simply gets properties (CanWrite = true).
			var tprops = target.GetType().GetProperties();

			tprops.Where(x => x.CanWrite == true).ToList().ForEach(prop =>
			{
				// check whether source object has the the property
				var sp = source.GetType().GetProperty(prop.ToString());
				if (sp != null)
				{
					// if yes, copy the value to the matching property
					var value = sp.GetValue(source, null);
					target.GetType().GetProperty(prop.ToString()).SetValue(target, value, null);
				}
			});

			target.AdditionalInternalInfo = source.AdditionalInternalInfo;

			return target;
		}

	}

}