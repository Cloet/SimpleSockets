namespace SimpleSockets.Messaging.Compression.Stream
{
	public abstract class ByteCompression
	{

		public abstract byte[] CompressBytes(byte[] data);

		public abstract byte[] DecompressBytes(byte[] data);

	}
}
