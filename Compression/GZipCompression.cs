using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compression
{
	public static class GZipCompression
	{


		/// <summary>
		/// Compresses all files in a directory
		/// </summary>
		/// <param name="directoryToCompress"></param>
		/// <param name="directoryTargetPath"></param>
		public static void Compress(DirectoryInfo directoryToCompress, string directoryTargetPath)
		{
			try
			{
				foreach (FileInfo fileToCompress in directoryToCompress.GetFiles())
				{
					Compress(fileToCompress, directoryTargetPath);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <summary>
		/// Compresses a single file
		/// </summary>
		/// <param name="fileToCompress"></param>
		/// <param name="directoryTargetPath"></param>
		public static void Compress(FileInfo fileToCompress, string directoryTargetPath)
		{
			try
			{
				using (FileStream originalFileStream = fileToCompress.OpenRead())
				{
					if ((File.GetAttributes(fileToCompress.FullName) &
						 FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
					{
						using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
						{
							using (GZipStream compressionStream =
								new GZipStream(compressedFileStream, CompressionMode.Compress))
							{
								originalFileStream.CopyTo(compressionStream);
							}
						}

						FileInfo info = new FileInfo(directoryTargetPath + Path.DirectorySeparatorChar +
													 fileToCompress.Name + ".gz");
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <summary>
		/// Compress a file and return the FileInfo of the new file.
		/// </summary>
		/// <param name="fileToCompress"></param>
		/// <returns></returns>
		public static FileInfo Compress(FileInfo fileToCompress)
		{
			try
			{
				if (fileToCompress.Extension == ".gz")
					throw new ArgumentException(fileToCompress.Name + " is already compressed.");

				FileInfo compressedFile = new FileInfo(Path.GetTempPath() + Path.DirectorySeparatorChar +
				                                       fileToCompress.Name + ".gz");

				using (FileStream originalFileStream = fileToCompress.OpenRead())
				{
					if ((File.GetAttributes(fileToCompress.FullName) &
					     FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
					{
						using (FileStream compressedFileStream = File.Create(compressedFile.FullName))
						{
							using (GZipStream compressionStream =
								new GZipStream(compressedFileStream, CompressionLevel.Fastest))
							{
								originalFileStream.CopyTo(compressionStream);
							}
						}
						return compressedFile;
					}

					throw new ArgumentException(fileToCompress.Name + " is not a valid file to compress.");
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <summary>
		/// Decompresses all ".gz" files in a folder.
		/// </summary>
		/// <param name="directoryToCompress"></param>
		public static void Decompress(DirectoryInfo directoryToCompress)
		{
			try
			{
				foreach (FileInfo fileToDecompress in directoryToCompress.GetFiles())
				{
					Decompress(fileToDecompress);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		/// <summary>
		/// Decompresses a file
		/// </summary>
		/// <param name="fileToDecompress"></param>
		public static FileInfo Decompress(FileInfo fileToDecompress)
		{
			try
			{
				using (FileStream originalFileStream = fileToDecompress.OpenRead())
				{
					if ((File.GetAttributes(fileToDecompress.FullName) &
						 FileAttributes.Hidden) != FileAttributes.Hidden & fileToDecompress.Extension == ".gz")
					{

						string currentFileName = fileToDecompress.FullName;
						string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

						using (FileStream decompressedFileStream = File.Create(newFileName))
						{
							using (GZipStream decompressionStream =
								new GZipStream(originalFileStream, CompressionMode.Decompress))
							{
								decompressionStream.CopyTo(decompressedFileStream);
							}

							return new FileInfo(newFileName);
						}
					}

					throw new Exception("The file cannot be decompressed.");

				}

			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}


		}



	}
}
