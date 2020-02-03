using NUnit.Framework;
using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Test.Sockets.Utils;

namespace Test.Sockets
{
	public class TcpSslClientTests
	{

		private SimpleSocketTcpSslClient _client = null;
		private SimpleSocketTcpSslListener _server = null;

		private byte[] GetCertFileContents() {
			using (var stream = this.GetType().Assembly.GetManifestResourceStream("Test.Sockets.Resources.TestCertificate.pfx")) {
				byte[] buffer = new byte[16 * 1024];
				using (MemoryStream ms = new MemoryStream()) {
					int read;
					while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
					{
						ms.Write(buffer, 0, read);
					}
					return ms.ToArray();
				}
			}
						
		}

		[OneTimeSetUp]
		public void Setup()
		{
			if (_client == null || _server == null) {
				ManualResetEvent mre = new ManualResetEvent(false);

				var cert = new X509Certificate2(GetCertFileContents(), "Password");
				_server = new SimpleSocketTcpSslListener(cert);
				_client = new SimpleSocketTcpSslClient(cert);

				new Thread(() => _server.StartListening(13000)).Start();

				ClientConnectedDelegate con = (client) =>
				{
					mre.Set();
				};

				_server.ClientConnected += con;

				_client.StartClient("127.0.0.1", 13000);

				mre.WaitOne(5000);
				_server.ClientConnected -= con;
			}

		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_client.Close();
			_server.Dispose();
			// _client.Dispose();
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

			using (var monitor = new EventMonitor(_server, "CustomHeaderReceived", msgRec, Mode.MANUAL))
			{
				_client.SendCustomHeader(message, header);
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
				_client.SendMessageContract(contract);
				monitor.Verify();
			}
		}

	}
}
