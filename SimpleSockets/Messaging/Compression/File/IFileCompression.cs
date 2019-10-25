using System.IO;

namespace SimpleSockets.Messaging.Compression.File
{
	public interface IFileCompression
	{
		/// <summary>
		/// Extension of the compressed file
		/// </summary>
		string Extension { get; }

		/// <summary>
		/// Compress a file and return the FileInfo of the new file.
		/// <para>Has to return the FileInfo of the compressed file in order to work.</para>
		/// </summary>
		/// <param name="fileToCompress"></param>
		/// <param name="outputFile"></param>
		/// <returns></returns>
		FileInfo Compress(FileInfo fileToCompress, FileInfo outputFile);


		/// <summary>
		/// Decompresses a file
		/// <para>Will have to return the FileInfo object of the decompressed file in order to work.</para>
		/// </summary>
		/// <param name="fileToDecompress"></param>
		/// <param name="outputFile"></param>
		FileInfo Decompress(FileInfo fileToDecompress, FileInfo outputFile);

	}
}
