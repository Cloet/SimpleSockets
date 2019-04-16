using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AsyncClientServer.Example.Server.Model;
using AsyncClientServer.Example.Server.ViewModel;
using AsyncClientServer.Server;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace AsyncClientServer.Example.Server.Views
{
	/// <summary>
	/// Interaction logic for DetailView.xaml
	/// </summary>
	public partial class DetailView : Window
	{
		private IServerListener _listener;
		private Model.Client _client;

		public DetailView(IServerListener listener, Model.Client client)
		{
			InitializeComponent();
			_listener = listener;
			_client = client;

			if (_client.LogPath != null)
			{
				RichTextBoxLogs.AppendText(File.ReadAllText(Path.GetFullPath(_client.LogPath)));
			}


			_client.TextReceived += new ReceivedText(ReceivedText);
		}

		private void ReceivedText(string message)
		{
			Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
				new Action(() =>
				{
					RichTextBoxLogs.AppendText(Environment.NewLine + message);
				}));
			
		}

		//Message
		private void ButtonMessage_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string message = new TextRange(RichTextBoxMessage.Document.ContentStart, RichTextBoxMessage.Document.ContentEnd).Text;
				_listener.SendMessage(_client.Id, message, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}


		}




		//Command 
		private void ButtonCommand_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string command = new TextRange(RichTextBoxCommand.Document.ContentStart, RichTextBoxCommand.Document.ContentEnd).Text;
				_listener.SendCommand(_client.Id, command, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}


		}


		//Folder
		private void ButtonChooseFolder_Click(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog();
			DialogResult result = dialog.ShowDialog();

			if (result == System.Windows.Forms.DialogResult.OK)
			{
				TextBoxFolderSource.Text = dialog.SelectedPath;
			}

		}

		private void ButtonFolder_Click(object sender, RoutedEventArgs e)
		{

			string source, destination;
			source = TextBoxFolderSource.Text;
			destination = TextBoxFolderDestination.Text;

			if (string.IsNullOrEmpty(source))
				throw new Exception("Source cannot be empty.");
			if (string.IsNullOrEmpty(destination))
				throw new Exception("Destination cannot be empty.");

			try
			{
				Task.Run(() => _listener.SendFolderAsync(_client.Id, source, destination, false));
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		//File
		private void ButtonChooseFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.InitialDirectory = "c:\\";
			var filter = "All Files | *.*";
			dialog.Filter = filter;
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			dialog.Multiselect = false;
			DialogResult result = dialog.ShowDialog();

			if (result == System.Windows.Forms.DialogResult.OK)
			{
				TextBoxFileSource.Text = dialog.FileNames[0];
			}

		}

		private void ButtonFile_Click(object sender, RoutedEventArgs e)
		{
			string source, destination;
			source = TextBoxFileSource.Text;
			destination = TextBoxFileDestination.Text;

			if (string.IsNullOrEmpty(source))
				throw new Exception("Source cannot be empty.");
			if (string.IsNullOrEmpty(destination))
				throw new Exception("Destination cannot be empty.");

			try
			{
				Task.Run(() => _listener.SendFileAsync(_client.Id, source, destination, false));
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}
	}
}
