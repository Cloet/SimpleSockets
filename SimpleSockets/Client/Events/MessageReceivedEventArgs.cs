using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Client
{
	public class MessageReceivedEventArgs
	{
		internal MessageReceivedEventArgs(string message, IDictionary<object,object> metadata) {
			Message = message;
			Metadata = metadata;
		}

		public string Message { get; private set; }

		public IDictionary<object,object> Metadata { get; private set; }

	}
}
