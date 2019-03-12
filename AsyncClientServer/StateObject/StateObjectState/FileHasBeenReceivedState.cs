using System.IO;
using AsyncClientServer.Client;
using AsyncClientServer.Server;
using Compression;
using Cryptography;

namespace AsyncClientServer.StateObject.StateObjectState
{
	public class FileHasBeenReceivedState: StateObjectState
	{
		public FileHasBeenReceivedState(IStateObject state) : base(state)
		{
		}

		public FileHasBeenReceivedState(IStateObject state, IAsyncClient client) : base(state, client)
		{
		}

		/// <summary>
		/// Invokes the file or message that has been received.
		/// </summary>
		/// <param name="receive"></param>
		public override void Receive(int receive)
		{


			//Received file
			FileInfo info = new FileInfo(State.Header);

			//Remove the .aes extension of the new file
			string newFileName = info.FullName.Remove(info.FullName.Length - info.Extension.Length);

			//Decrypts the file and save at new location. Deletes the encrypted file after decrypting.
			AES256.FileDecrypt(State.Header, newFileName);
			File.Delete(State.Header);

			//Decompresses the file using gzip.
			string decompressed = Decompress(newFileName);

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

			if (info.Extension == ".compressedGz")
			{
				FileInfo decompressedFile;
				decompressedFile = GZipCompression.Decompress(info);
				File.Delete(info.FullName);
				return decompressedFile.FullName;
			}
			
			if (info.Extension == ".compressedZip")
			{
				DirectoryInfo extractedFolder = new DirectoryInfo(info.FullName.Remove(info.FullName.Length - info.Extension.Length));
				ZipCompression.Extract(info.FullName, extractedFolder.FullName);
				File.Delete(info.FullName);
				return extractedFolder.FullName;
			}

			return null;

		}

	}
}
