using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer.Compression
{
	public class GZipCompression: FileCompression
	{

		/// <summary>
		/// Compress a file and return the FileInfo of the new file.
		/// </summary>
		/// <param name="fileToCompress"></param>
		/// <returns></returns>
		public override FileInfo Compress(FileInfo fileToCompress)
		{
			try
			{
				if (fileToCompress.Extension == Extension)
					throw new ArgumentException(fileToCompress.Name + " is already compressed.");

				FileInfo compressedFile = new FileInfo(Path.GetTempPath() + Path.DirectorySeparatorChar +
				                                       fileToCompress.Name + Extension);

				using (FileStream originalFileStream = fileToCompress.OpenRead())
				{
					if ((File.GetAttributes(fileToCompress.FullName) &
					     FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != Extension)
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
		/// Decompresses a file
		/// </summary>
		/// <param name="fileToDecompress"></param>
		public override FileInfo Decompress(FileInfo fileToDecompress)
		{
			try
			{
				using (FileStream originalFileStream = fileToDecompress.OpenRead())
				{
					if ((File.GetAttributes(fileToDecompress.FullName) &
						 FileAttributes.Hidden) != FileAttributes.Hidden)
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
