using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model
{

	/// <summary>
	/// Event that is triggered when a message is received
	/// </summary>
	/// <param name="id"></param>
	/// <param name="msg"></param>
	public delegate void MessageReceivedHandler(int id, string msg);

	/// <summary>
	/// Event that is triggered a message is sent to the server
	/// </summary>
	/// <param name="id"></param>
	/// <param name="close"></param>
	public delegate void MessageSubmittedHandler(int id, bool close);

	/// <summary>
	/// Event that is triggered when an object is received from the server
	/// </summary>
	/// <param name="id"></param>
	/// <param name="obj"></param>
	public delegate void ObjectFromClientReceivedHandler(int id, string obj);

	/// <summary>
	/// Event that is triggered when the client has disconnected
	/// </summary>
	/// <param name="id"></param>
	public delegate void ClientDisconnectedHandler(int id);

	/// <summary>
	/// Event that is triggered when the server receives a file
	/// </summary>
	/// <param name="id"></param>
	/// <param name="filepath"></param>
	public delegate void FileFromClientReceivedHandler(int id, string filepath);

	/// <summary>
	/// This class is the server, singleton class
	/// <para>Handles sending and receiving data to/from clients</para>
	/// <para>Extends <see cref="SendToClient"/>, Implements <seealso cref="IAsyncSocketListener"/></para>
	/// </summary>
	public class AsyncSocketListener : SendToClient, IAsyncSocketListener
	{
		private int _port;

		private const ushort Limit = 500;
		private int _flag;
		private string _receivedpath = "";
		private Socket _listener;

		public int Port
		{
			get => _port;
		}

		private static readonly AsyncSocketListener instance = new AsyncSocketListener();

		private readonly ManualResetEvent mre = new ManualResetEvent(false);
		private readonly IDictionary<int, IStateObject> clients = new Dictionary<int, IStateObject>();

		/// <summary>
		/// Get dictionary of clients
		/// </summary>
		/// <returns></returns>
		public IDictionary<int, IStateObject> GetClients()
		{
			return clients;
		}

		public event ObjectFromClientReceivedHandler ObjectReceived;
		public event MessageReceivedHandler MessageReceived;
		public event MessageSubmittedHandler MessageSubmitted;
		public event ClientDisconnectedHandler ClientDisconnected;
		public event FileFromClientReceivedHandler FileReceived;

		private AsyncSocketListener()
		{

		}

		public static AsyncSocketListener Instance
		{
			get
			{
				return instance;
			}
		}

		/* Starts the AsyncSocketListener */
		public void StartListening(int port)
		{
			_port = port;

			var host = Dns.GetHostEntry("127.0.0.1");
			var ip = host.AddressList[0];
			var endpoint = new IPEndPoint(ip, port);

			try
			{
				using (var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
				{
					listener.Bind(endpoint);
					listener.Listen(Limit);
					_listener = listener;
					while (true)
					{
						this.mre.Reset();
						listener.BeginAccept(this.OnClientConnect, listener);
						this.mre.WaitOne();
					}
				}
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		/* Gets a socket from the clients dictionary by his Id. */
		private IStateObject GetClient(int id)
		{
			IStateObject state;

			return this.clients.TryGetValue(id, out state) ? state : null;
		}

		/// <summary>
		/// returns if a certain client is connected
		/// </summary>
		/// <param name="id"></param>
		/// <returns>bool</returns>
		public bool IsConnected(int id)
		{
			try
			{

				var state = this.GetClient(id);

				return !(state.Listener.Poll(1000, SelectMode.SelectRead) && state.Listener.Available == 0);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
				//return false;
			}

		}

		/* Add a socket to the clients dictionary. Lock clients temporary to handle multiple access.
		 * ReceiveCallback raise a event, after the message receive complete. */
		public void OnClientConnect(IAsyncResult result)
		{
			this.mre.Set();
			_flag = 0;
			try
			{
				IStateObject state;

				lock (this.clients)
				{
					var id = !this.clients.Any() ? 1 : this.clients.Keys.Max() + 1;

					state = new StateObject(((Socket)result.AsyncState).EndAccept(result), id);
					this.clients.Add(id, state);
				}

				state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, this.ReceiveCallback, state);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
		}

		public void ReceiveCallback(IAsyncResult result)
		{
			try
			{

				int fileNameLen = 1;
				string filename = "";
				var state = (IStateObject)result.AsyncState;
				var receive = state.Listener.EndReceive(result);

				//Check if a message is being received or a file
				if (receive > 0)
				{
					//get filename or path of file
					if (_flag == 0)
					{
						fileNameLen = BitConverter.ToInt32(state.Buffer, 0);
						filename = Encoding.UTF8.GetString(state.Buffer, 4, fileNameLen);
						_receivedpath = filename;
						_flag++;

						/* check if the message is an serialized object or a command. If it is a file it will go to the else*/
						if (_receivedpath == "OBJECT" || _receivedpath == "NOFILE")
						{
							string test = Encoding.UTF8.GetString(state.Buffer, 4 + fileNameLen,
								receive - (4 + fileNameLen));
							state.Append(test);
							_flag = -1;
						}

					}
					else
					{
						/* Further convert bytes to string */
						if (_flag == -1)
						{

							string test = Encoding.UTF8.GetString(state.Buffer, 0, receive);
							state.Append(test);
						}
					}



					/* The message is a file so create a file and write data to it*/
					if (_flag >= 1)
					{
						//Get data for file and write it
						using (BinaryWriter writer = new BinaryWriter(File.Open(_receivedpath, FileMode.Append)))
						{
							if (_flag == 1)
							{
								/*Delete the file if it already exists */
								if (File.Exists(_receivedpath))
								{
									File.Delete(_receivedpath);
								}
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

				}


				if (_receivedpath != "OBJECT" && _receivedpath != "NOFILE")
				{
					FileReceived?.Invoke(state.Id, _receivedpath);
				}

				if (receive == state.BufferSize)
				{
					state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, this.ReceiveCallback, state);
				}
				else
				{


					if (_receivedpath == "OBJECT")
					{
						this.ObjectReceived?.Invoke(state.Id, state.Text);
					}
					else if (_receivedpath == "NOFILE")
					{
						this.MessageReceived?.Invoke(state.Id, state.Text);
					}

					_flag = 0;
					state.Reset();
					state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, this.ReceiveCallback, state);

				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}


		}

		/// <summary>
		/// Send data to client
		/// <para>You should not use this method. Use "SendToClient" instead</para>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="bytes"></param>
		/// <param name="close"></param>
		protected override void SendBytes(int id, Byte[] bytes, bool close)
		{
			var state = this.GetClient(id);

			if (state == null)
			{
				throw new Exception("Client does not exist.");
			}

			if (!this.IsConnected(state.Id))
			{
				//Sets client with id to disconnected
				ClientDisconnected?.Invoke(state.Id);
				throw new Exception("Destination socket is not connected.");
			}

			try
			{
				var send = bytes;

				state.Close = close;
				state.Listener.BeginSend(send, 0, send.Length, SocketFlags.None, this.SendCallback, state);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			catch (ArgumentException ae)
			{
				throw new Exception(ae.ToString());
			}
		}

		private void SendCallback(IAsyncResult result)
		{
			var state = (IStateObject)result.AsyncState;

			try
			{
				state.Listener.EndSend(result);
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			catch (ObjectDisposedException ode)
			{
				throw new Exception(ode.ToString());
			}
			finally
			{
				MessageSubmitted?.Invoke(state.Id, state.Close);
			}
		}


		/// <summary>
		/// Close a certain client
		/// </summary>
		/// <param name="id"></param>
		public void Close(int id)
		{
			var state = this.GetClient(id);

			if (state == null)
			{
				throw new Exception("Client does not exist.");
			}

			try
			{
				state.Listener.Shutdown(SocketShutdown.Both);
				state.Listener.Close();
			}
			catch (SocketException se)
			{
				throw new Exception(se.ToString());
			}
			finally
			{
				lock (this.clients)
				{
					this.clients.Remove(state.Id);
					ClientDisconnected?.Invoke(state.Id);
				}
			}
		}


		/// <summary>
		/// Properly dispose the class.
		/// </summary>
		public void Dispose()
		{
			try
			{
				foreach (var id in this.clients.Keys)
				{
					this.Close(id);
				}

				this.mre.Dispose();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.ToString());
			}
		}

	}
}
