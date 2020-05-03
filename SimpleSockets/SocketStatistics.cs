using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets
{
	public class SocketStatistics
	{
		private SocketProtocolType _protocol;

		private bool _ssl;

		public DateTime StartTime { get; private set; }

		public TimeSpan UpTime { get => DateTime.Now.ToUniversalTime() - StartTime.ToUniversalTime(); }

		public long Received { get; private set; } = 0;

		public long Sent { get; private set; } = 0;

		public long ReceivedMessages { get; private set; } = 0;

		public long SentMessages { get; private set; } = 0;

		public SocketStatistics(bool ssl, SocketProtocolType protocol) {
			_ssl = ssl;
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
			stats += "| - Received       : " + "".PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Bytes       : " + Received.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Messages    : " + ReceivedMessages.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Sent           : " + "".PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Bytes       : " + Sent.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "|    > Messages    : " + SentMessages.ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Protocol       : " + Enum.GetName(typeof(SocketProtocolType), _protocol).ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Ssl            : " + (_ssl ? "Yes".PadRight(49) : "No".PadRight(49)) + "|" + Environment.NewLine;
			stats += "=======================================================================" + Environment.NewLine;
			return stats;
		}


	}
}
