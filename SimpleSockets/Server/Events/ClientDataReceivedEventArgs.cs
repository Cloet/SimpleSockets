using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public abstract class ClientDataReceivedEventArgs
	{

		public abstract object ReceivedObject { get; }

		public abstract Type ReceivedObjectType { get; }

		public ISessionInfo ClientInfo { get; protected set; }

		public IDictionary<object, object> Metadata { get; protected set; }

	}
}
