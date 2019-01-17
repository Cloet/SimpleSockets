using System.Net.Sockets;

namespace AsyncClientServer.Helper
{
	/// <summary>
	/// Interface for stateobject
	/// </summary>
	public interface IStateObject
	{

		int BufferSize { get; }

		int Id { get; }

		bool Close { get; set; }

		byte[] Buffer { get; }

		Socket Listener { get; }

		string Text { get; }

		void Append(string text);

		void Reset();

	}
}
