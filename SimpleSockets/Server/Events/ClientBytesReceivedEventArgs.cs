using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public class ClientBytesReceivedEventArgs : ClientDataReceivedEventArgs
	{
		internal ClientBytesReceivedEventArgs(IClientInfo clientInfo, byte[] data, IDictionary<object, object> metadata) {
			ClientInfo = clientInfo;
			Metadata = metadata;
			Data = data;
		}

		public byte[] Data { get; private set; }

		public override object ReceivedObject => Data;

		public override Type ReceivedObjectType => typeof(byte[]);
	}
}
