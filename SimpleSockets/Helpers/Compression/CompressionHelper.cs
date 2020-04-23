using System;

namespace SimpleSockets.Helpers.Compression{

    internal static class CompressionHelper {

        internal static byte[] Compress(byte[] bytes, CompressionType mode) {
            if (bytes == null || bytes.Length <= 0)
                return null;

            if (mode == CompressionType.GZip)
                return Gzip.Compress(bytes);
            if (mode == CompressionType.Deflate)
                return Deflate.CompressBytes(bytes);

            throw new ArgumentException(nameof(mode));
        }

        internal static byte[] Decompress(byte[] bytes, CompressionType mode) {
            if (bytes == null || bytes.Length <= 0)
                return null;

            if (mode == CompressionType.GZip)
                return Gzip.Decompress(bytes);
            if (mode == CompressionType.Deflate)
                return Deflate.DecompressBytes(bytes);

            throw new ArgumentException(nameof(mode));
        }

    }

}