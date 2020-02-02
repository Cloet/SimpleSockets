using NUnit.Framework;
using SimpleSockets.Client;
using SimpleSockets.Server;
using SimpleSockets;
using System.Threading;
using Test.Sockets.Utils;

namespace Test.Sockets
{
	public class Tests
	{

		private SimpleSocketTcpClient _client = null;
		private SimpleSocketTcpListener _server = null;

		[SetUp]
		public void Setup()
		{
			ManualResetEvent mre = new ManualResetEvent(false);
			_server = new SimpleSocketTcpListener();
			_client = new SimpleSocketTcpClient();

			new Thread(() => _server.StartListening(13000)).Start();

			ClientConnectedDelegate con = (client) =>
			{
				mre.Set();
			};

			_server.ClientConnected += con;

			_client.StartClient("127.0.0.1", 13000);

			mre.WaitOne(500);
			_server.ClientConnected -= con;

		}

		[TearDown]
		public void TearDown() {
			_server.Dispose();
			_client.Dispose();
			_server = null;
			_client = null;
		}

		[Test]
		public void TestSendBasicMessage()
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
		public void TestSendCustomMessage()
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
	}
}