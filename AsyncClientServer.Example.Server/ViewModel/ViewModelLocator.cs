using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Server;

namespace AsyncClientServer.Example.Server.ViewModel
{
	public class ViewModelLocator
	{
		
		private static ClientInfoViewModel _clientInfoVM;

		static ViewModelLocator()
		{
			_clientInfoVM = new ClientInfoViewModel();

		}

		public static ClientInfoViewModel ClientInfoVM => _clientInfoVM;
	}
}
