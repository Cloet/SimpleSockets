using System;
using System.IO;

namespace SimpleSockets.Helpers.Compression
{

	internal static class CompressionHelper
	{

		#region Stream Compression

		/// <summary>
		/// Compresses an input stream of bytes
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		internal static byte[] Compress(byte[] bytes, CompressionMethod mode)
		{
			if (bytes == null || bytes.Length <= 0)
				return null;

			if (mode == CompressionMethod.GZip)
				return Gzip.Compress(bytes);
			if (mode == CompressionMethod.Deflate)
				return Deflate.CompressBytes(bytes);
			if (mode == CompressionMethod.None)
				return bytes;

			throw new ArgumentException(nameof(mode));
		}

		/// <summary>
		/// Decompresses an input stream of bytes
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		internal static byte[] Decompress(byte[] bytes, CompressionMethod mode)
		{
			if (bytes == null || bytes.Length <= 0)
				return null;

			if (mode == CompressionMethod.GZip)
				return Gzip.Decompress(bytes);
			if (mode == CompressionMethod.Deflate)
				return Deflate.DecompressBytes(bytes);
			if (mode == CompressionMethod.None)
				return bytes;

			throw new ArgumentException(nameof(mode));
		}

		#endregion

		#region File/Folder Compression

		/// <summary>
		/// Returns the compressed zip file.
		/// </summary>
		/// <param name="sourceDirPath"></param>
		/// <param name="targetZipPath"></param>
		/// <returns></returns>
		internal static FileInfo CompressToZip(string sourceDirPath, string targetZipPath) {
			return Zip.Compress(sourceDirPath, targetZipPath);
		}

		/// <summary>
		/// Returns the location of the new directory
		/// </summary>
		/// <param name="sourceZipPath"></param>
		/// <param name="targetDirPath"></param>
		/// <returns></returns>
		internal static string ExtractZip(string sourceZipPath, string targetDirPath) {
			return Zip.Extract(sourceZipPath, targetDirPath);
		}

		#endregion

	}

}