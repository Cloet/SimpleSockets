using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Client
{
	public abstract class DataReceivedEventArgs
	{

		public abstract object ReceivedObject { get; }

		public abstract Type ReceivedObjectType { get; }

		public IDictionary<object, object> Metadata { get; protected set; }

	}
}
