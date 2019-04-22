using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer.Cryptography
{
	public abstract class Encryption
	{

		public string Extension => ".EncryptedPART";

		/// <summary>
		/// Encrypt a string to bytes
		/// </summary>
		/// <param name="plainText"></param>
		/// <returns>The encrypted bytes</returns>
		public abstract byte[] EncryptStringToBytes(string plainText);

		/// <summary>
		/// Decrypt bytes to string.
		/// </summary>
		/// <param name="cipherText"></param>
		/// <returns>The decrypted string.</returns>
		public abstract string DecryptStringFromBytes(byte[] cipherText);

		/// <summary>
		/// Encrypts a file from its path and a plain password.
		/// <para>The file has to be saved at the same location as the inputfile</para>
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
