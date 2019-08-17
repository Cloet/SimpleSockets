using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace SimpleSockets.Messaging.Metadata
{

	/// <summary>
	/// This class is used to keep track of certain values for server/client
	/// <para>This is needed because the client and server work async</para>
	/// <para>Implements
	///<seealso cref="ISocketState"/>
	/// </para>
	/// </summary>
	internal class SocketState: ISocketState
	{

		/* Contains the state information. */

		//8192 = 8kb
		//16384 = 16kb
		//131072 = 0.131072Mb
		private static int _bufferSize = 524288;
		private IList<byte> _receivedBytes = new List<byte>();

		public string RemoteIPv4 { get; set; }
		public string RemoteIPv6 { get; set; }

		public string LocalIPv4 { get; set; }
		public string LocalIPv6 { get; set; }


		/// <summary>
		/// Constructor for StateObject
		/// </summary>
		/// <param name="listener"></param>
		/// <param name="id"></param>
		internal SocketState(Socket listener, int id = -1)
		{
			Listener = listener;

			SetIps();

			Id = id;
			Close = false;
			Reset();
		}

		private void SetIps()
		{
			try
			{
				RemoteIPv4 = ((IPEndPoint)Listener.RemoteEndPoint).Address.MapToIPv4().ToString();
				RemoteIPv6 = ((IPEndPoint)Listener.RemoteEndPoint).Address.MapToIPv6().ToString();

				LocalIPv4 = ((IPEndPoint)Listener.LocalEndPoint).Address.MapToIPv4().ToString();
				LocalIPv6 = ((IPEndPoint)Listener.LocalEndPoint).Address.MapToIPv6().ToString();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message, ex);
			}
		}

		internal SimpleMessage SimpleMessage { get; set; }

		/// <summary>
		/// Change the buffer size of the socket state
		/// </summary>
		/// <param name="size"></param>
		internal static void ChangeBufferSize(int size)
		{
			_bufferSize = size;
		}

		/// <summary>
		/// How many bytes have been read
		/// </summary>
		public int Read { get; private set; }

		/// <summary>
		/// Bytes that have been read but are not yet handled
		/// </summary>
		public byte[] UnhandledBytes { get; set; }

		/// <summary>
		/// The flag of the state
		/// </summary>
		public int Flag { get; set; }

		/// <summary>
		/// Get the id
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// Return of set close boolean
		/// <para>This parameter is used to check if the socket has to be closed.</para>
		/// </summary>
		public bool Close { get; set; }

		/// <summary>
		/// Gets the bufferSize
		/// </summary>
		public int BufferSize => _bufferSize;
		
		/// <summary>
		/// Gets or sets the Sslstream of the state
		/// </summary>
		public SslStream SslStream { get; set; }

		/// <summary>
		/// Gets the amount of bytes in the buffer
		/// </summary>
		public byte[] Buffer { get; set; } = new byte[_bufferSize];

		/// <summary>
		/// Returns the listener socket
		/// </summary>
		public Socket Listener { get; }

		/// <summary>
		/// Gets how much bytes have been received.
		/// </summary>
		public byte[] ReceivedBytes => _receivedBytes.ToArray();

		/// <summary>
		/// Appends a byte array
		/// </summary>
		/// <param name="bytes"></param>
		public void AppendBytes(byte[] bytes)
		{
			foreach (var b in bytes)
			{
				_receivedBytes.Add(b);
			}
		}

		/// <summary>
		/// Appends how much bytes have been read
		/// </summary>
		/// <param name="length"></param>
		public void AppendRead(int length)
		{
			Read += length;
		}

		/// <summary>
		/// Removes some bytes that have been read
		/// </summary>
		/// <param name="length"></param>
		public void SubtractRead(int length)
		{
			Read -= length;
		}

		/// <summary>
		/// Change the buffer
		/// </summary>
		/// <param name="bytes"></param>
		public void ChangeBuffer(byte[] bytes)
		{
			Buffer = bytes;
		}
		
		/// <summary>
		/// Resets the stringBuilder and other properties
		/// </summary>
		public void Reset()
		{
			_receivedBytes = new List<byte>();
			Read = 0;
			Flag = 0;
			//SimpleMessage = null;
			//ChangeBuffer(new byte[_bufferSize]);
		}

	}
}
