using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Client
{
	public class MessageFailedEventArgs
	{
		internal MessageFailedEventArgs(byte[] bytes, FailedReason fReason) {
			Payload = bytes;
			FailedReason = fReason;
		}

		public byte[] Payload { get; private set; }

		public FailedReason FailedReason { get; private set; }

	}
}
