using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer.Compression
{
	public abstract class FolderCompression
	{

		public string Extension => ".FolderPART";

		/// <summary>
		/// Compresses a folder to .zip file.
		/// </summary>
		/// <param name="sourceDirPath"></param>
		/// <param name="targetZipPath"></param>
		public abstract void Compress(string sourceDirPath, string targetZipPath);

		/// <summary>
		/// Extracts a Zip file to a target directory.
		/// </summary>
		/// <param name="sourceZipPath"></param>
		/// <param name="targetDirPath"></param>
		public abstract void Extract(string sourceZipPath, string targetDirPath);

	}
}
