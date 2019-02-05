using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Helper;

namespace AsyncClientServer.Model
{
	public abstract class ClientState
	{

		protected IAsyncClient Client;
		protected IAsyncSocketListener Server;

		protected ClientState(IAsyncClient client)
		{
			Server = null;
			Client = client;
		}

		protected ClientState(IAsyncSocketListener server)
		{
			Client = null;
			Server = server;
		}



		public abstract void Receive(IStateObject state, int receive);

	}
}
