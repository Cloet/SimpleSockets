using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public class ClientInfoEventArgs
	{
		internal ClientInfoEventArgs(ISessionInfo clientInfo) {
			ClientInfo = clientInfo;
		}

		public ISessionInfo ClientInfo { get; private set; }

	}
}
