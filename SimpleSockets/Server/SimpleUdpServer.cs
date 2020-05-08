using SimpleSockets.Helpers;
using SimpleSockets.Messaging;
using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSockets.Server {

	public class SimpleUdpServer : SimpleServer
	{
	    private EndPoint _epFrom = new IPEndPoint(IPAddress.Any, 0);

		/// <summary>
		/// Constructor
		/// </summary>
		public SimpleUdpServer() : base(SocketProtocolType.Udp) {
		}

		/// <summary>
		/// Listen for messages on ip, port.
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		public override void Listen(string ip, int port)
		{
			if (port < 1 || port > 65535)
				throw new ArgumentOutOfRangeException(nameof(port), port, "The port number must be between 1-65535");

			ListenerPort = port;
			ListenerIp = ip;

			var endpoint = new IPEndPoint(ListenerIPFromString(ip), port);

			TokenSource = new CancellationTokenSource();
			Token = TokenSource.Token;

			Task.Run(() => {
				try
				{
					Listener = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
					Listener.Bind(endpoint);
					Listening = true;

					OnServerStartedListening();
					Task.Run( () => Receive(), Token);
				}
				catch (ObjectDisposedException ode)
				{
					SocketLogger?.Log(ode, LogLevel.Fatal);
				}
			}, Token);
		}

		// Receives messages
		protected virtual void Receive()
		{
			try
			{
				var client = new SessionMetadata(Listener, -1, SocketLogger);
				while (!Token.IsCancellationRequested)
				{

					client.ReceivingData.Wait(Token);
					client.Timeout.Reset();
					client.ReceivingData.Reset();

					Listener.BeginReceiveFrom(client.DataReceiver.Buffer, 0, client.DataReceiver.Buffer.Length, SocketFlags.None, ref _epFrom, ReceiveCallback, client);
				}
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error receiving data from client", ex, LogLevel.Error);
			}
		}

		// Called by Receive
		protected virtual void ReceiveCallback(IAsyncResult result)
		{
			var client = (ISessionMetadata)result.AsyncState;
			try
			{
				var received = client.Listener.EndReceiveFrom(result, ref _epFrom);

				Statistics?.AddReceivedBytes(received);

				// Add byte per byte to datareceiver,
				// This way we can use a delimiter to check if a message has been received.
				if (received > 0)
				{
					SocketLogger?.Log($"Received {received} bytes from a client. {client.Info()}", LogLevel.Trace);
					var readBuffer = new byte[received];
					Array.Copy(client.DataReceiver.Buffer, 0, readBuffer, 0, readBuffer.Length);
					ByteDecoder(client, readBuffer);
				}

				// Resets buffer of the datareceiver.
				client.DataReceiver.ClearBuffer();
				client.ReceivingData.Set();
			}
			catch (Exception ex)
			{
				client.ReceivingData.Set();
				SocketLogger?.Log("Error receiving a message.", ex, LogLevel.Error);
				SocketLogger?.Log("Trying to restart the datareceiver for client " + client.Id, LogLevel.Debug);
			}
		}

		// Send bytes to a client.
		protected override void SendToSocket(int clientId, byte[] payload)
		{
			ISessionMetadata client = null;
			ConnectedClients?.TryGetValue(clientId, out client);
			try
			{

				if (client != null)
				{
					client.WritingData.Wait();
					client.WritingData.Reset();

					client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, client);
				}

			}
			catch (Exception ex)
			{
				if (client != null)
					client.WritingData.Set();
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
			}
		}

		// Called by SendToSocket
		protected void SendCallback(IAsyncResult result)
		{
			var client = (ISessionMetadata)result.AsyncState;

			try
			{
				Statistics?.AddSentMessages(1);

				var count = client.Listener.EndSend(result);
				Statistics?.AddSentBytes(count);
				SocketLogger?.Log($"Sent {count} bytes to a client. {client.Info()}", LogLevel.Trace);

				client.WritingData.Set();
			}
			catch (Exception ex)
			{
				client.WritingData.Set();
				SocketLogger?.Log("An error occurred when sending a message to client " + client.Id, ex, LogLevel.Error);
			}
		}

		// Send bytes to a client.
		protected override async Task<bool> SendToSocketAsync(int clientId, byte[] payload)
		{
			ISessionMetadata client = null;
			ConnectedClients?.TryGetValue(clientId, out client);
			try
			{
				if (client != null)
				{

					client.WritingData.Wait();
					client.WritingData.Reset();
					Statistics?.AddSentMessages(1);

					var result = client.Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, _ => { }, client);
					var count = await Task.Factory.FromAsync(result, (r) => client.Listener.EndSend(r));
					Statistics?.AddSentBytes(count);
					SocketLogger?.Log($"Sent {count} bytes to a client. {client.Info()}", LogLevel.Trace);

					client.WritingData.Set();

					return true;
				}

				return false;
			}
			catch (Exception ex)
			{
				if (client != null)
					client.WritingData.Set();
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
				return false;
			}
		}

		// Disposes of the server.
		public override void Dispose()
		{
			base.Dispose();
		}

	}

}