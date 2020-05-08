using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;

namespace SimpleSockets.Messaging {

    public class PacketReceiver {

		internal byte[] Buffer { get; private set; }

		internal int BufferSize { get; private set; }

		private byte[] _received;

		internal byte[] Received => _received;

        private LogHelper _logger;

        internal PacketReceiver(LogHelper logger, int bufferSize) {
			BufferSize = bufferSize;
            Buffer = new byte[BufferSize];
            _received = new byte[0];
            _logger = logger;
        }

		internal void ClearBuffer() {
			Buffer = null;
			Buffer = new byte[BufferSize];
		}

		/// <summary>
		/// Returns true if delimiter was found.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		internal bool AppendByteToReceived(byte readByte) {
			_received = PacketHelper.MergeByteArrays(_received, new byte[1] { readByte });

			if (_received.Length >= PacketHelper.PacketDelimiter.Length) {
				var end = new byte[PacketHelper.PacketDelimiter.Length];
				Array.Copy(_received, _received.Length - PacketHelper.PacketDelimiter.Length, end, 0, end.Length);
				if (end.SequenceEqual(PacketHelper.PacketDelimiter))
				{
					_logger?.Log("Packet delimiter found, building message from received bytes.", LogLevel.Trace);
					var temp = new byte[_received.Length - PacketHelper.PacketDelimiter.Length];
					Array.Copy(_received, 0, temp,0, temp.Length);
					_received = temp;
					return true;
				}
			}
			return false;
		}

		internal Packet BuildMessageFromPayload(byte[] encryptionPassphrase, byte[] presharedKey) {
			try
			{

				var pb = PacketReceiverBuilder.InitializeReceiver(_logger, _received[0], out var headerLength);

				var header = new byte[headerLength];
				var content = new byte[_received.Length - headerLength - 1];

				Array.Copy(_received, 1, header, 0, header.Length);
				Array.Copy(_received,1+headerLength,content,0,content.Length);

				return pb.AddPassphrase(encryptionPassphrase)
					.AppendHeaderBytes(header)
					.AppendContentBytes(content)
					.Build(presharedKey);
			}
			catch (Exception ex) {
				_logger?.Log("An error occurred receiving a message from a connected socket.", ex, LogLevel.Error);
				return null;
			}
		}

    }

}