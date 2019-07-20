using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using AsyncClientServer.Client;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Winform.Client
{
	public partial class Client : Form
	{
		public Client()
		{
			InitializeComponent();
		}


		private SocketClient _client;
		private void Client_Load(object sender, EventArgs e)
		{
			_client = new AsyncSocketClient();
			_client.AllowReceivingFiles = true;
			BindEvents();
			_client.StartClient("127.0.0.1", 13000);
		}


		private void BindEvents()
		{
			_client.ProgressFileReceived += Progress;
			_client.ConnectedToServer += ConnectedToServer;
			_client.ClientErrorThrown += ErrorThrown;
			_client.MessageReceived += ServerMessageReceived;
			_client.MessageSubmitted += ClientMessageSubmitted;
			_client.FileReceived += FileReceived;
			_client.DisconnectedFromServer += Disconnected;
			_client.MessageFailed += MessageFailed;
			_client.CustomHeaderReceived += CustomHeader;
		}

		#region Events

		private void CustomHeader(SocketClient a, string msg, string header)
		{
			WriteLine("Bytes received from server with header = " + header + " and message = " + msg);
		}


		private void ErrorThrown(SocketClient socketClient, Exception error)
		{
			WriteLine("The client has thrown an error: " + error.Message);
			WriteLine("Stacktrace: " + error.StackTrace);
		}

		private void ConnectedToServer(SocketClient a)
		{
			WriteLine("The client has connected to the server on ip " + a.Ip);
		}

		private void ServerMessageReceived(SocketClient a, string msg)
		{
			WriteLine("Message received from the server: " + msg);
		}

		private void FileReceived(SocketClient a, string file)
		{
			WriteLine("The client has received a File/Folder and has saved this File/Folder at path :  " + file);
		}

		private void Disconnected(SocketClient a, string ip, int port)
		{
			WriteLine("The client has disconnected from the server with ip " + a.Ip + "on port " + a.Port);
		}

		private void Progress(SocketClient client, int bytes, int messageSize)
		{

		}

		private void ClientMessageSubmitted(SocketClient a, bool close)
		{
			WriteLine("The client has submitted a message to the server.");
		}

		private void MessageFailed(SocketClient tcpClient, byte[] messageData, Exception exception)
		{
			WriteLine("The client has failed to send a message.");
			WriteLine("Error: " + exception.Message);
		}

		#endregion

		//Converts DateTime to a string according to cultureInfo. (uses CurrentCulture.)
		private static string ConvertDateTimeToString(DateTime time)
		{
			var cultureInfo = CultureInfo.CurrentCulture;
			//CultureInfo us = new CultureInfo("en-US");
			var shortDateFormatString = cultureInfo.DateTimeFormat.ShortDatePattern;
			var longTimeFormatString = cultureInfo.DateTimeFormat.LongTimePattern;

			return time.ToString(shortDateFormatString + " " + longTimeFormatString, cultureInfo);

		}



		private void WriteLine(string text)
		{
			this.Invoke((MethodInvoker)delegate
			{
				richTextBox1.AppendText(Environment.NewLine + "[" + ConvertDateTimeToString(DateTime.Now) + "] " + text);
			});
		}

		private void BtnMessage_Click(object sender, EventArgs e)
		{
			try
			{
				var msg = rtMessage.Text;
				_client.SendMessage(msg, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnSendCustom_Click(object sender, EventArgs e)
		{
			try
			{
				_client.SendCustomHeaderMessage(rtMessageCustom.Text, rtHeader.Text, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			
		}

		private void BtnBrowseFolder_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog();
			DialogResult result = dialog.ShowDialog();

			if (result == System.Windows.Forms.DialogResult.OK)
			{
				txtSourceFolder.Text = dialog.SelectedPath;
			}
		}

		private void BtnSendFolder_Click(object sender, EventArgs e)
		{
			string source, destination;
			source = txtSourceFolder.Text;
			destination = txtDestinatioinFolder.Text;

			if (string.IsNullOrEmpty(source))
				throw new Exception("Source cannot be empty.");
			if (string.IsNullOrEmpty(destination))
				throw new Exception("Destination cannot be empty.");

			try
			{
				_client.SendFolder(source, destination, false);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void BtnBrowseFile_Click(object sender, EventArgs e)
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
				txtSourceFile.Text = dialog.FileNames[0];
			}
		}

		private void BtnSendFile_Click(object sender, EventArgs e)
		{
			string source, destination;
			source = txtSourceFile.Text;
			destination = txtFileDestination.Text;

			if (string.IsNullOrEmpty(source))
				throw new Exception("Source cannot be empty.");
			if (string.IsNullOrEmpty(destination))
				throw new Exception("Destination cannot be empty.");

			try
			{
				Task.Run(() => _client.SendFile(source, destination, false));
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

}
