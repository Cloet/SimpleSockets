using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public class ClientObjectReceivedEventArgs
	{
		internal ClientObjectReceivedEventArgs(object recObj, Type objType, IClientInfo clientInfo, IDictionary<object,object> metadata) {
			ReceivedObject = recObj;
			ObjectType = objType;
			Metadata = metadata;
			ClientInfo = clientInfo;
		}

		public object ReceivedObject { get; private set; }

		public Type ObjectType { get; private set; }

		public IDictionary<object, object> Metadata { get; private set; }

		public IClientInfo ClientInfo { get; private set; }

	}

}
