using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public class ClientObjectReceivedEventArgs: ClientDataReceivedEventArgs
	{
		internal ClientObjectReceivedEventArgs(object recObj, Type objType, IClientInfo clientInfo, IDictionary<object,object> metadata) {
			_recObj = recObj;
			_objType = objType;
			Metadata = metadata;
			ClientInfo = clientInfo;
		}

		private object _recObj;

		private Type _objType;

		public override object ReceivedObject => _recObj;

		public override Type ReceivedObjectType => _objType;
	}

}
