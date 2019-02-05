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

		protected AsyncClient Client;

		public ClientState(AsyncClient client)
		{
			Client = client;
		}

		public abstract void Receive(IStateObject state, int receive);

	}
}
