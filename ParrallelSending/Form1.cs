using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimpleSockets.Client;
using SimpleSockets.Server;

namespace ParrallelSending
{
	public partial class Form1 : Form
	{
		private SimpleSocketListener _server;
		private IList<SimpleSocketClient> _clients = new List<SimpleSocketClient>();
		public Form1()
		{
			InitializeComponent();
		}

		private void AppendServerRTxt(string txt)
		{
			this.Invoke((MethodInvoker)delegate
			{
				rtxtServer.AppendText(txt);
			});
		}

		private void AppendClientRTxt(string txt)
		{
			this.Invoke((MethodInvoker)delegate { rtxtClients.AppendText(txt); });
		}

		private void ServerOnServerHasStarted()
		{
			AppendServerRTxt("Server has started." + Environment.NewLine);
		}

		private void OnConnectedToServer(SimpleSocketClient client)
		{
			AppendClientRTxt("Client has connected to the server." + Environment.NewLine);
		}

		private void OnMessageReceived(SimpleSocketClient client, string msg)
		{
			AppendClientRTxt("Message from server :" + msg + Environment.NewLine);
		}

		private void ServerOnMessageReceived(int id, string message)
		{
			AppendServerRTxt("Message from client " + id + "," + message + Environment.NewLine);
		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}

		private void Button1_Click(object sender, EventArgs e)
		{
			Parallel.For(0, _clients.Count, i =>
			{
				for (int j = 0; j < _clients.Count; j++)
				{
					_clients[j].SendMessage("This is test message " + i + " count " + j);
				}
			});
		}

		private void Button2_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < 10; i++)
			{
				_clients.Add(new SimpleSocketTcpClient());
				_clients[i].StartClient("127.0.0.1", 13000);
				_clients[i].MessageReceived += OnMessageReceived;
				_clients[i].ConnectedToServer += OnConnectedToServer;
			}
		}

		private void Button3_Click(object sender, EventArgs e)
		{
			_server = new SimpleSocketTcpListener();
			_server.MessageReceived += ServerOnMessageReceived;
			_server.ServerHasStarted += ServerOnServerHasStarted;
			_server.StartListening(13000);
		}
	}
}
