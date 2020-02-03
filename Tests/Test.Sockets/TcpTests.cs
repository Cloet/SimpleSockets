using NUnit.Framework;
using SimpleSockets.Client;
using SimpleSockets.Server;
using SimpleSockets;
using System.Threading;
using Test.Sockets.Utils;
using System;
using SimpleSockets.Messaging.Metadata;

namespace Test.Sockets
{
	[TestFixture]
	public class TcpTests
	{
		private SimpleSocketTcpClient _client = null;
		private SimpleSocketTcpListener _server = null;
		private int _clientid = 0;

		private MessageContractImpl _contract;

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

			_client.ObjectSerializer = new JsonSerialization();
			_server.ObjectSerializer = new JsonSerialization();

			_contract = new MessageContractImpl();
			_client.AddMessageContract(_contract);
			_server.AddMessageContract(_contract);

			_server.ClientConnected += con;

			_client.StartClient("127.0.0.1", 13000);

			mre.WaitOne(10000);
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
		public void Client_Message_Server()
		{
			string message = "This is a test message.";

			SimpleSockets.Server.MessageReceivedDelegate msgRec = (client, msg) => {
				Assert.AreEqual(message, msg);
			};

			using (var monitor = new EventMonitor(_server, "MessageReceived", msgRec, Mode.MANUAL))
			{
				_client.SendMessage(message);
				monitor.Verify();
			}
		}

		[Test]
		public void Client_CustomMessage_Server()
		{
			string message = "This is a test custom header message.";
			string header = "This is a message header.";
			
			SimpleSockets.Server.CustomHeaderReceivedDelegate msgRec = (client, msg, head) => {
				Assert.AreEqual(message, msg);
				Assert.AreEqual(header, head);
			};

			using (var monitor = new EventMonitor(_server, "CustomHeaderReceived", msgRec, Mode.MANUAL)) {
				_client.SendCustomHeader(message, header);
				monitor.Verify();
			}
		}

		[Test]
		public void Client_MessageContract_Server()
		{
			string message = "This is a test message contract message client -> server.";

			_contract.Message = message;

			Action<SimpleSocket, IClientInfo, object, string> msgRec = (socket, client, msg, head) => {
				Assert.AreEqual(message, msg);
			};

			using (var monitor = new EventMonitor(_contract, "OnMessageReceived", msgRec, Mode.MANUAL))
			{
				_client.SendMessageContract(_contract);
				monitor.Verify();
			}
		}

		[Test]
		public void Client_Object_Server() {

			string name = "Cloet";
			string text = "This is the text of a custom object send to the server from the client.";
			DateTime date = new DateTime(2000, 1, 1);
			double number = 50.5989;

			var customObject = new DataObject(name, text, number, date);

			SimpleSockets.Server.ObjectReceivedDelegate msgRec = (client, obj, objType) => {
				if (objType == typeof(DataObject))
				{
					var rec = (DataObject)Convert.ChangeType(obj, objType);
					Assert.AreEqual(name, rec.Name);
					Assert.AreEqual(text, rec.Text);
					Assert.AreEqual(date, rec.Date);
					Assert.AreEqual(number, rec.Number);
				}
				else
					Assert.IsTrue(false);
			};

			using (var monitor = new EventMonitor(_server, "ObjectReceived", msgRec, Mode.MANUAL)) {
				_client.SendObject(customObject);
				monitor.Verify();
			}

		}

		// Server sending messages to client

		[Test]
		public void Server_Message_Client()
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
		public void Server_CustomMessage_Client()
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
		public void Server_MessageContract_Client()
		{
			string message = "This is a test message contract message server -> client.";

			_contract.Message = message;

			Action<SimpleSocket, IClientInfo, object, string> msgRec = (socket, client, msg, head) => {
				Assert.AreEqual(message, msg);
			};

			using (var monitor = new EventMonitor(_contract, "OnMessageReceived", msgRec, Mode.MANUAL))
			{
				_server.SendMessageContract(_clientid, _contract);
				monitor.Verify();
			}
		}

		[Test]
		public void Server_Object_Client()
		{

			string name = "Cloet";
			string text = "This is the text of a custom object send to a client from the server.";
			DateTime date = new DateTime(2000, 1, 1);
			double number = 50.5989;

			var customObject = new DataObject(name, text, number, date);

			SimpleSockets.Client.ObjectReceivedDelegate msgRec = (client, obj, objType) => {
				if (objType == typeof(DataObject))
				{
					var rec = (DataObject)Convert.ChangeType(obj, objType);
					Assert.AreEqual(name, rec.Name);
					Assert.AreEqual(text, rec.Text);
					Assert.AreEqual(date, rec.Date);
					Assert.AreEqual(number, rec.Number);
				}
				else
					Assert.IsTrue(false);
			};

			using (var monitor = new EventMonitor(_client, "ObjectReceived", msgRec, Mode.MANUAL))
			{
				_server.SendObject(_clientid, customObject);
				monitor.Verify();
			}

		}

	}
}