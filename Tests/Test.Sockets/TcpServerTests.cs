using NUnit.Framework;
using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Test.Sockets.Utils;

namespace Test.Sockets
{
	public class TcpServerTests
	{

		private SimpleSocketTcpClient _client = null;
		private SimpleSocketTcpListener _server = null;
		private int _clientid = 0;

		[OneTimeSetUp]
		public void Setup()
		{
			ManualResetEvent mre = new ManualResetEvent(false);
			_server = new SimpleSocketTcpListener();
			_client = new SimpleSocketTcpClient();

			new Thread(() => _server.StartListening(13000)).Start();

			ClientConnectedDelegate con = (client) =>
			{
				_clientid = client.Id;
				mre.Set();
			};

			_server.ClientConnected += con;

			_client.StartClient("127.0.0.1", 13000);

			mre.WaitOne(5000);
			_server.ClientConnected -= con;

		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_server.Dispose();
			_client.Dispose();
			_server = null;
			_client = null;
		}

		[Test]
		public void TestSendBasicMessage()
		{
			string message = "This is a test message.";

			SimpleSockets.Client.MessageReceivedDelegate msgRec = (client, msg) => {
				Assert.AreEqual(message, msg);
			};

			using (var monitor = new EventMonitor(_client, "MessageReceived", msgRec, Mode.MANUAL))
			{
				_server.SendMessage(_clientid, message);
				monitor.Verify();
			}
		}

		[Test]
		public void TestSendCustomMessage()
		{
			string message = "This is a test custom header message.";
			string header = "This is a message header.";

			SimpleSockets.Client.CustomHeaderReceivedDelegate msgRec = (client, msg, head) => {
				Assert.AreEqual(message, msg);
				Assert.AreEqual(header, head);
			};

			using (var monitor = new EventMonitor(_client, "CustomHeaderReceived", msgRec, Mode.MANUAL))
			{
				_server.SendCustomHeader(_clientid, message, header);
				monitor.Verify();
			}
		}


		[Test]
		public void TestSendMessageContract()
		{
			string message = "This is a test message contract message.";

			var contract = new MessageContractImpl();
			_client.AddMessageContract(contract);
			_server.AddMessageContract(contract);

			contract.Message = message;

			Action<SimpleSocket, IClientInfo, object, string> msgRec = (socket, client, msg, head) => {
				Assert.AreEqual(message, msg);
			};

			using (var monitor = new EventMonitor(contract, "OnMessageReceived", msgRec, Mode.MANUAL))
			{
				_server.SendMessageContract(_clientid, contract);
				monitor.Verify();
			}
		}

	}
}
