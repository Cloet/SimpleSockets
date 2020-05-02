using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Client
{
	public class ObjectReceivedEventArgs: DataReceivedEventArgs
	{
		internal ObjectReceivedEventArgs(object recObj, Type objType, IDictionary<object,object> metadata) {
			_recObj = recObj;
			_objType = objType;
			Metadata = metadata;
		}

		private Type _objType;

		private object _recObj;

		public override object ReceivedObject => _recObj;

		public override Type ReceivedObjectType => _objType;
	}
}
