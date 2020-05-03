using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Server
{
	public class ClientInfoEventArgs
	{
		internal ClientInfoEventArgs(IClientInfo clientInfo) {
			ClientInfo = clientInfo;
		}

		public IClientInfo ClientInfo { get; private set; }

	}
}
