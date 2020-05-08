using System;

namespace SimpleSockets.Helpers.Cryptography {

    internal static class CryptographyHelper {

        internal static byte[] Encrypt(byte[] bytes, byte[] key, EncryptionMethod mode) {
			if (bytes == null || bytes.Length <= 0)
                return null;
            if (key== null || key.Length <= 0)
                return null;

			if (mode == EncryptionMethod.None)
				return bytes;
			else if (mode == EncryptionMethod.Aes)
				return Aes.EncryptBytes(bytes, key);
			else
				throw new InvalidOperationException("Invalid encryptionmethod.");
        }

        internal static byte[] Decrypt(byte[] bytes, byte[] key, EncryptionMethod mode) {
            if (bytes == null || bytes.Length <= 0)
                return null;
            if (key== null || key.Length <= 0)
                return null;

			if (mode == EncryptionMethod.None)
				return bytes;
			else if (mode == EncryptionMethod.Aes)
				return Aes.DecryptBytes(bytes, key);
			else
				throw new InvalidOperationException("Invalid encryptionmethod.");
        }

    }

}