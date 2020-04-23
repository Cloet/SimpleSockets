using System;
using System.IO;
using System.Security.Cryptography;

namespace SimpleSockets.Helpers.Cryptography {

    public static class Aes
    {

        private static byte[] GenerateRandomSalt() {
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider()) {
                for (int i = 0; i < 10; i++) {
                    rng.GetBytes(data);
                }
            }

            return data;
        }

        internal static byte[] EncryptBytes(byte[] bytes, byte[] key) {
               

            if (bytes == null || bytes.Length <= 0)
                throw new ArgumentNullException(nameof(bytes));

            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));

            try  {
                byte[] salt = GenerateRandomSalt();

                RijndaelManaged aes = new RijndaelManaged { KeySize = 256, BlockSize = 128};

                var encryptionKey = new Rfc2898DeriveBytes(key, salt, 5000);

                aes.Key = encryptionKey.GetBytes(aes.KeySize / 8);
                aes.IV = encryptionKey.GetBytes(aes.BlockSize / 8);
                
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                byte[] encrypted;
                using (var memStream = new MemoryStream()) {
                    memStream.Write(salt, 0, salt.Length);
                    using (var cs = new CryptoStream(memStream, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
                        cs.Write(bytes, 0, bytes.Length);
                    }
                    encrypted = memStream.ToArray();
                }

                return encrypted;
            } catch (Exception) {
                return null;
            }

        }

        internal static byte[] DecryptBytes(byte[] encryptedBytes, byte[] key) {

            if (encryptedBytes == null || encryptedBytes.Length <= 0)
                throw new ArgumentNullException(nameof(encryptedBytes));

            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));

            try {

                byte[] salt = new byte[32];
                byte[] bytesWithoutSalt = new byte[encryptedBytes.Length - salt.Length];

                Array.Copy(encryptedBytes, 0, salt, 0, 32);
                Array.Copy(encryptedBytes, 32, bytesWithoutSalt, 0, bytesWithoutSalt.Length);
                encryptedBytes = bytesWithoutSalt;

                var aes = new RijndaelManaged { KeySize = 256, BlockSize = 128};
                var encryptionkey = new Rfc2898DeriveBytes(key, salt, 5000);
                aes.Key = encryptionkey.GetBytes(aes.KeySize / 8);
                aes.IV = encryptionkey.GetBytes(aes.BlockSize / 8);
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                var outMemStream = new MemoryStream();
                using (var memStream = new MemoryStream(encryptedBytes)) {
                    using (var cs = new CryptoStream(memStream, aes.CreateDecryptor(), CryptoStreamMode.Read)){
                        int read;
                        var buffer = new byte[1048576];
                        while ((read = cs.Read(buffer, 0, buffer.Length)) > 0) {
                            outMemStream.Write(buffer,0,read);
                        }
                    }
                }

                return outMemStream.ToArray();
                
            } catch (Exception) {
                return null;
            }

        }

    }

}