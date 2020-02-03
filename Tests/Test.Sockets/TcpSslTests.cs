using NUnit.Framework;
using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Messaging.MessageContracts;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Test.Sockets.Utils;

namespace Test.Sockets
{
	public class TcpSslTests
	{

		private SimpleSocketTcpSslClient _client = null;
		private SimpleSocketTcpSslListener _server = null;
		private int _clientid = 0;

		private MessageContractImpl _contract;

		private byte[] GetCertFileContents()
		{
			using (var stream = this.GetType().Assembly.GetManifestResourceStream("Test.Sockets.Resources.TestCertificate.pfx"))
			{
				byte[] buffer = new byte[16 * 1024];
				using (MemoryStream ms = new MemoryStream())
				{
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
			ManualResetEvent mre = new ManualResetEvent(false);
			var cert = new X509Certificate2(GetCertFileContents(), "Password");

			_server = new SimpleSocketTcpSslListener(cert);
			_client = new SimpleSocketTcpSslClient(cert);

			new Thread(() => _server.StartListening(13000)).Start();

			ClientConnectedDelegate con = (client) =>
			{
				_clientid = client.Id;
				mre.Set();
			};

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
		public void Cient_CustomMessage_Server()
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
		public void Client_MessageContract_Server()
		{
			string message = "This is a test message contract message client->server.";

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

		// Server to client tests

		[Test]
		public void Sever_Message_Cient()
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
			string message = "This is a test message contract message server->client.";

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

	}
}
