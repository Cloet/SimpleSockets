using System;
using System.Collections.Generic;
using AsyncClientServer.Model;

namespace AsyncClientServer.Helper
{
	/// <summary>
	/// Interface for AsyncSocketListener
	/// <para>Implements <seealso cref="IDisposable"/></para>
	/// </summary>
	public interface IAsyncSocketListener : IDisposable
	{
		event MessageReceivedHandler MessageReceived;

		event MessageSubmittedHandler MessageSubmitted;

		event ClientDisconnectedHandler ClientDisconnected;

		event FileFromClientReceivedHandler FileReceived;

		event ServerHasStartedHandler ServerHasStarted;

		int Port { get; }

		void StartListening(int port);

		bool IsConnected(int id);

		void OnClientConnect(IAsyncResult result);

		void ReceiveCallback(IAsyncResult result);

		IDictionary<int, IStateObject> GetClients();

		void Close(int id);

		/// <summary>
		/// Send a message to corresponding client.
		/// <para>Id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		/// <param name="close"></param>
		void SendMessage(int id, string message, Boolean close);

		/// <summary>
		/// Sends an object to corresponding client.
		/// <para>The id is not zero-based!</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="anyObj"></param>
		/// <param name="close"></param>
		void SendObject(int id, SerializableObject anyObj, bool close);

		void SendFile(int id, string fileLocation, string remoteSaveLocation, Boolean close);



		/// <summary>
		/// Sends a file to all clients
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <param name="remoteSaveLocation"></param>
		/// <param name="close"></param>
		void SendFileToAllClients(string fileLocation, string remoteSaveLocation, Boolean close);

		/// <summary>
		/// Sends a Message to all clients
		/// </summary>
		/// <param name="message"></param>
		/// <param name="close"></param>
		void SendMessageToAllClients(string message, Boolean close);

		/// <summary>
		/// Sends an object to all clients
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="close"></param>
		void SendObjectToAllClients(SerializableObject obj, Boolean close);

	}
}
