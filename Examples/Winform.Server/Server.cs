using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsyncClientServer;
using AsyncClientServer.Messaging.Metadata;
using AsyncClientServer.Server;

namespace Winform.Server
{
	public partial class Server : Form
	{
		public Server()
		{
			InitializeComponent();
		}

		private AsyncSocketListener _listener;

		private void Server_Load(object sender, EventArgs e)
		{
			_listener = new AsyncSocketListener();
			BindEvents();
			_listener.StartListening(13000);

		}

		private void BindEvents()
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
			_listener.ServerErrorThrown += new ServerErrorThrownHandler(ErrorThrown);
		}

		//*****Begin Events************///

		private void CustomHeaderReceived(int id, string msg, string header)
		{
			WriteLine("The server received a message from the client with ID " + id + " the header is : " + header + " and the message is : " + msg);
		}

		private void MessageReceived(int id, string msg)
		{
			WriteLine("The server has received a message from client " + id + " the message reads: " + msg);
		}

		private void MessageSubmitted(int id, bool close)
		{
			WriteLine("A message has been sent to client " + id);
		}

		private void FileReceived(int id, string path)
		{
			WriteLine("The server has received a file from client " + id + " the file/folder is stored at : " + path);
		}

		private void Progress(int id, int bytes, int messageSize)
		{
		}

		private void ServerHasStarted()
		{
			WriteLine("The Server has started.");
		}

		private void ErrorThrown(Exception exception)
		{
			WriteLine("The server has thrown an error. Message : " + exception.Message);
			WriteLine("Stacktrace: " + exception.StackTrace);
		}

		private void MessageFailed(int id, byte[] messageData, string exceptionMessage)
		{
			WriteLine("The server has failed to deliver a message to client " + id);
			WriteLine("Error message : " + exceptionMessage);
		}

		private void ClientConnected(int id, ISocketInfo clientState)
		{
			WriteLine("Client " + id + " with IPv4 " + clientState.RemoteIPv4 + " has connected to the server.");


			ListViewItem lvi = new ListViewItem {Text = id.ToString()};
			lvi.SubItems.Add(clientState.LocalIPv4);
			lvi.SubItems.Add(clientState.RemoteIPv4);
			lvi.SubItems.Add(clientState.LocalIPv6);
			lvi.SubItems.Add(clientState.RemoteIPv6);

			this.Invoke((MethodInvoker)delegate
			{
				lstClients.Items.Add(lvi);
			});

		}

		private void ClientDisconnected(int id)
		{
			WriteLine("Client " + id + " has disconnected from the server.");
		}

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
			this.Invoke((MethodInvoker) delegate
			{
				richTextBox1.AppendText(Environment.NewLine + "[" + ConvertDateTimeToString(DateTime.Now) + "] " + text);
			});
		}


		private void Label9_Click(object sender, EventArgs e)
		{

		}

		private int GetID()
		{
			var id = txtClient.Text;

			if (string.IsNullOrEmpty(id))
				throw new Exception("Please enter the id of a client.");

			var isInt = int.TryParse(id, out var result);

			if (!isInt)
				throw new Exception("Please enter a valid value.");

			var clients = _listener.GetConnectedClients();
			if (!clients.Keys.Contains(result))
				throw new Exception("The client with the ID " + result + " is not connected.");

			return result;
		}

		private void BtnMessage_Click(object sender, EventArgs e)
		{
			try
			{

				var id = GetID();
				var msg = rtMessage.Text;
				_listener.SendMessage(id, msg, false);
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
				var id = GetID();
				_listener.SendCustomHeaderMessage(id,rtMessageCustom.Text, rtHeader.Text, false);
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

			var id = GetID();

			if (string.IsNullOrEmpty(source))
				throw new Exception("Source cannot be empty.");
			if (string.IsNullOrEmpty(destination))
				throw new Exception("Destination cannot be empty.");

			try
			{
				_listener.SendFolder(id, source, destination, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

			var id = GetID();

			if (string.IsNullOrEmpty(source))
				throw new Exception("Source cannot be empty.");
			if (string.IsNullOrEmpty(destination))
				throw new Exception("Destination cannot be empty.");

			try
			{
				_listener.SendFile(id, source, destination, false);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

	}
}
