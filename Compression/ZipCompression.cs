using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Compression
{
	public static class ZipCompression
	{

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
				throw new Exception(ex.Message,ex);
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
				if (!Directory.Exists(targetDirPath))
					Directory.CreateDirectory(targetDirPath);

				if (!File.Exists(sourceZipPath))
					throw new ArgumentException("The zip file does not exist.");

				ZipFile.ExtractToDirectory(sourceZipPath, targetDirPath);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

	}
}
