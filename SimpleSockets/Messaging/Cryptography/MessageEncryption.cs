using System.IO;

namespace SimpleSockets.Messaging.Cryptography
{
	public interface IMessageEncryption
	{
		/// <summary>
		/// Extension of encrypted File.
		/// </summary>
		string Extension { get; }

		/// <summary>
		/// Encrypt bytes to bytes
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns>The encrypted bytes</returns>
		byte[] EncryptBytes(byte[] bytes);

		/// <summary>
		/// Decrypt bytes to bytes.
		/// </summary>
		/// <param name="cipherText"></param>
		/// <returns>The decrypted string.</returns>
		byte[] DecryptBytes(byte[] cipherText);

		/// <summary>
		/// Encrypts a file from its path and a plain password.
		/// <para>The file has to be saved at the same location as the inputfile</para>
		/// </summary>
		/// <param name="inputFile"></param>
		/// <param name="outputFile"></param>
		FileInfo FileEncrypt(string inputFile, string outputFile);

		/// <summary>
		/// Decrypts an encrypted file with the FileEncrypt method through its path and the plain password.
		/// </summary>
		/// <param name="inputFile"></param>
		/// <param name="outputFile"></param>
		FileInfo FileDecrypt(string inputFile, string outputFile);

	}
}
