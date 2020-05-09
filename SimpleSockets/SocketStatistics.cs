using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SimpleSockets
{
	/// <summary>
	/// Displays statistics for a socket.
	/// </summary>
	public class SocketStatistics
	{

		private long _received = 0;

		private long _sent = 0;

		private long _receivedMessages = 0;

		private long _sentMessages = 0;

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
		public long Received => _received;

		/// <summary>
		/// Amount of bytes a socket has sent.
		/// </summary>
		public long Sent => _sent;

		/// <summary>
		/// Amount of complete messages a socket received.
		/// Only valid messages that are received are added. If an exception is thrown when receiving a message it will not be added.
		/// </summary>
		public long ReceivedMessages => _receivedMessages;

		/// <summary>
		/// Amount of messages sent fromt he socket.
		/// This may include messages that haven't been received by another socket.
		/// </summary>
		public long SentMessages => _sentMessages;

		internal SocketStatistics(SocketProtocolType protocol) {
			_protocol = protocol;
			StartTime = DateTime.Now;
		}

		internal void AddReceivedBytes(long amount) {
			if (amount > 0) {
				Interlocked.Add(ref _received, amount);
			}
		}

		internal void AddSentBytes(long amount) {
			if (amount > 0)
			{
				Interlocked.Add(ref _sent, amount);
			}
		}

		internal void AddReceivedMessages(int amount) {
			if (amount > 0)
			{
				Interlocked.Add(ref _receivedMessages, amount);
			}
		}

		internal void AddSentMessages(int amount) {
			if (amount > 0)
			{
				Interlocked.Add(ref _sentMessages, amount);
			}
		}

		public override string ToString()
		{
			var stats = "=======================================================================" + Environment.NewLine;
			stats += "|  Statistics                                                         |" + Environment.NewLine;
			stats += "|---------------------------------------------------------------------|" + Environment.NewLine;
			stats += "| - Started        : " + StartTime.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - UpTime         : " + UpTime.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Protocol       : " + Enum.GetName(typeof(SocketProtocolType), _protocol).ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Received         " + "".PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Bytes       : " + Received.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Messages    : " + ReceivedMessages.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Sent             " + "".PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Bytes       : " + Sent.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Messages    : " + SentMessages.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "=======================================================================" + Environment.NewLine;
			return stats;
		}


	}
}
