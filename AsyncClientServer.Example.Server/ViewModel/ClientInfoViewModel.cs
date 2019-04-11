using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AsyncClientServer.Example.Server.Annotations;

namespace AsyncClientServer.Example.Server.ViewModel
{
	public class ClientInfoViewModel: INotifyPropertyChanged
	{

		private ObservableCollection<Model.Client> _clientList = new ObservableCollection<Model.Client>();

		public ObservableCollection<Model.Client> ClientList
		{
			get => _clientList;
			set
			{
				_clientList = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
