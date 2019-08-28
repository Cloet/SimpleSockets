namespace SimpleSockets.Messaging.Compression.Stream
{
	public interface IByteCompression
	{
		/// <summary>
		/// Compress bytes
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		byte[] CompressBytes(byte[] data);

		/// <summary>
		/// Decompress compressed bytes.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		byte[] DecompressBytes(byte[] data);

	}
}
