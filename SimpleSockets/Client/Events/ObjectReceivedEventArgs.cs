using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Client
{
	public class ObjectReceivedEventArgs
	{
		internal ObjectReceivedEventArgs(object recObj, Type objType, IDictionary<object,object> metadata) {
			ReceivedObject = recObj;
			ObjectType = objType;
			Metadata = metadata;
		}

		public object ReceivedObject { get; private set; }

		public Type ObjectType { get; private set; }

		public IDictionary<object,object> Metadata { get; private set; }

	}
}
