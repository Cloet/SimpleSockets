using System.Text;

namespace AsyncClientServer.Messaging.Cryptography
{
	public abstract class MessageEncryption
	{

		public string Extension => ".EncryptedPART";

		/// <summary>
		/// Encrypt a string to bytes
		/// </summary>
		/// <param name="plainText"></param>
		/// <returns>The encrypted bytes</returns>
		public byte[] EncryptStringToBytes(string plainText)
		{
			byte[] toEncrypt = Encoding.UTF8.GetBytes(plainText);
			return EncryptBytes(toEncrypt);
		}

		/// <summary>
		/// Decrypt bytes to string.
		/// </summary>
		/// <param name="cipherText"></param>
		/// <returns>The decrypted string.</returns>
		public string DecryptStringFromBytes(byte[] cipherText)
		{
			byte[] decryptedBytes = DecryptBytes(cipherText);
			return Encoding.UTF8.GetString(decryptedBytes);
		}

		/// <summary>
		/// Encrypt bytes to bytes
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns>The encrypted bytes</returns>
		public abstract byte[] EncryptBytes(byte[] bytes);

		/// <summary>
		/// Decrypt bytes to bytes.
		/// </summary>
		/// <param name="cipherText"></param>
		/// <returns>The decrypted string.</returns>
		public abstract byte[] DecryptBytes(byte[] cipherText);

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
