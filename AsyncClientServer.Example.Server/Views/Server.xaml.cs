using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AsyncClientServer.Cryptography;
using AsyncClientServer.Example.Server.ViewModel;
using AsyncClientServer.Messaging;
using AsyncClientServer.Messaging.Metadata;
using AsyncClientServer.Server;

namespace AsyncClientServer.Example.Server
{
	/// <summary>
	/// Interaction logic for Server.xaml
	/// </summary>
	public partial class Server : Window
	{

		private IServerListener _listener;
		private ClientInfoViewModel _clientVM;

		public Server()
		{
			InitializeComponent();

			StartServer();
		}

		public void StartServer()
		{
			//_listener = new AsyncSocketSslListener(@"", "");
			_listener = new AsyncSocketListener();
			_clientVM = (ClientInfoViewModel) ListViewClients.DataContext;
			_clientVM.Listener = _listener;
			BindEvents();
			_listener.StartListening(13000);
		}

		public void BindEvents()
		{
			//Events
			_listener.ProgressFileReceived += new FileTransferProgressHandler(Progress);
			_listener.MessageReceived += new MessageReceivedHandler(MessageReceived);
			_listener.MessageSubmitted += new MessageSubmittedHandler(MessageSubmitted);
			_listener.CustomHeaderReceived += new CustomHeaderMessageReceivedHandler(CustomHeaderReceived);
			_listener.ClientDisconnected += new ClientDisconnectedHandler(ClientDisconnected);
			_listener.ClientConnected += new ClientConnectedHandler(ClientConnected);
			_listener.FileReceived += new FileFromClientReceivedHandler(FileReceived);
			_listener.ServerHasStarted += new ServerHasStartedHandler(ServerHasStarted);
			_listener.MessageFailed += new DataTransferToClientFailedHandler(MessageFailed);
		}


		//*****Begin Events************///

		private void CustomHeaderReceived(int id, string msg, string header)
		{
			Model.Client client = _clientVM.ClientList.First(x => x.Id == id);
			client.Read(header + ": " + msg);
		}

		private void MessageReceived(int id, string msg)
		{
			Model.Client client = _clientVM.ClientList.First(x => x.Id == id);
			client.Read("MESSAGE" + ": " + msg);
		}

		private void MessageSubmitted(int id, bool close)
		{
			Model.Client client = _clientVM.ClientList.First(x => x.Id == id);
			client.Read("Message submitted to client.");
		}

		private void FileReceived(int id, string path)
		{
			Model.Client client = _clientVM.ClientList.First(x => x.Id == id);
			client.Read("File/Folder has been received and stored at path " + path +".");
		}

		private void Progress(int id, int bytes, int messageSize)
		{

		}

		private void ServerHasStarted()
		{
		}

		private void MessageFailed(int id, byte[] messageData, string exceptionMessage)
		{
			Model.Client client = _clientVM.ClientList.First(x => x.Id == id);
			client.Read("Message has failed to send." + Environment.NewLine + exceptionMessage);
		}

		private void ClientConnected(int id, ISocketInfo clientState)
		{

			Model.Client c = new Model.Client(id, clientState.LocalIPv4,clientState.LocalIPv6,clientState.RemoteIPv4,clientState.RemoteIPv6);
			c.Connected = true;

			Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, 
				new Action(() =>
				{
					var count = _clientVM.ClientList.Count;
					c.ListId = ++count;
					_clientVM.ClientList.Add(c);
				}));

		}

		private void ClientDisconnected(int id)
		{
			Model.Client client = _clientVM.ClientList.First(x => x.Id == id);
			client.Connected = false;
			client.Read("Client has disconnected from the server.");
		}


		//Stop
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			_listener.StopListening();
		}

		//Start
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			_clientVM = new ClientInfoViewModel();
			_clientVM.Listener = _listener;
			ListViewClients.DataContext = _clientVM;
			_listener.ResumeListening();

		}

		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(_listener.Ip);
		}


		//******END EVENTS************///

	}
}
