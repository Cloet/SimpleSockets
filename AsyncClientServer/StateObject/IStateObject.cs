using System.Net.Sockets;

namespace AsyncClientServer.StateObject
{
	/// <summary>
	/// Interface for stateobject
	/// </summary>
	public interface IStateObject
	{
		/// <summary>
		/// Get the buffersize
		/// </summary>
		int BufferSize { get; }

		/// <summary>
		/// Get or set the size of the message
		/// </summary>
		int MessageSize { get; set; }

		/// <summary>
		/// Get or set the size of the header of the current message
		/// </summary>
		int HeaderSize { get; set; }

		/// <summary>
		/// get how much bytes have been read.
		/// </summary>
		int Read { get; }

		/// <summary>
		/// The flag of the stateObject, used to check  in which state the object is.
		/// </summary>
		int Flag { get; set; }

		/// <summary>
		/// The header of the message currently receiving.
		/// </summary>
		string Header { get; set; }

		/// <summary>
		/// The id of the state
		/// </summary>
		int Id { get; }

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
		/// Currently received text
		/// </summary>
		string Text { get; }

		/// <summary>
		/// Append text to stringBuilder
		/// </summary>
		/// <param name="text"></param>
		void Append(string text);

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
		/// <param name="test"></param>
		void ChangeBuffer(byte[] test);

		/// <summary>
		/// The current state object.
		/// </summary>
		StateObjectState.StateObjectState CurrentState { get; set; }

		/// <summary>
		/// Reset the current state object.
		/// </summary>
		void Reset();

	}
}
