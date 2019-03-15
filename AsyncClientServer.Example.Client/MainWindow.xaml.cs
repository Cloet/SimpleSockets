using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AsyncClientServer.Client;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace AsyncClientServer.Example.Client
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		private string _selectedFileFolder = null;
		private IAsyncClient _client;

		public MainWindow()
		{
			InitializeComponent();
		}

		//Starts the client in a separate thread
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Thread t = new Thread(StartClient);
			t.Start();
		}

		private void StartClient()
		{
			_client = new AsyncClient();

			//Bind events
			_client.ProgressFileReceived += new ProgressFileTransferHandler(Progress);
			_client.Connected += new ConnectedHandler(ConnectedToServer);
			_client.MessageReceived += new ClientMessageReceivedHandler(ServerMessageReceived);
			_client.MessageSubmitted += new ClientMessageSubmittedHandler(ClientMessageSubmitted);
			_client.FileReceived += new FileFromServerReceivedHandler(FileReceived);
			_client.Disconnected += new DisconnectedFromServerHandler(Disconnected);


			_client.StartClient("127.0.0.1", 13000);

		}




		//Events
		private void ConnectedToServer(IAsyncClient a)
		{
			Dispatcher.Invoke(() => { TextBlockStatus.Text = "CONNECTED"; });
			AppendRichtTextBox("Client has connected to the server.");
		}

		private void ServerMessageReceived(IAsyncClient a, string header, string msg)
		{
			AppendRichtTextBox(header + " received from the server:" + msg);
		}

		private void FileReceived(IAsyncClient a, string file)
		{
			_client.SendMessage("File has been received.", false);
			Dispatcher.Invoke(() => { ProgressBarProgress.Value = 0; });
			AppendRichtTextBox("File has been received and is stored at path: " + file);
		}

		private void Disconnected(string ip, int port)
		{
			Dispatcher.Invoke(() => { TextBlockStatus.Text = "NOT CONNECTED"; });
			AppendRichtTextBox("Client has disconnected from the server with ip" + ip + " on port " + port);
		}

		private void Progress(IAsyncClient a, int bytes, int messageSize)
		{
			double b = double.Parse(bytes.ToString());
			double m = double.Parse(messageSize.ToString());

			double percentageDone = b / m * 100;

			Dispatcher.Invoke(() => ProgressBarProgress.Value = percentageDone);
		}

		private void ClientMessageSubmitted(IAsyncClient a, bool close)
		{
			//Nothing
		}

		//End Events

		//Append to textbox from separate thread
		private void AppendRichtTextBox(string append)
		{
			Dispatcher.Invoke(() => { RichTextBoxOutput.AppendText(Environment.NewLine + append); });
		}

		//Buttons
		//Searches for a file
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.InitialDirectory = "c:\\";
			var filter = "All Files | *.*";
			dialog.Filter = filter;
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			dialog.Multiselect = false;

			if (dialog.ShowDialog() == true)
			{
				_selectedFileFolder = dialog.FileNames[0];
			}

			TextBlockSource.Text = _selectedFileFolder;

		}

		//Searches for a folder
		private void ButtonFolder_Click(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog();
			DialogResult result = dialog.ShowDialog();

			if (result == System.Windows.Forms.DialogResult.OK)
			{
				_selectedFileFolder = dialog.SelectedPath;
			}

			TextBlockSource.Text = _selectedFileFolder;
		}

		//Send a file or folder
		private void ButtonSendFileFolder_Click(object sender, RoutedEventArgs e)
		{

			try
			{
				if (TextBlockTarget.Text == string.Empty)
					throw new Exception("The target cannot be empty.");
				if (_selectedFileFolder == string.Empty)
					throw new Exception("The source cannot be empty.");

				bool encrypt = CheckBoxFileFolder.IsChecked == true;

				if (Directory.Exists(Path.GetFullPath(_selectedFileFolder)))
				{
					_client.SendFolderAsync(Path.GetFullPath(_selectedFileFolder), Path.GetFullPath(TextBlockTarget.Text),encrypt, false);
				}
				else
				{
					_client.SendFileAsync(Path.GetFullPath(_selectedFileFolder), Path.GetFullPath(TextBlockTarget.Text),false);
					//_client.SendFile(Path.GetFullPath(_selectedFileFolder), Path.GetFullPath(TextBlockTarget.Text),encrypt,true, false);
				}

			}
			catch (Exception ex)
			{
				AppendRichtTextBox("\nError \n" + ex.Message);
			}

		}

		//Send a command
		private void ButtonSendCommand_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (TextBoxCommand.Text == string.Empty)
					throw new Exception("The command cannot be empty.");

				bool encrypt = CheckBoxMessage.IsChecked == true;

				_client.SendCommandAsync(TextBoxCommand.Text,encrypt, false);
			}
			catch (Exception ex)
			{
				AppendRichtTextBox("\nError \n" + ex.Message);
			}
		}

		//Send a message
		private void ButtonSendMessage_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (TextBoxMessage.Text == string.Empty)
					throw new Exception("The message cannot be empty");

				bool encrypt = CheckBoxMessage.IsChecked == true;

				_client.SendMessageAsync(TextBoxMessage.Text,encrypt, false);
			}
			catch (Exception ex)
			{
				AppendRichtTextBox("\nError \n" + ex.Message);
			}
		}

		//Properly exists the app (closes the client thread)
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Environment.Exit(0);
		}


	}
}
