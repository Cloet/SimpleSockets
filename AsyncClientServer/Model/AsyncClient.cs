using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model
{


	public delegate void ConnectedHandler(AsyncClient a);
	public delegate void ClientMessageReceivedHandler(AsyncClient a, string msg);
	public delegate void ClientMessageSubmittedHandler(AsyncClient a, bool close);
	/// <summary>
	/// The Following code handles the client in an Async fashion.
	/// <para>To send messages to the corresponding Server, you should use the class "SendToServer"</para>
	/// <para>Implements
	/// <seealso cref="IAsyncClient"/>
	/// </para>
	/// </summary>
	public sealed class AsyncClient : SendToServer, IAsyncClient
	{
		private const ushort Port = 13000;

		private Socket _listener;
		private bool _close;
		private int _flag;
		private string _receivedpath = "";

		private readonly ManualResetEvent _connected = new ManualResetEvent(false);
		private readonly ManualResetEvent _sent = new ManualResetEvent(false);
		private readonly ManualResetEvent _received = new ManualResetEvent(false);

		/// <summary>
		/// This event is used to check if the client is connected
		/// </summary>
		public event ConnectedHandler Connected;
		/// <summary>
		/// This event is used to check if the client received a message
		/// </summary>
		public event ClientMessageReceivedHandler MessageReceived;
		/// <summary>
		/// This event is used to check if the client sends a message
		/// </summary>
		public event ClientMessageSubmittedHandler MessageSubmitted;

		public AsyncClient()
		{
		}

		/// <summary>
		/// Starts the client.
		/// <para>Loops every 5 seconds to try and start connection with server.</para>
		/// </summary>
		public void StartClient()
		{
			var host = Dns.GetHostEntry("127.0.0.1");
			var ip = host.AddressList[0];
			var endpoint = new IPEndPoint(ip, Port);

			try
			{
				//Try and connect
				_listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				_listener.BeginConnect(endpoint, this.OnConnectCallback, _listener);
				_connected.WaitOne();


				//If client is connected activate connected event
				var connectedHandler = this.Connected;
				if (connectedHandler != null)
				{
					connectedHandler(this);
				}

			}
			catch (Exception ex)
			{
			}
		}

		/// <summary>
		/// Check if client is connected to server
		/// </summary>
		/// <returns>bool</returns>
		public bool IsConnected()
		{
			try
			{
				return !(_listener.Poll(1000, SelectMode.SelectRead) && _listener.Available == 0);
			}
			catch (Exception)
			{
				return false;
			}
		}

		private void OnConnectCallback(IAsyncResult result)
		{
			var server = (Socket)result.AsyncState;

			try
			{
				//Client is connected to server and set connected variable
				server.EndConnect(result);
				_connected.Set();
			}
			catch (SocketException)
			{
				//Loops until connected
				Thread.Sleep(5000);
				StartClient();
			}
		}

		#region Receive data
		/// <summary>
		/// Start receiving data from server.
		/// </summary>
		public void Receive()
		{
			//Start receiving data
			var state = new StateObject(_listener);

			_flag = 0;
			state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, this.ReceiveCallback, state);

		}

		private void ReceiveCallback(IAsyncResult result)
		{
			try
			{

				int fileNameLen = 1;
				string filename = "";
				var state = (IStateObject) result.AsyncState;
				var receive = state.Listener.EndReceive(result);

				//Process received information
				if (receive > 0)
				{
					if (_flag == 0)
					{
						fileNameLen = BitConverter.ToInt32(state.Buffer, 0);
						filename = Encoding.UTF8.GetString(state.Buffer, 4, fileNameLen);
						_receivedpath = filename;
						_flag++;

						if (File.Exists(_receivedpath))
						{
							File.Delete(_receivedpath);
						}

					}

					if (filename == "NOFILE" || filename == "OBJECT")
					{
						state.Append(
							Encoding.UTF8.GetString(state.Buffer, 4 + fileNameLen, receive - (4 + fileNameLen)));
						_flag = -1;
					}
					else
					{

						if (_flag >= 1)
						{
							//Get data for file and write it
							using (BinaryWriter writer = new BinaryWriter(File.Open(_receivedpath, FileMode.Append)))
							{
								if (_flag == 1)
								{
									writer.Write(state.Buffer, 4 + fileNameLen, receive - (4 + fileNameLen));
									_flag++;
								}
								else
								{
									writer.Write(state.Buffer, 0, receive);
									writer.Close();
								}

							}

						}

						if (_flag == -1)
						{
							state.Append(Encoding.UTF8.GetString(state.Buffer, 0, receive));
						}
					}
				}

				if (receive == state.BufferSize)
				{
					state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None,
						this.ReceiveCallback, state);
				}
				else
				{
					var messageReceived = this.MessageReceived;

					if (messageReceived != null && filename == "NOFILE")
					{
						messageReceived(this, state.Text);

					}


					state.Reset();
					_received.Set();

					if (!state.Close)
					{
						Receive();
					}

				}
			}
			catch (Exception ex)
			{
			}
		}
		#endregion

		#region Send data
		/// <summary>
		/// Sends data to server
		/// <para>This method should not be used,instead use methods in "SendToServer"</para>
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="close"></param>
		protected override void SendBytes(Byte[] bytes, bool close)
		{

			try
			{

				if (!this.IsConnected())
				{
					throw new Exception("Destination socket is not connected.");
				}
				else
				{
					var send = bytes;

					_close = close;
					_listener.BeginSend(send, 0, send.Length, SocketFlags.None, this.SendCallback, _listener);
				}
			}
			catch (Exception ex)
			{
			}
		}

		private void SendCallback(IAsyncResult result)
		{
			try
			{
				var resceiver = (Socket)result.AsyncState;

				resceiver.EndSend(result);
			}
			catch (SocketException se)
			{
			}
			catch (ObjectDisposedException se)
			{
			}

			var messageSubmitted = this.MessageSubmitted;

			if (messageSubmitted != null)
			{
				messageSubmitted(this, _close);
			}

			_sent.Set();
		}
		#endregion

		private void Close()
		{
			try
			{
				if (!this.IsConnected())
				{
					return;
				}

				_listener.Shutdown(SocketShutdown.Both);
				_listener.Close();
			}
			catch (SocketException se)
			{
			}
		}

		/// <summary>
		/// Safely close client and break all connections to server.
		/// </summary>
		public void Dispose()
		{
			_connected.Dispose();
			_sent.Dispose();
			_received.Dispose();
			this.Close();
		}

	}
}
