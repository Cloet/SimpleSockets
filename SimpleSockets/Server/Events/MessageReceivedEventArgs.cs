using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public class MessageReceivedEventArgs
	{
		internal MessageReceivedEventArgs(string message,IClientInfo clientInfo, IDictionary<object,object> metadata) {
			Message = message;
			Metadata = metadata;
			ClientInfo = clientInfo;
		}

		public IClientInfo ClientInfo;

		public IDictionary<object, object> Metadata;

		public string Message;
	}
}
