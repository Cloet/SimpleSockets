using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SimpleSockets.Messaging.Cryptography
{
	public class Aes256: IMessageEncryption
	{

		/// <summary>
		/// Extension of ecnrypted file.
		/// </summary>
		public string Extension => ".Aes";

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

		private readonly byte[] _key;
		private readonly byte[] _IV;


		/// <summary>
		/// Encrypt bytes using AES
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public byte[] EncryptBytes(byte[] bytes)
		{
			// Check arguments.
			if (bytes == null || bytes.Length <= 0)
				throw new ArgumentNullException(nameof(bytes));
			byte[] encrypted;

			// Create an Aes object
			// with the specified key and IV.
			using (var aesAlg = Aes.Create())
			{
				if (aesAlg == null) throw new Exception("Unable to create AES object...");

				aesAlg.Key = _key;
				aesAlg.IV = _IV;

				// Create an encryption to perform the stream transform.
				var encryption = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for encryption.
				using (var msEncrypt = new MemoryStream())
				{
					using (var csEncrypt = new CryptoStream(msEncrypt, encryption, CryptoStreamMode.Write))
					{
						using (var originalByteStream = new MemoryStream(bytes))
						{
							int data;
							while ((data = originalByteStream.ReadByte()) != -1)
								csEncrypt.WriteByte((byte)data);
						}
					}

					encrypted = msEncrypt.ToArray();
				}
			}


			// Return the encrypted bytes from the memory stream.
			return encrypted;
		}

		/// <summary>
		/// Decrypt bytes, encrypted with AES
		/// </summary>
		/// <param name="cipherBytes"></param>
		/// <returns></returns>
		public byte[] DecryptBytes(byte[] cipherBytes)
		{
			try
			{
				// Check arguments.
				if (cipherBytes == null || cipherBytes.Length <= 0)
					throw new ArgumentNullException(nameof(cipherBytes));

				byte[] outputBytes;

				// Create an Aes object
				// with the specified key and IV.
				using (var aesAlg = Aes.Create())
				{

					if (aesAlg == null) throw new Exception("Unable to create AES object...");

					aesAlg.Key = _key;
					aesAlg.IV = _IV;

					// Create a decryption to perform the stream transform.
					var decryption = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

					// Create the streams used for decryption.
					using (var msDecrypt = new MemoryStream(cipherBytes))
					{
						using (var csDecrypt = new CryptoStream(msDecrypt, decryption, CryptoStreamMode.Read))
						{
							using (var mStream = new MemoryStream())
							{
								int data;
								while ((data = csDecrypt.ReadByte()) != -1)
									mStream.WriteByte((byte)data);

								outputBytes = mStream.ToArray();
							}
						}
					}

				}

				return outputBytes;
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
		/// <param name="outputfile"></param>
		public FileInfo FileEncrypt(string inputFile, string outputfile)
		{

			//generate random salt
			byte[] salt = GenerateRandomSalt();

			//create output file name
			var fsCrypt = new FileStream(outputfile, FileMode.Create);

			//convert password string to byte array
			byte[] passwordBytes = _key;

			//Set Rijndael symmetric encryption algorithm
			RijndaelManaged AES = new RijndaelManaged {KeySize = 256, BlockSize = 128};

			//"What it does is repeatedly hash the user password along with the salt." High iteration counts.
			var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
			AES.Key = key.GetBytes(AES.KeySize / 8);
			AES.IV = key.GetBytes(AES.BlockSize / 8);

			AES.Padding = PaddingMode.PKCS7;
			AES.Mode = CipherMode.CBC;

			// write salt to the begining of the output file, so in this case can be random every time
			fsCrypt.Write(salt, 0, salt.Length);

			var cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

			var fsIn = new FileStream(inputFile, FileMode.Open);

			//create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
			var buffer = new byte[1048576];

			try
			{
				int read;
				while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
				{
					cs.Write(buffer, 0, read);
				}

				// Close up
				fsIn.Close();
			}
			catch (Exception ex)
			{
				throw new Exception("Error:" + ex.Message, ex);
			}
			finally
			{
				cs.Close();
				fsCrypt.Close();
			}

			return new FileInfo(outputfile);
		}

		/// <summary>
		/// Decrypts an encrypted file with the FileEncrypt method through its path and the plain password.
		/// </summary>
		/// <param name="inputFile"></param>
		/// <param name="outputFile"></param>
		public FileInfo FileDecrypt(string inputFile, string outputFile)
		{
			byte[] passwordBytes = _key;
			byte[] salt = new byte[32];

			FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
			fsCrypt.Read(salt, 0, salt.Length);

			var AES = new RijndaelManaged {KeySize = 256, BlockSize = 128};
			var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
			AES.Key = key.GetBytes(AES.KeySize / 8);
			AES.IV = key.GetBytes(AES.BlockSize / 8);
			AES.Padding = PaddingMode.PKCS7;
			AES.Mode = CipherMode.CBC;

			var cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);

			FileInfo output = new FileInfo(outputFile);
			if (output.Extension == Extension)
			{
				outputFile = output.Name;
			}

			var fsOut = new FileStream(outputFile, FileMode.Create);

			var buffer = new byte[1048576];

			try
			{
				int read;
				while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
				{
					fsOut.Write(buffer, 0, read);
				}
			}
			catch (CryptographicException exCryptographicException)
			{
				throw new CryptographicException(exCryptographicException.Message, exCryptographicException);
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

			return new FileInfo(outputFile);
		}

	}


}

