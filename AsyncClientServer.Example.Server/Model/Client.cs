using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer.Example.Server.Model
{
	public class Client
	{

		public int Id { get; set; }
		public string RemoteIPv4 { get; set; }
		public string RemoteIPv6 { get; set; }

		public string LocalIPv4 { get; set; }
		public string LocalIPv6 { get; set; }

		public Client(int id, string localIPv4, string localIPv6, string remoteIPv4, string remoteIPv6)
		{
			Id = id;
			RemoteIPv4 = remoteIPv4;
			RemoteIPv6 = remoteIPv6;

			LocalIPv4 = localIPv4;
			LocalIPv6 = localIPv6;

		}

	}
}
