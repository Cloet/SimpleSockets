using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer.Compression
{
	public static class ZipCompression
	{

		public static string Extension => ".CZip";


		/// <summary>
		/// Compresses a folder to .zip file.
		/// </summary>
		/// <param name="sourceDirPath"></param>
		/// <param name="targetZipPath"></param>
		public static void Compress(string sourceDirPath, string targetZipPath)
		{
			try
			{
				if (!Directory.Exists(sourceDirPath))
					throw new ArgumentException("Source directory does not exist.");

				ZipFile.CreateFromDirectory(sourceDirPath, targetZipPath);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <summary>
		/// Extracts a Zip file to a target directory.
		/// </summary>
		/// <param name="sourceZipPath"></param>
		/// <param name="targetDirPath"></param>
		public static void Extract(string sourceZipPath, string targetDirPath)
		{
			try
			{
				//Check if the file exists.
				if (!File.Exists(sourceZipPath))
					throw new ArgumentException("The zip file does not exist.");

				//Extract all entries from the archive.
				using (ZipArchive archive = ZipFile.OpenRead(sourceZipPath))
				{
					foreach (ZipArchiveEntry entry in archive.Entries)
					{
						FileInfo destFile = new FileInfo(Path.GetFullPath(Path.Combine(targetDirPath, entry.FullName)));

						//Make sure subdirectories exist. If not create them.
						if (!Directory.Exists(destFile.DirectoryName) && destFile.DirectoryName != null)
							Directory.CreateDirectory(destFile.DirectoryName);

						//Make sure the destination is a file and not a directory without files in it. Then extract the file.
						if (destFile.Name != string.Empty)
							entry.ExtractToFile(destFile.FullName, true);

					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

	}
}
