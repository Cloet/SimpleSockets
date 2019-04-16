using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace AsyncClientServer.StateObject
{

	/// <summary>
	/// This class is used to keep track of certain values for server/client
	/// <para>This is needed because the client and server work async</para>
	/// <para>Implements
	///<seealso cref="IStateObject"/>
	/// </para>
	/// </summary>
	public class StateObject: IStateObject
	{

		/* Contains the state information. */

		//8192 = 8kb
		//16384 = 16kb
		//131072 = 0.131072Mb
		private const int Buffer_Size = 524288;
		private List<byte> _receivedBytes = new List<byte>();
		private StringBuilder _sb;

		public string RemoteIPv4 { get; set; }
		public string RemoteIPv6 { get; set; }

		public string LocalIPv4 { get; set; }
		public string LocalIPv6 { get; set; }

		/// <summary>
		/// Constructor for StateObject
		/// </summary>
		/// <param name="listener"></param>
		/// <param name="id"></param>
		public StateObject(Socket listener, int id = -1)
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
				RemoteIPv4 = ((IPEndPoint)Listener.LocalEndPoint).Address.MapToIPv4().ToString();
				RemoteIPv6 = ((IPEndPoint)Listener.LocalEndPoint).Address.MapToIPv6().ToString();

				LocalIPv4 = ((IPEndPoint)Listener.LocalEndPoint).Address.MapToIPv4().ToString();
				LocalIPv6 = ((IPEndPoint)Listener.LocalEndPoint).Address.MapToIPv6().ToString();
			}
			catch (Exception ex)
			{

			}
		}

		/// <summary>
		/// How many bytes have been read
		/// </summary>
		public int Read { get; private set; }

		/// <summary>
		/// The flag of the state
		/// </summary>
		public int Flag { get; set; }

		/// <summary>
		/// The header of the message
		/// </summary>
		public string Header { get; set; }

		/// <summary>
		/// True if the current message is encrypted.
		/// </summary>
		public bool Encrypted { get; set; }

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
		public int BufferSize => Buffer_Size;

		/// <summary>
		/// Get or set the MessageSize of the current message
		/// </summary>
		public int MessageSize { get; set; }

		/// <summary>
		/// Gets or sets the Sslstream of the state
		/// </summary>
		public SslStream SslStream { get; set; }

		/// <summary>
		/// Get or set the HeaderSize of the current message
		/// </summary>
		public int HeaderSize { get; set; }

		/// <summary>
		/// Gets the amount of bytes in the buffer
		/// </summary>
		public byte[] Buffer { get; set; } = new byte[Buffer_Size];

		/// <summary>
		/// Returns the listener socket
		/// </summary>
		public Socket Listener { get; }

		/// <summary>
		/// Returns the text from stringBuilder
		/// </summary>
		public string Text => this._sb.ToString();

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
		/// Add text to stringBuilder
		/// </summary>
		/// <param name="text"></param>
		public void Append(string text)
		{
			_sb.Append(text);
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
		/// Change the state of the current stateObject
		/// </summary>
		public StateObjectState.StateObjectState CurrentState { get; set; }


		/// <summary>
		/// Resets the stringBuilder and other properties
		/// </summary>
		public void Reset()
		{
			Header = "";
			MessageSize = 0;
			HeaderSize = 0;
			Encrypted = false;
			_receivedBytes = new List<byte>();
			Read = 0;
			Flag = 0;
			_sb = new StringBuilder();
		}

	}
}
