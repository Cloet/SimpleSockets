using System;
using System.IO;
using AsyncClientServer.Client;
using AsyncClientServer.Server;
using Compression;
using Cryptography;

namespace AsyncClientServer.StateObject.StateObjectState
{
	public class FileHasBeenReceivedState: StateObjectState
	{
		public FileHasBeenReceivedState(IStateObject state,string tempFilePath) : base(state)
		{
			_tempFilePath = tempFilePath;
		}

		public FileHasBeenReceivedState(IStateObject state, IAsyncClient client,string tempFilePath) : base(state, client)
		{
			_tempFilePath = tempFilePath;
		}

		private readonly string _tempFilePath;


		private void Move(string source, string destPath)
		{

			if (File.Exists(source))
			{
				if (Path.GetPathRoot(source) == Path.GetPathRoot(destPath))
				{
					if (File.Exists(destPath))
						File.Delete(destPath);

					File.Move(source, destPath);
				}
				else
				{
					File.Copy(source, destPath, true);
					File.Delete(source);
				}
			}

			if (Directory.Exists(source))
			{
				if (Path.GetPathRoot(source) == Path.GetPathRoot(destPath))
				{
					if (!Directory.Exists(destPath))
						Directory.CreateDirectory(destPath);

					Directory.Move(source, destPath);
				}
				else
				{
					if (!Directory.Exists(destPath))
						Directory.CreateDirectory(destPath);

					//Now Create all of the directories
					foreach (string dirPath in Directory.GetDirectories(source, "*",SearchOption.AllDirectories))
						Directory.CreateDirectory(dirPath.Replace(source, destPath));

					//Copy all the files & Replaces any files with the same name
					foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
						File.Copy(newPath, newPath.Replace(source, destPath), true);

					Directory.Delete(source, true);

				}
			}

		}

		/// <summary>
		/// Invokes the file or message that has been received.
		/// </summary>
		/// <param name="receive"></param>
		public override void Receive(int receive)
		{


			//Received file
			FileInfo info = new FileInfo(_tempFilePath);
			FileInfo targetPath = new FileInfo(State.Header);

			//Gets the path without name
			string tPath = targetPath.FullName.Remove(targetPath.FullName.Length - targetPath.Name.Length);
			string newFileName = info.FullName;

			//If the file is encrypted.
			if (State.Encrypted)
			{
				//Remove the .aes extension of the new file
				newFileName = info.FullName.Remove(info.FullName.Length - info.Extension.Length);

				//Decrypts the file and save at new location. Deletes the encrypted file after decrypting.
				AES256.FileDecrypt(_tempFilePath, newFileName);
				File.Delete(_tempFilePath);
			}

			//Decompresses the file using gzip.
			string targetName = Decompress(newFileName);
			string decompressed = tPath + targetName;
			newFileName = info.FullName.Remove(info.FullName.Length - info.Name.Length) + targetName;

			Move(newFileName, decompressed);

			//If client == null then the file is send to the server so invoke server event else do client event.
			if (Client == null)
			{
				AsyncSocketListener.Instance.InvokeFileReceived(State.Id, decompressed);
				return;
			}

			Client.InvokeFileReceived(decompressed);
		}


		/// <summary>
		/// Decompress the received file or folder
		/// <para>Returns the path of the folder or file.</para>
		/// </summary>
		/// <param name="path"></param>
		public string Decompress(string path)
		{
			FileInfo info = new FileInfo(path);

			if (info.Extension == ".CGz")
			{
				FileInfo decompressedFile = GZipCompression.Decompress(info);
				File.Delete(info.FullName);
				return decompressedFile.Name;
			}
			
			if (info.Extension == ".CZip")
			{
				DirectoryInfo extractedFolder = new DirectoryInfo(info.FullName.Remove(info.FullName.Length - info.Extension.Length));
				ZipCompression.Extract(info.FullName, extractedFolder.FullName);
				File.Delete(info.FullName);
				return extractedFolder.Name;
			}

			return info.Name;

		}

	}
}
