using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AsyncClientServer.Example.Server.Annotations;
using AsyncClientServer.Example.Server.Model;
using AsyncClientServer.Example.Server.Views;
using AsyncClientServer.Server;

namespace AsyncClientServer.Example.Server.ViewModel
{
	public class ClientInfoViewModel: INotifyPropertyChanged
	{
		public ServerListener Listener;
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



		private ICommand _detailItem;

		public ICommand DetailItem
		{
			get
			{
				return _detailItem ?? (_detailItem = new RelayCommand(x => DetailItemCommand((Model.Client) x)));
			}
		}

		public void DetailItemCommand(Model.Client item)
		{
			DetailView dv = new DetailView(Listener, item);
			dv.ShowDialog();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
