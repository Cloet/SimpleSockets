using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Client
{
	public class BytesReceivedEventArgs : DataReceivedEventArgs
	{
		internal BytesReceivedEventArgs(byte[] data, IDictionary<object, object> metadata) {
			Metadata = metadata;
			Data = data;
		}

		public byte[] Data { get; private set; }

		public override object ReceivedObject => Data;

		public override Type ReceivedObjectType => typeof(byte[]);
	}
}
