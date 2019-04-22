using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer.Cryptography
{
	public abstract class Encryption
	{

		public string Extension => ".aes";

		/// <summary>
		/// Encrypt a string to bytes
		/// </summary>
		/// <param name="plainText"></param>
		/// <returns></returns>
		public abstract byte[] EncryptStringToBytes(string plainText);

		/// <summary>
		/// Decrypt bytes to string
		/// </summary>
		/// <param name="cipherText"></param>
		/// <returns></returns>
		public abstract string DecryptStringFromBytes(byte[] cipherText);

		/// <summary>
		/// Encrypts a file from its path and a plain password.
		/// </summary>
		/// <param name="inputFile"></param>
		public abstract void FileEncrypt(string inputFile);

		/// <summary>
		/// Decrypts an encrypted file with the FileEncrypt method through its path and the plain password.
		/// </summary>
		/// <param name="inputFile"></param>
		/// <param name="outputFile"></param>
		public abstract void FileDecrypt(string inputFile, string outputFile);

	}
}
