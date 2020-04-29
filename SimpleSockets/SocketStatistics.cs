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

		public TimeSpan UpTime { get => DateTime.Now.ToUniversalTime() - StartTime; }

		public long Received { get; private set; } = 0;

		public long Sent { get; private set; } = 0;

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

		public override string ToString()
		{
			var stats = "=======================================================================" + Environment.NewLine;
			stats += "|  Statistics                                                         |" + Environment.NewLine;
			stats += "|---------------------------------------------------------------------|" + Environment.NewLine;
			stats += "| - Started        : " + StartTime.ToString().PadRight(48) + "|" + Environment.NewLine;
			stats += "| - UpTime         : " + UpTime.ToString().PadRight(48) + "|" + Environment.NewLine;
			stats += "| - Bytes Received : " + Received.ToString().PadRight(48) + "|" + Environment.NewLine;
			stats += "| - Bytes Sent     : " + Sent.ToString().PadRight(48) + "|" + Environment.NewLine;
			stats += "| - Protocol       : " + Enum.GetName(typeof(SocketProtocolType), _protocol).ToString().PadRight(48) + "|" + Environment.NewLine;
			stats += "| - Ssl            : " + (_ssl ? "Yes".PadRight(48) : "No".PadRight(48)) + "|" + Environment.NewLine;
			stats += "=======================================================================" + Environment.NewLine;
			return stats;
		}


	}
}
