using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using AsyncClientServer.StateObject;
using AsyncClientServer.StateObject.StateObjectState;

namespace AsyncClientServer.Server
{



	/// <summary>
	/// This class is the server, singleton class
	/// <para>Handles sending and receiving data to/from clients</para>
	/// <para>Extends <see cref="SendToClient"/>, Implements <seealso cref="IServerListener"/></para>
	/// </summary>
	public class AsyncSocketListener : ServerListener
	{

		/// <summary>
		/// Get the instance of the server
		/// </summary>
		public static AsyncSocketListener Instance { get; } = new AsyncSocketListener();


		private AsyncSocketListener()
		{
			Init();
		}

		/// <summary>
		/// Start listening on specified port and ip.
		/// <para/>The limit is the maximum amount of client which can connect at one moment.
		/// </summary>
		/// <param name="ip">The ip the server will be listening to.</param>
		/// <param name="port">The port on which the server will be running.</param>
		/// <param name="limit">Optional parameter, default value is 500.</param>
		public override void StartListening(string ip, int port, int limit = 500)
		{
			if (string.IsNullOrEmpty(ip))
				throw new ArgumentNullException(nameof(ip));
			if (port < 1)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (limit < 0)
				throw new ArgumentException("Limit cannot be under 0.");
			if (limit == 0)
				throw new ArgumentException("Limit cannot be 0.");

			Port = port;
			Ip = ip;

			var host = Dns.GetHostEntry(ip);
			var ipServer = host.AddressList[0];
			var endpoint = new IPEndPoint(ipServer, port);

			try
			{
				using (var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
				{
					listener.Bind(endpoint);
					listener.Listen(Limit);

					ServerHasStartedInvoke();
					while (true)
					{
						_mre.Reset();
						listener.BeginAccept(OnClientConnect, listener);
						_mre.WaitOne();
					}
				}
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		protected override void OnClientConnect(IAsyncResult result)
		{
			_mre.Set();
			try
			{
				IStateObject state;

				lock (_clients)
				{
					var id = !_clients.Any() ? 1 : _clients.Keys.Max() + 1;

					state = new StateObject.StateObject(((Socket)result.AsyncState).EndAccept(result), id);
					_clients.Add(id, state);
					ClientConnectedInvoke(id);
				}
				StartReceiving(state);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}

		}
	}
}
