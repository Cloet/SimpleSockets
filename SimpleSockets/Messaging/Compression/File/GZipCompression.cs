using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleSockets.Messaging.Compression.File
{
	internal class GZipCompression: FileCompression
	{

		/// <summary>
		/// Compress a file and return the FileInfo of the new file.
		/// </summary>
		/// <param name="fileToCompress"></param>
		/// <param name="outputFile"></param>
		/// <returns></returns>
		public override FileInfo Compress(FileInfo fileToCompress, FileInfo outputFile)
		{
			try
			{
				if (fileToCompress.Extension == Extension)
					throw new ArgumentException(fileToCompress.Name + " is already compressed.");

				//var compressedFile = new FileInfo(outputPath + Path.DirectorySeparatorChar + fileToCompress.Name + Extension);

				using (var originalFileStream = fileToCompress.OpenRead())
				{
					if ((System.IO.File.GetAttributes(fileToCompress.FullName) &FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != Extension)
					{
						using (var compressedFileStream = outputFile.Create())
						{
							using (var compressionStream = new System.IO.Compression.GZipStream(compressedFileStream, System.IO.Compression.CompressionLevel.Optimal))
							{
								originalFileStream.CopyTo(compressionStream);
							}
						}
						return outputFile;
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
		/// <param name="outputPath"></param>
		public override FileInfo Decompress(FileInfo fileToDecompress, FileInfo outputfile)
		{
			try
			{
				using (var originalFileStream = fileToDecompress.OpenRead())
				{
					if ((System.IO.File.GetAttributes(fileToDecompress.FullName) &FileAttributes.Hidden) != FileAttributes.Hidden)
					{
						if (string.IsNullOrEmpty(outputfile.FullName) || outputfile.Exists)
							throw new Exception("File already exists.");

						using (var decompressedFileStream = outputfile?.Create())
						{
							using (var decompressionStream =new System.IO.Compression.GZipStream(originalFileStream, System.IO.Compression.CompressionMode.Decompress))
							{
								decompressionStream.CopyTo(decompressedFileStream);
							}

							return outputfile;
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
