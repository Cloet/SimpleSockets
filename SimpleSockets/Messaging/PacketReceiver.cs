using System;
using SimpleSockets.Helpers;

namespace SimpleSockets.Messaging
{

    public class PacketReceiver
    {

        internal byte[] Buffer { get; private set; }

        internal int BufferSize { get; private set; }

        private byte[] _received;

        internal byte[] Received => _received;

        internal bool LastByteEscape { get; set; }

        private LogHelper _logger;

        internal PacketReceiver(LogHelper logger, int bufferSize)
        {
            BufferSize = bufferSize;
            Buffer = new byte[BufferSize];
            _received = new byte[0];
            _logger = logger;
            LastByteEscape = false;
        }

        internal void ClearBuffer()
        {
            Buffer = null;
            Buffer = new byte[BufferSize];
        }

        /// <summary>
        /// Returns true if delimiter was found.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        internal bool AppendByteToReceived(byte readByte)
        {
            _received = PacketHelper.MergeByteArrays(_received, new byte[1] { readByte });
            return false;
        }


        internal Packet BuildMessageFromPayload(byte[] encryptionPassphrase, byte[] presharedKey)
        {
            try
            {
                var pb = PacketReceiverBuilder.InitializeReceiver(_logger, _received[0], out var headerLength);

                var header = new byte[headerLength];
                var content = new byte[_received.Length - headerLength - 1];

                Array.Copy(_received, 1, header, 0, header.Length);
                Array.Copy(_received, 1 + headerLength, content, 0, content.Length);

                return pb.AddPassphrase(encryptionPassphrase)
                    .AppendHeaderBytes(header)
                    .AppendContentBytes(content)
                    .Build(presharedKey);
            }
            catch (Exception ex)
            {
                _logger?.Log("An error occurred receiving a message from a connected socket.", ex, LogLevel.Error);
                return null;
            }
        }

    }

}
