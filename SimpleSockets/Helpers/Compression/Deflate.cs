using System.IO;
using System.IO.Compression;

namespace SimpleSockets.Helpers.Compression
{
	internal static class Deflate
	{

		internal static byte[] CompressBytes(byte[] data)
		{
			var output = new MemoryStream();
			using (var stream = new DeflateStream(output, CompressionLevel.Optimal))
			{
				stream.Write(data, 0, data.Length);
			}
			return output.ToArray();
		}

		internal static byte[] DecompressBytes(byte[] data)
		{
			var input = new MemoryStream(data);
			var output = new MemoryStream();
			using (var stream = new DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
			{
				stream.CopyTo(output);
			}
			return output.ToArray();
		}

	}
}
