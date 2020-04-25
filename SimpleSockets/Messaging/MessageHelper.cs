using System;
using System.Text;

namespace SimpleSockets {

    internal static class MessageHelper {

        internal static string ByteArrayToString(byte[] array) {
            var hex = new StringBuilder(array.Length * 2);
            foreach(var b in array)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        internal static byte[] StringToByteArray(string hex) {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }


        internal static byte[] MergeByteArrays(byte[] arr1 , byte[] arr2) {

            if (arr1 == null && arr2 == null)
                return new byte[0];

            byte[] newArray = new byte[ (arr1 == null ? 0 : arr1.Length) + (arr2 == null ? 0 : arr2.Length)];
            arr1?.CopyTo(newArray,0);
            arr2?.CopyTo(newArray, (arr1 == null ? 0 : arr1.Length));
            return newArray;
        }

        internal static byte[] PacketDelimiter => MergeByteArrays(Encoding.UTF8.GetBytes(Environment.NewLine), Encoding.UTF8.GetBytes(Environment.NewLine));

    }

}