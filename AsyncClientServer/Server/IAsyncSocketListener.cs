using System;
using System.Collections.Generic;
using AsyncClientServer.StateObject;

namespace AsyncClientServer.Server
{

	/// <summary>
	/// Interface for AsyncSocketListener
	/// <para>Implements <seealso cref="T:System.IDisposable" /></para>
	/// </summary>
	public interface IAsyncSocketListener : IDisposable, ISendToClient
	{
		/// <summary>
		/// An event that is triggered when a message is received.
		/// </summary>
		event MessageReceivedHandler MessageReceived;

		/// <summary>
		/// Event that is triggered when a message has been send.
		/// </summary>
		event MessageSubmittedHandler MessageSubmitted;

		/// <summary>
		/// Event that is triggered when a client has disconnected from the server.
		/// </summary>
		event ClientDisconnectedHandler ClientDisconnected;

		/// <summary>
		/// Event that is triggered when a file has been received from a client.
		/// </summary>
		event FileFromClientReceivedHandler FileReceived;

		/// <summary>
		/// Event that is triggered when the server has started.
		/// </summary>
		event ServerHasStartedHandler ServerHasStarted;

		/// <summary>
		/// The port the server is running on
		/// </summary>
		int Port { get; }

		/// <summary>
		/// Starts the server on a certain port
		/// </summary>
		/// <param name="port"></param>
		void StartListening(int port);

		/// <summary>
		/// Checks if a client is connected
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		bool IsConnected(int id);

		/// <summary>
		/// Triggered in when a client connects to the server
		/// </summary>
		/// <param name="result"></param>
		void OnClientConnect(IAsyncResult result);

		/// <summary>
		/// Gets all connected clients
		/// </summary>
		/// <returns></returns>
		IDictionary<int, IStateObject> GetClients();

		/// <summary>
		/// Check a single client if he's still active
		/// </summary>
		/// <param name="id"></param>
		void CheckClient(int id);

		/// <summary>
		/// Checks all clients if they are still active and removes them if they are deactive.
		/// </summary>
		void CheckAllClients();

		/// <summary>
		/// Closes a certain client
		/// </summary>
		/// <param name="id"></param>
		void Close(int id);

		/// <summary>
		/// Invokes FileReceived event of the server
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="filePath">the path of the file that has been received</param>
		void InvokeFileReceived(int id, string filePath);

		/// <summary>
		/// Invokes MessageReceived event of the server
		/// </summary>
		/// <param name="id">Client id</param>
		/// <param name="header">Message type</param>
		/// <param name="text">the message</param>
		void InvokeMessageReceived(int id, string header, string text);

	}
}
