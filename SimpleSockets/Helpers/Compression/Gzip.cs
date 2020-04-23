using System.IO;
using System.IO.Compression;

namespace SimpleSockets.Helpers.Compression {

    internal static class Gzip {
        internal static byte[] Compress(byte[] bytes) {
            var outputStream = new MemoryStream();
            using (var memStream = new MemoryStream(bytes)) {
                using(var cStream = new GZipStream(outputStream, CompressionLevel.Optimal)) {
                    memStream.CopyTo(cStream);
                }
            }
            return outputStream.ToArray();
        }

        internal static byte[] Decompress(byte[] bytes) {
            var outputStream = new MemoryStream();
            using (var memStream = new MemoryStream(bytes)) {
                using (var dStream = new GZipStream(memStream, CompressionMode.Decompress)) {
                    dStream.CopyTo(outputStream);
                }
            }
            return outputStream.ToArray();
        }

    }

}