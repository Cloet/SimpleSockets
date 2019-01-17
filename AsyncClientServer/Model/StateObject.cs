using System.Net.Sockets;
using System.Text;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model
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

		private const int Buffer_Size = 1024;
		private readonly byte[] buffer = new byte[Buffer_Size];
		private readonly Socket listener;
		private readonly int id;
		private StringBuilder sb;

		/// <summary>
		/// Constructor for StateObject
		/// </summary>
		/// <param name="listener"></param>
		/// <param name="id"></param>
		public StateObject(Socket listener, int id = -1)
		{
			this.listener = listener;
			this.id = id;
			this.Close = false;
			this.Reset();
		}

		/// <summary>
		/// Get the id
		/// </summary>
		public int Id
		{
			get
			{
				return this.id;
			}
		}

		/// <summary>
		/// Return of set close boolean
		/// <para>This parameter is used to check if the socket has to be closed.</para>
		/// </summary>
		public bool Close { get; set; }

		/// <summary>
		/// Gets the buffersize
		/// </summary>
		public int BufferSize
		{
			get
			{
				return Buffer_Size;
			}
		}

		/// <summary>
		/// Gets the amount of bytes in the buffer
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				return this.buffer;
			}
		}

		/// <summary>
		/// Returns the listener socket
		/// </summary>
		public Socket Listener
		{
			get
			{
				return this.listener;
			}
		}

		/// <summary>
		/// Returns the text from stringbuilder
		/// </summary>
		public string Text
		{
			get
			{
				return this.sb.ToString();
			}
		}

		/// <summary>
		/// Add text to stringbuilder
		/// </summary>
		/// <param name="text"></param>
		public void Append(string text)
		{
			this.sb.Append(text);
		}

		/// <summary>
		/// Resets the stringbuilder
		/// </summary>
		public void Reset()
		{
			this.sb = new StringBuilder();
		}

	}
}
