using System.Net.Sockets;

namespace AsyncClientServer.Helper
{
	/// <summary>
	/// Interface for stateobject
	/// </summary>
	public interface IStateObject
	{

		int BufferSize { get; }

		int MessageSize { get; set; }

		int HeaderSize { get; set; }

		int Read { get; }

		int Flag { get; set; }

		string Header { get; set; }

		int Id { get; }

		bool Close { get; set; }

		byte[] Buffer { get; }

		Socket Listener { get; }

		string Text { get; }

		void Append(string text);

		void AppendRead(int length);

		void Reset();

	}
}
