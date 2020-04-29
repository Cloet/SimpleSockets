using System;
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

        public bool Disposed { get; set; }

        public SimpleTcpClient(bool useSsl): base(useSsl, SocketProtocolType.Tcp) {
        }

        public void Connect(string serverIp, int serverPort, int autoReconnect) {

			if (string.IsNullOrEmpty(serverIp))
				throw new ArgumentNullException(nameof(serverIp));
			if (serverPort < 1 || serverPort > 65535)
				throw new ArgumentOutOfRangeException(nameof(serverPort));
			if (autoReconnect < 3)
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
				var metadata = new ClientMetadata(Listener,-1, EncryptionMethod, CompressionMethod, SocketLogger);
				Sent.Set();
				Receive(metadata);
			}
			catch (SocketException)
			{
			}
			catch (Exception ex) {

			}

		}

		internal virtual void Receive(IClientMetadata metadata) {
			try
			{

				DataReceiver rec = metadata.DataReceiver;
				while (!Token.IsCancellationRequested)
				{

					metadata.ReceivingData.Wait(Token);
					metadata.Timeout.Reset();
					metadata.ReceivingData.Reset();

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
					
				}
			}
			catch (Exception ex) {
			}
		}

		protected override void SendToServer(byte[] payload) {
			try
			{
				Sent.Wait();
				Sent.Reset();
				SocketLogger?.Log("Sending message.", LogLevel.Debug);
				Listener.BeginSend(payload, 0, payload.Length, SocketFlags.None, SendCallback, Listener);
			}
			catch (Exception ex) {
				throw new Exception(ex.ToString());
			}
		}

		protected void SendCallback(IAsyncResult result) {
			try
			{
				var socket = (Socket)result.AsyncState;
				socket.EndSend(result);
				SocketLogger?.Log("Message has been sent.", LogLevel.Debug);
				Sent.Set();
			}
			catch (Exception ex) {
				Sent.Set();
				throw new Exception(ex.ToString());
			}
		}

		public override void Dispose()
        {

        }
    }

}