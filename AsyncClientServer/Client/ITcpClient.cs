using System;

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
		/// Tries to connect to the server
		/// <para>Will try to reconnect every 5 seconds (default value)</para>
		/// </summary>
		/// <param name="ipServer">Ip of the server</param>
		/// <param name="port">Port of the server</param>
		void StartClient(string ipServer, int port);

		/// <summary>
		/// Tries to connect to the server
		/// </summary>
		/// <param name="ipServer">The server ip</param>
		/// <param name="port">The port the server is using</param>
		/// <param name="reconnectInSeconds">Default is 5</param>
		void StartClient(string ipServer, int port, int reconnectInSeconds);

		/// <summary>
		/// The port on which the server is running
		/// </summary>
		int Port { get; }

		/// <summary>
		/// The ip of the server
		/// </summary>
		string IpServer { get; }

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
		/// Invokes a MessageReceived Event.
		/// </summary>
		/// <param name="header">Type of message that has been received</param>
		/// <param name="text">The message itself</param>
		void InvokeMessage(string header, string text);

		/// <summary>
		/// Invokes a FileReceived Event
		/// </summary>
		/// <param name="filePath">Location where the file is stored.</param>
		void InvokeFileReceived(string filePath);

		/// <summary>
		/// Invokes ProgressReceived event
		/// </summary>
		/// <param name="bytesReceived"></param>
		/// <param name="messageSize"></param>
		void InvokeFileTransferProgress(int bytesReceived, int messageSize);

	}
}
