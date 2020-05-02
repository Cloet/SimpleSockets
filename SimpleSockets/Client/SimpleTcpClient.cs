using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Client;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Messaging;

namespace SimpleSockets {

    public class SimpleTcpClient : SimpleClient
    {

        public SimpleTcpClient(bool useSsl): base(useSsl, SocketProtocolType.Tcp) {
        }

		/// <summary>
		/// Tries to connect to a server.
		/// </summary>
		/// <param name="serverIp"></param>
		/// <param name="serverPort"></param>
		/// <param name="autoReconnect">Set to 0 to disable autoreconnect.</param>
        public void ConnectTo(string serverIp, int serverPort, int autoReconnect = 5) {

			if (string.IsNullOrEmpty(serverIp))
				throw new ArgumentNullException(nameof(serverIp));
			if (serverPort < 1 || serverPort > 65535)
				throw new ArgumentOutOfRangeException(nameof(serverPort));
			if (autoReconnect > 0 && autoReconnect < 4)
				throw new ArgumentOutOfRangeException(nameof(autoReconnect));

			ServerIp = serverIp;
			ServerPort = serverPort;
			AutoReconnect = new TimeSpan(0, 0, 0, autoReconnect);

			EndPoint = new IPEndPoint(GetIp(ServerIp), ServerPort);

			TokenSource = new CancellationTokenSource();
			Token = TokenSource.Token;

			Task.Run(() =>
			{
				if (Token.IsCancellationRequested)
					return;

				Listener = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				Listener.BeginConnect(EndPoint, OnConnected, Listener);
				Connected.Wait(Token);		

			});
        }

		private void OnConnected(IAsyncResult result) {
			var socket = (Socket)result.AsyncState;

			try
			{
				socket.EndConnect(result);
				Connected.Set();
				var metadata = new ClientMetadata(Listener,-1, SocketLogger);
				Sent.Set();
				Receive(metadata);
			}
			catch (SocketException)
			{
				if (Disposed)
					return;

				Connected.Reset();

				if (AutoReconnect.Seconds == 0)
					return;

				Thread.Sleep(AutoReconnect.Seconds);
				if (Listener != null & !Disposed)
					Listener.BeginConnect(EndPoint, OnConnected, Listener);
				else if (Listener == null && !Disposed)
					ConnectTo(ServerIp, ServerPort, AutoReconnect.Seconds);
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error finalizing connection.", ex, LogLevel.Fatal);
			}
		}

		internal virtual void Receive(IClientMetadata metadata) {
			try
			{
				while (!Token.IsCancellationRequested)
				{

					metadata.ReceivingData.Wait(Token);
					metadata.Timeout.Reset();
					metadata.ReceivingData.Reset();

					var rec = metadata.DataReceiver;
					metadata.Listener.BeginReceive(rec.Buffer, 0, rec.BufferSize, SocketFlags.None, ReceiveCallback, metadata);
				}
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error receiving data from client " + metadata.Id, ex, LogLevel.Error);
			}
		}

		internal virtual void ReceiveCallback(IAsyncResult result)
		{
			var client = (IClientMetadata)result.AsyncState;
			var dReceiver = client.DataReceiver;
			client.Timeout.Set();
			try
			{
				if (!IsConnected())
					ShutDown();
				else
				{
					var received = client.Listener.EndReceive(result);

					if (received > 0) {
						var readBuffer = client.DataReceiver.Buffer.Take(received).ToArray();
						for (int i = 0; i < readBuffer.Length; i++) {
							var end = client.DataReceiver.AppendByteToReceived(readBuffer[i]);
							if (end) {
								var message = client.DataReceiver.BuildMessageFromPayload(EncryptionPassphrase, PreSharedKey);
								if (message != null)
									OnMessageReceivedHandler(message);
								client.ResetDataReceiver();
							}
						}
					}
					// Allow server to receive more bytes of a client.
					client.ReceivingData.Set();
				}
			}
			catch (Exception ex)
			{
				SocketLogger?.Log("Error receiving a message.", ex, LogLevel.Error);
			}
		}

		public void ShutDown() {
			try
			{
				Connected.Reset();
				TokenSource.Cancel();
				if (Listener != null) {
					Listener.Shutdown(SocketShutdown.Both);
					Listener.Close();
					Listener = null;
					OnDisconnectedFromServer();
				}
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error closing the client", ex, LogLevel.Error);
			}
		}

		protected override async Task<bool> SendToServerAsync(byte[] payload)
		{
			try
			{
				Sent.Wait();
				Sent.Reset();

				if (Listener == null)
					return false;

				var result = Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, _ => { }, Listener);
				var count = await Task.Factory.FromAsync(result, (r) => Listener.EndSend(r));
				Statistics?.AddSentBytes(count);
				Sent.Set();
				return true;
			}
			catch (Exception ex) {
				Sent.Set();
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
				return false;
			}
		}

		protected override void SendToServer(byte[] payload) {
			try
			{
				Sent.Wait();
				Sent.Reset();
				Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, Listener);
			}
			catch (Exception ex) {
				SocketLogger?.Log("Error sending a message.", ex, LogLevel.Error);
			}
		}

		protected void SendCallback(IAsyncResult result) {
			try
			{
				var socket = (Socket)result.AsyncState;
				var count = socket.EndSend(result);
				Statistics?.AddSentBytes(count);
				Sent.Set();
			}
			catch (Exception ex) {
				Sent.Set();
				SocketLogger?.Log(ex, LogLevel.Error);
			}
		}

		public override void Dispose()
        {

        }

	}

}