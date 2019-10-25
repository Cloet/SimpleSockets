using System.IO;

namespace SimpleSockets.Messaging.Compression.Folder
{
	public interface IFolderCompression
	{
		/// <summary>
		/// Extension of the compressed Folder
		/// </summary>
		string Extension { get; }

		/// <summary>
		/// Compresses a folder to .zip file.
		/// <para>The targetZipPath has to be the path where the file will be saved</para>
		/// </summary>
		/// <param name="sourceDirPath"></param>
		/// <param name="targetZipPath"></param>
		FileInfo Compress(string sourceDirPath, string targetZipPath);

		/// <summary>
		/// Extracts a Zip file to a target directory.
		/// <para>TargetDirPath has to be the path where the folder will be extracted to.</para>
		/// </summary>
		/// <param name="sourceZipPath"></param>
		/// <param name="targetDirPath"></param>
		string Extract(string sourceZipPath, string targetDirPath);
		
	}
}
