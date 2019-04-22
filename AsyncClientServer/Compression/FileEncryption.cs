using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer.Compression
{
	public abstract class FileEncryption
	{

		public string Extension => ".FilePART";

		/// <summary>
		/// Compress a file and return the FileInfo of the new file.
		/// </summary>
		/// <param name="fileToCompress"></param>
		/// <returns></returns>
		public abstract FileInfo Compress(FileInfo fileToCompress);


		/// <summary>
		/// Decompresses a file
		/// </summary>
		/// <param name="fileToDecompress"></param>
		public abstract FileInfo Decompress(FileInfo fileToDecompress);


	}
}
