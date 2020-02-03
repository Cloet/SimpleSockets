using NUnit.Framework;
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
	public class TcpParallelTests
	{

		private SimpleSocketTcpListener _server = null;
		private List<IClientInfo> clients = new List<IClientInfo>();

		private int _numClients = 25;
		private int _numMessages = 5000;

		[OneTimeSetUp]
		public void Setup()
		{
			_server = new SimpleSocketTcpListener();

			new Thread(() => _server.StartListening(13000)).Start();

		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_server.Dispose();
			_server = null;
		}

		[Test]
		public void Client_ParallelMessages_Server() {

			Counter counter = new Counter();
			Counter clientCounter = new Counter();

			ManualResetEvent mre = new ManualResetEvent(false);
			ClientConnectedDelegate con = (client) =>
			{
				clientCounter.Count();
			};

			SimpleSockets.Server.MessageReceivedDelegate msgRec = (client, msg) => {
				counter.Count();

				if (counter.GetCount == _numClients * _numMessages)
					mre.Set();
			};

			_server.MessageReceived += msgRec;
			_server.ClientConnected += con;

			for (var i = 0; i < _numClients; i++)
			{
				new Thread(() => StartClient()).Start();
			}

			// If it can't complete in 15 minutes fail
			mre.WaitOne(new TimeSpan(0, 15, 0));

			_server.MessageReceived -= msgRec;
			_server.ClientConnected -= con;

			Assert.AreEqual(_numClients, clientCounter.GetCount); // True if all clients have connected
			Assert.AreEqual((_numMessages * _numClients), counter.GetCount); // True if all messages have been received.

		}

		private void StartClient() {
			var client = new SimpleSocketTcpClient();
			client.StartClient("127.0.0.1", 13000);

			string message = "This is test message nr ";

			for (var i = 0; i < _numMessages; i++) {
				client.SendMessage(message + (i + 1));
			}

		}

	}

	public sealed class Counter
	{
		private int _current = 0;

		public int GetCount => _current;

		public void Count()
		{
			Interlocked.Increment(ref _current);
		}

		public void reset()
		{
			_current = 0;
		}
	}

}
