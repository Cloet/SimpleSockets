using System;
using AsyncClientServer.Compression;
using AsyncClientServer.Cryptography;
using AsyncClientServer.StateObject;

namespace AsyncClientServer.Client
{
	/// <summary>
	/// Interface for AsyncClient
	/// <para>Implements
	/// <seealso cref="T:System.IDisposable" /></para>
	/// </summary>
	public interface ITcpClient : IDisposable, ISendToServer
	{

		/// <summary>
		/// This event is invoked when the client connects to the server.
		/// </summary>
		event ConnectedHandler Connected;

		/// <summary>
		/// This event is invoked when the client receives a message from the server
		/// </summary>
		event ClientMessageReceivedHandler MessageReceived;

		/// <summary>
		/// This event is invoked when the client has submitted a message to the server
		/// </summary>
		event ClientMessageSubmittedHandler MessageSubmitted;

		/// <summary>
		/// This event is invoked when the client receives a file
		/// </summary>
		event FileFromServerReceivedHandler FileReceived;

		/// <summary>
		/// Event that tracks the progress of a FileTransfer.
		/// </summary>
		event ProgressFileTransferHandler ProgressFileReceived;

		/// <summary>
		/// Event that is used to check if the client is still connected to the server.
		/// </summary>
		event DisconnectedFromServerHandler Disconnected;

		/// <summary>
		/// Event that is triggered when a message fails to send
		/// </summary>
		event DataTransferFailedHandler MessageFailed;

		/// <summary>
		/// Event that is triggered when an error is thrown
		/// </summary>
		event ErrorHandler ErrorThrown;

		/// <summary>
		/// Tries to connect to the server
		/// </summary>
		/// <param name="ipServer">The server ip</param>
		/// <param name="port">The port the server is using</param>
		/// <param name="reconnectInSeconds">Default is 5</param>
		void StartClient(string ipServer, int port, int reconnectInSeconds = 5);

		/// <summary>
		/// The port on which the server is running
		/// </summary>
		int Port { get; }

		/// <summary>
		/// The ip of the server
		/// </summary>
		string IpServer { get; }

		/// <summary>
		/// Used to encrypt files/folders
		/// </summary>
		Encryption MessageEncrypter { get; set; }

		/// <summary>
		/// Used to compress files before sending
		/// </summary>
		FileCompression ClientFileCompressor { get; set; }

		/// <summary>
		/// Used to compress folder before sending
		/// </summary>
		FolderCompression ClientFolderCompressor { get; set; }

		/// <summary>
		/// Tries to reconnect every x seconds
		/// </summary>
		int ReconnectInSeconds { get; }

		/// <summary>
		/// Checks if client is connected to the server
		/// </summary>
		/// <returns></returns>
		bool IsConnected();

		/// <summary>
		/// Changes the BufferSize of the client.
		/// </summary>
		/// <param name="bufferSize"></param>
		void ChangeSocketBufferSize(int bufferSize);
	}
}
