using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Client
{
	public class MessageReceivedEventArgs: DataReceivedEventArgs
	{
		internal MessageReceivedEventArgs(string message, IDictionary<object,object> metadata) {
			Message = message;
			Metadata = metadata;
		}

		public string Message { get; private set; }

		public override Type ReceivedObjectType => typeof(string);

		public override object ReceivedObject => Message;
	}
}
