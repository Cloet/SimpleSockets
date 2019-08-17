using System.Collections.Generic;
using System.IO;

namespace SimpleSockets.Messaging.Compression.File
{
	public abstract class FileCompression
	{

		public string Extension => ".GZip";

		/// <summary>
		/// Compress a file and return the FileInfo of the new file.
		/// <para>Has to return the FileInfo of the compressed file in order to work.</para>
		/// </summary>
		/// <param name="fileToCompress"></param>
		/// <param name="outputFile"></param>
		/// <returns></returns>
		public abstract FileInfo Compress(FileInfo fileToCompress, FileInfo outputFile);


		/// <summary>
		/// Decompresses a file
		/// <para>Will have to return the FileInfo object of the decompressed file in order to work.</para>
		/// </summary>
		/// <param name="fileToDecompress"></param>
		/// <param name="outputFile"></param>
		public abstract FileInfo Decompress(FileInfo fileToDecompress, FileInfo outputFile);

	}
}
