using SimpleSockets.Helpers;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;
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

		protected TimeSpan MessageResponseWaitTime = new TimeSpan(0,0,5);
        protected int MessageAttempts = 15;

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

					client.ReceivingData.WaitOne();
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
				client.UDPEndPoint = _epFrom;	

				if (!IsConnectionAllowed(client)) {
					SocketLogger?.Log($"Connection from client not allowed. {client.Info()}",LogLevel.Warning);
					client.DataReceiver.ClearBuffer();
					client.ReceivingData.Set();
					return;
				}

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
		protected override bool SendToSocket(int clientId, byte[] payload)
		{
			ISessionMetadata client = null;
			ConnectedClients?.TryGetValue(clientId, out client);
			try
			{

				if (client != null)
				{
					client.WritingData.WaitOne();
					client.WritingData.Reset();

					client.Listener.BeginSendTo(payload, 0, payload.Length, SocketFlags.None, client.UDPEndPoint, SendCallback, client);

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

		// Called by SendToSocket
		protected void SendCallback(IAsyncResult result)
		{
			var client = (ISessionMetadata)result.AsyncState;

			try
			{
				Statistics?.AddSentMessages(1);

				var count = client.Listener.EndSendTo(result);
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

					client.WritingData.WaitOne();
					client.WritingData.Reset();
					Statistics?.AddSentMessages(1);

					var result = client.Listener.BeginSendTo(payload, 0, payload.Length, SocketFlags.None, client.UDPEndPoint, _ => { }, client);
					var count = await Task.Factory.FromAsync(result, (r) => client.Listener.EndSendTo(r));
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

		private Response RequestUdpMessage(int clientId, int responseTimeInMs, string data) {
            try {
				var req = Request.UdpMessageRequest(data, responseTimeInMs);
	            SendPacket(clientId, req.BuildRequestToPacket());
            	return GetResponse(req.RequestGuid, req.Expiration);
			} catch (TimeoutException ex) {
				SocketLogger?.Log($"Failed to get a response from UDP message @ client {clientId}",ex, LogLevel.Trace);
				return null;
			}
		}

        private async Task<Response> RequestUdpMessageAsync(int clientId, int responseTimeInMs, string data) {
            try {
				var req = Request.UdpMessageRequest(data, responseTimeInMs);
	            await SendPacketAsync(clientId, req.BuildRequestToPacket());
            	return GetResponse(req.RequestGuid, req.Expiration);
			} catch (TimeoutException ex) {
				SocketLogger?.Log($"Failed to get a response from UDP message @ client {clientId}",ex, LogLevel.Trace);
				return null;
			}
        }

		/// <summary>
		/// Send a packet build with <seealso cref="PacketBuilder"/>.
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		public override async Task<bool> SendPacketAsync(int clientId, Packet packet)
		{
			if (packet.MessageType == PacketType.File || packet.MessageType == PacketType.Folder) {
				try
				{
					int attempt = 0, maxAttempts = MessageAttempts;
					var p = AddDataOntoPacket(packet);
					var payload = PacketHelper.ByteArrayToString(p.BuildPayload());
					var response = RequestUdpMessage(clientId, (int)MessageResponseWaitTime.TotalMilliseconds, payload);

					if ( (response == null || response.Resp != ResponseType.UdpResponse) && attempt <= maxAttempts) {
						attempt++;
						SocketLogger?.Log($"Failed to deliver UDP message, retrying attempt {attempt} of {maxAttempts}.",LogLevel.Trace);
						response = RequestUdpMessage(clientId, (int)MessageResponseWaitTime.TotalMilliseconds, payload);
					} 
					else if ( (response == null || response.Resp != ResponseType.UdpResponse) && attempt > maxAttempts) {
						throw new InvalidOperationException(response.ExceptionMessage,response.Exception);
					}

					return true;
				}
				catch (Exception ex)
				{
					SocketLogger?.Log("Error sending a packet.", ex, LogLevel.Error);
					return false;
				}
			} else 
				return await base.SendPacketAsync(clientId, packet);
		}

		/// <summary>
		/// Send a packet build with <seealso cref="PacketBuilder"/>.
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		public override bool SendPacket(int clientId, Packet packet)
		{
			if (packet.MessageType == PacketType.File || packet.MessageType == PacketType.Folder) {
				try
				{
					int attempt = 0, maxAttempts = MessageAttempts;
					var p = AddDataOntoPacket(packet);
					var payload = PacketHelper.ByteArrayToString(p.BuildPayload());
					var response = RequestUdpMessage(clientId, (int)MessageResponseWaitTime.TotalMilliseconds, payload);

					if ( (response == null || response.Resp != ResponseType.UdpResponse) && attempt <= maxAttempts) {
						attempt++;
						SocketLogger?.Log($"Failed to deliver UDP message, retrying attempt {attempt} of {maxAttempts}.",LogLevel.Trace);
						response = RequestUdpMessage(clientId, (int)MessageResponseWaitTime.TotalMilliseconds, payload);
					} 
					else if ( (response == null || response.Resp != ResponseType.UdpResponse) && attempt > maxAttempts) {
						throw new InvalidOperationException(response.ExceptionMessage,response.Exception);
					}

					return true;
				}
				catch (Exception ex)
				{
					SocketLogger?.Log("Error sending a packet.", ex, LogLevel.Error);
					return false;
				}
			} else 
				return base.SendPacket(clientId, packet);
		}

		// Disposes of the server.
		public override void Dispose()
		{
			base.Dispose();
		}

	}

}