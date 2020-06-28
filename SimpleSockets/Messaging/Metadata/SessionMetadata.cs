using SimpleSockets.Helpers;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

namespace SimpleSockets.Messaging.Metadata 
{

	internal class SessionMetadata : ISessionMetadata
	{

		public int Id { get; private set; }

		public string ClientName { get; set; }

		public Guid Guid { get; set; }

		public string OsVersion { get; set; }

		public string UserDomainName { get; set; }

		public SslStream SslStream { get; set; }

		public ManualResetEventSlim ReceivingData { get; set; } = new ManualResetEventSlim(true);

		public ManualResetEventSlim Timeout { get; set; } = new ManualResetEventSlim(true);

		public ManualResetEventSlim WritingData { get; set; } = new ManualResetEventSlim(false);

		public Socket Listener { get; set; }

		public PacketReceiver DataReceiver { get; private set; }

		public string IPv4 { get; set; }
		public string IPv6 { get; set; }

		public static int BufferSize { get; private set; } = 4096;

		private LogHelper _logger;

		public SessionMetadata(Socket listener, int id, LogHelper logger = null)
		{
			Id = id;
			_logger = logger;
			Listener = listener;
			DataReceiver = new PacketReceiver(logger, BufferSize);
		
			DetermineIPAddresses();
		}
		
		// Set the IP addresses of the remote listener.
		private void DetermineIPAddresses() {
			try {
				IPAddress remote = ((IPEndPoint) Listener.RemoteEndPoint).Address;
				IPv4 = remote.MapToIPv4().ToString();
				IPv6 = remote.MapToIPv6().ToString();
			} catch (Exception) {

			}
		}

		public void ChangeDataReceiver(PacketReceiver receiver) {
			DataReceiver = receiver;
		}

		public void ResetDataReceiver()
		{
			DataReceiver = null;
			DataReceiver = new PacketReceiver(_logger, BufferSize);
		}

		public void ChangeBufferSize(int size)
		{
			if (size < 256)
				throw new ArgumentOutOfRangeException(nameof(size), size, "Buffersize must be higher than 255.");
			BufferSize = size;
		}

		public string Info()
		{
			if (Guid != Guid.Empty)
			{
				return ($"ID:{Id}, GUID:{Guid}, IPv4:{IPv4}, IPv6:{IPv6}");
			}

			return ($"ID:{Id}, IPv4:{IPv4}, IPv6:{IPv6}");
		}

		public void Dispose()
		{
			DataReceiver = null;
		}

        public ISessionMetadata Clone(int id)
        {
            var session = new SessionMetadata(this.Listener, id, this._logger);
			session.ClientName = this.ClientName;
			session.DataReceiver = this.DataReceiver;
			session.Guid = this.Guid;
			session.UserDomainName = this.UserDomainName;
			session.OsVersion = this.OsVersion;
			return session;
        }
    }

}