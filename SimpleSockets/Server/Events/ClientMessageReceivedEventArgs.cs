using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public class ClientMessageReceivedEventArgs: ClientDataReceivedEventArgs
	{
		internal ClientMessageReceivedEventArgs(string message,IClientInfo clientInfo, IDictionary<object,object> metadata) {
			Message = message;
			Metadata = metadata;
			ClientInfo = clientInfo;
		}

		public string Message { get; private set; }

		public override object ReceivedObject => Message;

		public override Type ReceivedObjectType => typeof(string);
	}
}
