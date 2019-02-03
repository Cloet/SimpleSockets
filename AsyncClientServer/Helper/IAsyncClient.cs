using System;
using AsyncClientServer.Model;

namespace AsyncClientServer.Helper
{
	/// <summary>
	/// Interface for AsyncClient
	/// <para>Implements
	/// <seealso cref="IDisposable"/></para>
	/// </summary>
	public interface IAsyncClient : IDisposable
	{
		event ConnectedHandler Connected;

		event ClientMessageReceivedHandler MessageReceived;

		event ClientMessageSubmittedHandler MessageSubmitted;

		event FileFromServerReceivedHandler FileReceived;

		void StartClient(string ipServer, int port);

		void StartClient(string ipServer, int port, int reconnectInSeconds);

		int Port { get; }

		string IpServer { get; }

		int ReconnectInSeconds { get; }

		bool IsConnected();

		void Receive();


		/// <summary>
		/// Send a message to the server
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		void SendMessage(string message, bool close);

		/// <summary>
		/// Send an object to server
		/// <para>This object will be serialized using xml</para>
		/// <para>If you want to send your own objects use "SerializableObject" wrapper</para>
		/// </summary>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		void SendObject(SerializableObject anyObj, bool close);

		/// <summary>
		/// Send a file to server
		/// <para>Simple way of sending large files over sockets</para>
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteFileLocation"></param>
		/// <param name="close"></param>
		void SendFile(string fileLocation, string remoteFileLocation, bool close);



	}
}
