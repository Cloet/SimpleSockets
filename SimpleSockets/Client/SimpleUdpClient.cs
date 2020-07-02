using SimpleSockets.Helpers;
using SimpleSockets.Messaging;
using SimpleSockets.Messaging.Metadata;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSockets.Client {

	public class SimpleUdpClient : SimpleClient
	{

		private EndPoint _epFrom = new IPEndPoint(IPAddress.Any, 0);

		public SimpleUdpClient() : base(SocketProtocolType.Udp) {

		}

		public override void ConnectTo(string serverIp, int serverPort, TimeSpan autoReconnect)
		{
			if (string.IsNullOrEmpty(serverIp))
				throw new ArgumentNullException(nameof(serverIp),"Invalid server ip.");
			if (serverPort < 1 || serverPort > 65535)
				throw new ArgumentOutOfRangeException(nameof(serverPort),"A server port must be between 1 and 65535.");
			if (autoReconnect.TotalSeconds < 5) // at least 5 seconds
				throw new ArgumentOutOfRangeException(nameof(autoReconnect),"The autoreconnect time needs to be at least 5 seconds.");

			ServerIp = serverIp;
			ServerPort = serverPort;
			AutoReconnect = autoReconnect;

			EndPoint = new IPEndPoint(GetIp(ServerIp), ServerPort);

			TokenSource = new CancellationTokenSource();
			Token = TokenSource.Token;

			Task.Run(() =>
			{
				if (Disposed || Token.IsCancellationRequested)
					return;

				Listener = new Socket(EndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
				Listener.BeginConnect(EndPoint, OnConnected, Listener);
				Connected.WaitOne();
			});
		}

		// called when the client connects to the server.
		private void OnConnected(IAsyncResult result)
		{
			var socket = (Socket)result.AsyncState;

			try
			{
				socket.EndConnect(result);

				Connected.Set();
				var metadata = new SessionMetadata(Listener, -1, SocketLogger);
				Sent.Set();
				OnConnectedToServer();
				SendAuthenticationMessage();
				Task.Run(() => Receive(metadata), Token);
			}
			catch (SocketException)
			{
				if (Disposed)
					return;

				Connected.Reset();

				if (AutoReconnect.TotalMilliseconds <= 0)
					return;

				Thread.Sleep((int)AutoReconnect.TotalMilliseconds);
				if (Listener != null & !Disposed)
					Listener.BeginConnect(EndPoint, OnConnected, Listener);
				else if (Listener == null && !Disposed)
					ConnectTo(ServerIp, ServerPort, AutoReconnect);
			}
			catch (Exception ex)
			{
				OnDisconnectedFromServer();
				SocketLogger?.Log("Error finalizing connection.", ex, LogLevel.Fatal);
			}
		}

		// Receives data from the server
		protected virtual void Receive(ISessionMetadata metadata)
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
			} catch (OperationCanceledException) {
				ShutDownConnectionLost();
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error receiving data from client", ex, LogLevel.Error);
				ShutDownConnectionLost();
			}
		}

		// Called by Receive
		protected virtual void ReceiveCallback(IAsyncResult result)
		{
			if (Disposed)
				return;

			var client = (ISessionMetadata)result.AsyncState;
			try
			{
				if (client.Listener == null)
					return;

				var received = client.Listener.EndReceiveFrom(result, ref _epFrom);

				Statistics?.AddReceivedBytes(received);

				// Add byte per byte to datareceiver,
				// This way we can use a delimiter to check if a message has been received.
				if (received > 0)
				{
					SocketLogger?.Log($"Received {received} bytes.", LogLevel.Trace);
					var readBuffer = new byte[received];
					Array.Copy(client.DataReceiver.Buffer, 0, readBuffer, 0, readBuffer.Length);
					ByteDecoder(client, readBuffer);
				}

				// Resets buffer of the datareceiver.
				client.DataReceiver.ClearBuffer();
				client.ReceivingData.Set();
			} catch (SocketException se) {
				if (se.ErrorCode != (int)SocketError.NotConnected && se.ErrorCode != 107)
					SocketLogger?.Log("Server was forcibly closed.", se, LogLevel.Fatal);
				ShutDownConnectionLost();
				client.ReceivingData.Set();
			} catch (ObjectDisposedException) {
				ShutDownConnectionLost();
			} catch (Exception ex)
			{
				client.ReceivingData.Set();
				SocketLogger?.Log("Error receiving a message.", ex, LogLevel.Error);
				SocketLogger?.Log("Trying to restart the datareceiver for client " + client.Id, LogLevel.Debug);
			}
		}

		// Sends data to the server
		protected override bool SendToServer(byte[] payload)
		{
			try
			{
				if (Listener == null)
					throw new InvalidOperationException("Listener is null.");

				Sent.WaitOne();
				Sent.Reset();

				Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, Listener);

				return true;
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
				OnMessageFailed(new MessageFailedEventArgs(payload, FailedReason.NotConnected));
				return false;
			}
		}

		// Called by SendToServer
		private void SendCallback(IAsyncResult result) {
			try
			{
				Statistics?.AddSentMessages(1);
				var socket = (Socket)result.AsyncState;
				var count = socket.EndSend(result);
				Statistics?.AddSentBytes(count);
				SocketLogger?.Log($"Sent {count} bytes to the server.", LogLevel.Trace);

				Sent.Set();
			}
			catch (Exception ex)
			{
				Sent.Set();
				SocketLogger?.Log(ex, LogLevel.Error);
			}
		}

		// Send data to the server.
		protected override async Task<bool> SendToServerAsync(byte[] payload)
		{
			try
			{
				if (!IsConnected())
					throw new InvalidOperationException("Client is not connected.");

				Sent.WaitOne();
				Sent.Reset();

				if (Listener == null)
					return false;

				var result = Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, _ => { }, Listener);
				var count = await Task.Factory.FromAsync(result, (r) => Listener.EndSend(r));
				Statistics?.AddSentBytes(count);
				SocketLogger?.Log($"Sent {count} bytes to the server.", LogLevel.Trace);
				Statistics?.AddSentMessages(1);

				Sent.Set();
				return true;
			}
			catch (Exception ex)
			{
				OnMessageFailed(new MessageFailedEventArgs(payload, FailedReason.NotConnected));
				Sent.Set();
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
				return false;
			}
		}

		/// <summary>
		/// Shutdowns the client
		/// </summary>
		public override void ShutDown()
		{
			base.ShutDown();
		}

		/// <summary>
		/// Disposes of the udp client.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();
		}
	}

}