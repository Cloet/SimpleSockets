namespace SimpleSockets.Helpers.Cryptography {

    internal static class CryptographyHelper {

        internal static byte[] Encrypt(byte[] bytes, byte[] key, EncryptionType mode) {
            if (bytes == null || bytes.Length <= 0)
                return null;
            if (key== null || key.Length <= 0)
                return null;
            return Aes.EncryptBytes(bytes,key);
        }

        internal static byte[] Decrypt(byte[] bytes, byte[] key, EncryptionType mode) {
            if (bytes == null || bytes.Length <= 0)
                return null;
            if (key== null || key.Length <= 0)
                return null;
            return Aes.DecryptBytes(bytes, key);
        }

    }

}