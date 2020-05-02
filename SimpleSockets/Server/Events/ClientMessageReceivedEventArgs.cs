using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public class ClientMessageReceivedEventArgs
	{
		internal ClientMessageReceivedEventArgs(string message,IClientInfo clientInfo, IDictionary<object,object> metadata) {
			Message = message;
			Metadata = metadata;
			ClientInfo = clientInfo;
		}

		public IClientInfo ClientInfo { get; private set; }

		public IDictionary<object, object> Metadata { get; private set; }

		public string Message { get; private set; }
	}
}
