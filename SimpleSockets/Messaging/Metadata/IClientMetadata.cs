using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

namespace SimpleSockets.Messaging.Metadata
{
	/// <summary>
	/// Interface for SocketState
	/// </summary>
	public interface IClientMetadata: IClientInfo
	{
		/// <summary>
		/// Get the buffersize
		/// </summary>
		int BufferSize { get; }

		/// <summary>
		/// Bytes that need to be processed
		/// </summary>
		byte[] UnhandledBytes { get; set; }

		/// <summary>
		/// Get or set the SslStream
		/// </summary>
		SslStream SslStream { get; set; }

		/// <summary>
		/// Manual reset event used to check when a client is busy reading data.
		/// </summary>
		ManualResetEvent MreRead { get; }

		/// <summary>
		/// Manual reset event used to check if a client is busy receiving data
		/// </summary>
		ManualResetEvent MreReceiving { get; }

		/// <summary>
		/// Manual reset event used to check if a client has timed out.
		/// </summary>
		ManualResetEvent MreTimeout { get; }

		/// <summary>
		/// get how much bytes have been read.
		/// </summary>
		int Read { get; }

		/// <summary>
		/// The flag of the stateObject, used to check  in which state the object is.
		/// </summary>
		int Flag { get; set; }
		
		/// <summary>
		/// If the state should be closed after this message
		/// </summary>
		bool Close { get; set; }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		byte[] Buffer { get; }

		/// <summary>
		/// The listener socket
		/// </summary>
		Socket Listener { get; }

		/// <summary>
		/// Append byte
		/// </summary>
		/// <param name="length"></param>
		void AppendRead(int length);

		/// <summary>
		/// Subtract from bytes
		/// </summary>
		/// <param name="length"></param>
		void SubtractRead(int length);

		/// <summary>
		/// Change the value of the buffer
		/// </summary>
		/// <param name="newBuffer"></param>
		void ChangeBuffer(byte[] newBuffer);

		/// <summary>
		/// Reset the current state object.
		/// </summary>
		void Reset();

		/// <summary>
		/// Gets how much bytes have been received
		/// </summary>
		byte[] ReceivedBytes { get; }

		/// <summary>
		/// Append the bytes to the state.
		/// </summary>
		/// <param name="bytes"></param>
		void AppendBytes(byte[] bytes);

		/// <summary>
		/// Disposes the listener object.
		/// </summary>
		void DisposeListener();

	}
}
