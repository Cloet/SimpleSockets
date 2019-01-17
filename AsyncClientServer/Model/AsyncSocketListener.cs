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

	public delegate void MessageReceivedHandler(int id, string msg);
	public delegate void MessageSubmittedHandler(int id, bool close);
	public delegate void ObjectReceivedHandler(int id, string obj);
	public delegate void ClientDisconnectedHandler(int id);

	/// <summary>
	/// This class is the server, singleton class
	/// <para>Handles sending and receiving data to/from clients</para>
	/// <para>Extends <see cref="SendToClient"/>, Implements <seealso cref="IAsyncSocketListener"/></para>
	/// </summary>
	public class AsyncSocketListener : SendToClient,IAsyncSocketListener
	{
		private const ushort Port = 13000;
		private const ushort Limit = 250;
		private int flag;
		private string receivedpath = "";

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

		public event ObjectReceivedHandler ObjectReceived;
		public event MessageReceivedHandler MessageReceived;
		public event MessageSubmittedHandler MessageSubmitted;
		public event ClientDisconnectedHandler ClientDisconnected;

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
		public void StartListening()
		{
			var host = Dns.GetHostEntry("127.0.0.1");
			var ip = host.AddressList[0];
			var endpoint = new IPEndPoint(ip, Port);

			try
			{
				using (var listener = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
				{
					listener.Bind(endpoint);
					listener.Listen(Limit);
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
				return false;
			}

		}

		/* Add a socket to the clients dictionary. Lock clients temporary to handle multiple access.
		 * ReceiveCallback raise a event, after the message receive complete. */
		public void OnClientConnect(IAsyncResult result)
		{
			this.mre.Set();
			flag = 0;
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
					if (flag == 0)
					{
						fileNameLen = BitConverter.ToInt32(state.Buffer, 0);
						filename = Encoding.UTF8.GetString(state.Buffer, 4, fileNameLen);
						receivedpath = filename;
						flag++;

					}

					/* check if the message is an serialized object or a command. If it is a file it will go to the else*/
					if (filename == "OBJECT" || filename == "NOFILE")
					{
						state.Append(Encoding.UTF8.GetString(state.Buffer, 4 + fileNameLen, receive - (4 + fileNameLen)));
						flag = -1;
					}
					else
					{
						/* The message is a file so create a file and write data to it*/
						if (flag >= 1)
						{
							//Get data for file and write it
							using (BinaryWriter writer = new BinaryWriter(File.Open(receivedpath, FileMode.Append)))
							{
								if (flag == 1)
								{
									/*Delete the file if it already exists */
									if (File.Exists(receivedpath))
									{
										File.Delete(receivedpath);
									}
									writer.Write(state.Buffer, 4 + fileNameLen, receive - (4 + fileNameLen));
									flag++;
								}
								else
								{
									writer.Write(state.Buffer, 0, receive);
									writer.Close();
								}
							}

						}

						/* Further convert bytes to string */
						if (flag == -1)
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


					if (receivedpath == "OBJECT")
					{
						var objectReceived = this.ObjectReceived;

						if (objectReceived != null)
						{
							objectReceived(state.Id, state.Text);
						}

					}
					else
					{
						var messageReceived = this.MessageReceived;
						if (messageReceived != null && receivedpath == "NOFILE")
						{
							messageReceived(state.Id, state.Text);

						}
					}




					state.Reset();

				}
			}
			catch (Exception ex)
			{
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
			}
			catch (ArgumentException ae)
			{
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
			}
			catch (ObjectDisposedException ode)
			{
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
			}
		}

	}
}
