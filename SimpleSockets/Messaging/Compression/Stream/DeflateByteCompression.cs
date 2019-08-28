using System.IO;
using System.IO.Compression;

namespace SimpleSockets.Messaging.Compression.Stream
{
	public class DeflateByteCompression: IByteCompression
	{

		public byte[] CompressBytes(byte[] data)
		{
			var output = new MemoryStream();
			using (var stream = new DeflateStream(output, CompressionLevel.Optimal))
			{
				stream.Write(data, 0, data.Length);
			}
			return output.ToArray();
		}

		public byte[] DecompressBytes(byte[] data)
		{
			var input = new MemoryStream(data);
			var output = new MemoryStream();
			using (var stream = new DeflateStream(input, CompressionMode.Decompress))
			{
				stream.CopyTo(output);
			}
			return output.ToArray();
		}

	}
}
