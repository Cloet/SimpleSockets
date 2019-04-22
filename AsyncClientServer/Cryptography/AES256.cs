using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer.Cryptography
{
	public class Aes256: Encryption
	{

		public Aes256()
		{
			_key = Encoding.UTF8.GetBytes("AEJ46SDLZOEER467");
			_IV = Encoding.UTF8.GetBytes("JFKZER82340qsdDF");

		}


		public Aes256(string key, string IV)
		{
			_key = Encoding.UTF8.GetBytes(key);
			_IV = Encoding.UTF8.GetBytes(IV);
		}

		public Aes256(byte[] key, byte[] IV)
		{
			_key = key;
			_IV = IV;
		}

		private byte[] _key;
		private byte[] _IV;


		//String

		/// <summary>
		/// Encrypt a string to bytes
		/// </summary>
		/// <param name="plainText"></param>
		/// <returns></returns>
		public override byte[] EncryptStringToBytes(string plainText)
		{
			// Check arguments.
			if (plainText == null || plainText.Length <= 0)
				throw new ArgumentNullException("plainText");
			byte[] encrypted;

			// Create an Aes object
			// with the specified key and IV.
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = _key;
				aesAlg.IV = _IV;

				// Create an encryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for encryption.
				using (MemoryStream msEncrypt = new MemoryStream())
				{
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{

						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
						{
							//Write all data to the stream.
							swEncrypt.Write(plainText);
						}
						encrypted = msEncrypt.ToArray();
					}
				}
			}


			// Return the encrypted bytes from the memory stream.
			return encrypted;

		}

		/// <summary>
		/// Decrypt bytes to string
		/// </summary>
		/// <param name="cipherText"></param>
		/// <returns></returns>
		public override string DecryptStringFromBytes(byte[] cipherText)
		{

			try
			{
				// Check arguments.
				if (cipherText == null || cipherText.Length <= 0)
					throw new ArgumentNullException("cipherText");

				byte[] decryptedBytes = new byte[cipherText.Length];

				// Declare the string used to hold
				// the decrypted text.
				string plaintext = null;

				// Create an Aes object
				// with the specified key and IV.
				using (Aes aesAlg = Aes.Create())
				{
					aesAlg.Key = _key;
					aesAlg.IV = _IV;

					// Create a decryptor to perform the stream transform.
					ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

					// Create the streams used for decryption.
					using (MemoryStream msDecrypt = new MemoryStream(cipherText))
					{
						using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
						{
							using (StreamReader srDecrypt = new StreamReader(csDecrypt))
							{
								// Read the decrypted bytes from the decrypting stream
								// and place them in a string.
								plaintext = srDecrypt.ReadToEnd();
							}
						}
					}

				}

				return plaintext;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}



		//Files

		/// <summary>
		/// Creates a random salt that will be used to encrypt your file. This method is required on FileEncrypt.
		/// </summary>
		/// <returns></returns>
		private byte[] GenerateRandomSalt()
		{
			byte[] data = new byte[32];

			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				for (int i = 0; i < 10; i++)
				{
					// Fille the buffer with the generated data
					rng.GetBytes(data);
				}
			}

			return data;
		}

		/// <summary>
		/// Encrypts a file from its path and a plain password.
		/// </summary>
		/// <param name="inputFile"></param>
		public override void FileEncrypt(string inputFile)
		{

			//generate random salt
			byte[] salt = GenerateRandomSalt();

			//create output file name
			FileStream fsCrypt = new FileStream(inputFile + Extension, FileMode.Create);

			//convert password string to byte array
			byte[] passwordBytes = _key;

			//Set Rijndael symmetric encryption algorithm
			RijndaelManaged AES = new RijndaelManaged();
			AES.KeySize = 256;
			AES.BlockSize = 128;
			AES.Padding = PaddingMode.PKCS7;

			//"What it does is repeatedly hash the user password along with the salt." High iteration counts.
			var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
			AES.Key = key.GetBytes(AES.KeySize / 8);
			AES.IV = key.GetBytes(AES.BlockSize / 8);

			AES.Mode = CipherMode.CFB;

			// write salt to the begining of the output file, so in this case can be random every time
			fsCrypt.Write(salt, 0, salt.Length);

			CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

			FileStream fsIn = new FileStream(inputFile, FileMode.Open);

			//create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
			byte[] buffer = new byte[1048576];
			int read;

			try
			{
				while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
				{
					cs.Write(buffer, 0, read);
				}

				// Close up
				fsIn.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
			finally
			{
				cs.Close();
				fsCrypt.Close();
			}
		}

		/// <summary>
		/// Decrypts an encrypted file with the FileEncrypt method through its path and the plain password.
		/// </summary>
		/// <param name="inputFile"></param>
		/// <param name="outputFile"></param>
		public override void FileDecrypt(string inputFile, string outputFile)
		{
			byte[] passwordBytes = _key;
			byte[] salt = new byte[32];

			FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
			fsCrypt.Read(salt, 0, salt.Length);

			RijndaelManaged AES = new RijndaelManaged();
			AES.KeySize = 256;
			AES.BlockSize = 128;
			var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
			AES.Key = key.GetBytes(AES.KeySize / 8);
			AES.IV = key.GetBytes(AES.BlockSize / 8);
			AES.Padding = PaddingMode.PKCS7;
			AES.Mode = CipherMode.CFB;

			CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);

			FileStream fsOut = new FileStream(outputFile, FileMode.Create);

			int read;
			byte[] buffer = new byte[1048576];

			try
			{
				while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
				{
					fsOut.Write(buffer, 0, read);
				}
			}
			catch (CryptographicException ex_CryptographicException)
			{
				throw new CryptographicException(ex_CryptographicException.Message, ex_CryptographicException);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}

			try
			{
				cs.Close();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
			finally
			{
				fsOut.Close();
				fsCrypt.Close();
			}
		}

	}


}

