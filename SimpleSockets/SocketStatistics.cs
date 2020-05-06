using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets
{
	/// <summary>
	/// Displays statistics for a socket.
	/// </summary>
	public class SocketStatistics
	{
		private SocketProtocolType _protocol;

		/// <summary>
		/// The datetime a socket has started.
		/// </summary>
		public DateTime StartTime { get; private set; }

		/// <summary>
		/// The amount of time a socket is active.
		/// </summary>
		public TimeSpan UpTime { get => DateTime.Now.ToUniversalTime() - StartTime.ToUniversalTime(); }

		/// <summary>
		/// Amount of bytes a socket has received.
		/// </summary>
		public long Received { get; private set; } = 0;

		/// <summary>
		/// Amount of bytes a socket has sent.
		/// </summary>
		public long Sent { get; private set; } = 0;

		/// <summary>
		/// Amount of complete messages a socket received.
		/// Only valid messages that are received are added. If an exception is thrown when receiving a message it will not be added.
		/// </summary>
		public long ReceivedMessages { get; private set; } = 0;

		/// <summary>
		/// Amount of messages sent fromt he socket.
		/// This may include messages that haven't been received by another socket.
		/// </summary>
		public long SentMessages { get; private set; } = 0;

		internal SocketStatistics(SocketProtocolType protocol) {
			_protocol = protocol;
			StartTime = DateTime.Now;
		}

		internal void AddReceivedBytes(long amount) {
			Received += amount;
		}

		internal void AddSentBytes(long amount) {
			Sent += amount;
		}

		internal void AddReceivedMessages(int amount) {
			ReceivedMessages += amount;
		}

		internal void AddSentMessages(int amount) {
			SentMessages += amount;
		}

		public override string ToString()
		{
			var stats = "=======================================================================" + Environment.NewLine;
			stats += "|  Statistics                                                         |" + Environment.NewLine;
			stats += "|---------------------------------------------------------------------|" + Environment.NewLine;
			stats += "| - Started        : " + StartTime.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - UpTime         : " + UpTime.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Protocol       : " + Enum.GetName(typeof(SocketProtocolType), _protocol).ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Received       : " + "".PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Bytes       : " + Received.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Messages    : " + ReceivedMessages.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Sent           : " + "".PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Bytes       : " + Sent.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Messages    : " + SentMessages.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "=======================================================================" + Environment.NewLine;
			return stats;
		}


	}
}
