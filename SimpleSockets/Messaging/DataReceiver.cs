using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Helpers;

namespace SimpleSockets.Messaging {

    internal class DataReceiver: IDisposable {

        internal SslStream SslStream { get; set; }

        internal ManualResetEventSlim ReceivingData { get; set; }

        internal ManualResetEventSlim Timeout { get; set; }

        internal ManualResetEventSlim WritingData { get; set; }

        internal Socket Listener { get; set; }

        internal MessageState MessageStatus { get; private set; }

        internal byte[] Buffer { get; }

        internal int BufferSize => 65536;

        private byte[] _buffer;

        private byte[] _received;

        private byte[] _header;

        private LogHelper _logger;

        private FluentMessageBuilder _messageBuilder = null;

        private int _headerLength = 0;

        internal DataReceiver(Socket listener, LogHelper _logger) {
            Listener = listener;
            _buffer = new byte[BufferSize];
            _received = new byte[0];
            MessageStatus = MessageState.Idle;
        }

        /// <summary>
        /// Returns true if a full message has been read.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        internal async Task<MessageState> ReceiveData(int amount) {
            if (amount == 0) return MessageStatus;

            var readBytes = new byte[0];

            try {
                // When the first bytes of a new message are received set header information and get header length.
                // else just get bytes from the buffer.
                if (_messageBuilder == null) {
                    _messageBuilder = FluentMessageBuilder.InitializeReceiver(_logger, _buffer.Take(1).First() , out _headerLength);            
                    MessageStatus = MessageState.ReceivingHeader;

                    if (amount == 1) return MessageStatus;

                    amount -= 1; // Bytes left to process.
                    readBytes = _buffer.Skip(1).Take(_buffer.Length -1).ToArray();
                } else
                    readBytes = _buffer.Take(amount).ToArray();
                    
                var packetEndFound = false;

                // Check if packet delimiter was found
                if (amount >= 4) {
                    var end = readBytes.Skip(readBytes.Length - 4).Take(4).ToArray();
                    if (end.SequenceEqual(MessageHelper.PacketDelimiter)) {
                        _logger?.Log("Packet delimiter found.", LogLevel.Debug);
                        packetEndFound = true;
                    }
                }

                // Process message headers.
                if (MessageStatus == MessageState.ReceivingHeader) {
                    if (amount <= _headerLength)
                        _messageBuilder.AppendHeaderBytes(readBytes);
                    else
                        _messageBuilder.AppendHeaderBytes(readBytes.Take(_headerLength).ToArray());

                    if (_messageBuilder.TheMessage.MessageHeader.Length > _headerLength) {
                        _logger.Log($"Expected {_headerLength} header bytes but read {_messageBuilder.TheMessage.MessageHeader.Length}... skipping message.", LogLevel.Warning);
                        MessageStatus = MessageState.Skipping;
                    }

                    if (MessageStatus != MessageState.Skipping && _messageBuilder.TheMessage.MessageHeader.Length == _headerLength) {
                        _messageBuilder.BuildMessageHeader();
                        MessageStatus = MessageState.ReceivingContent;  
                    }
                }

                if (MessageStatus == MessageState.ReceivingContent) {

                }

                // If delimiter was found this means a complete message was read.
                if (packetEndFound) {
                    _messageBuilder = null;
                    MessageStatus = MessageState.Completed;
                }

                return MessageStatus;                
            } catch (Exception ex) {
                _logger?.Log("Something went wrong trying to receive data.", ex, LogLevel.Error);
                throw new Exception(ex.ToString());
            }
        }

        internal void PacketReceived() {

        }

        internal void AppendBytesToReceived(byte[] bytesToAppend) => _received = MessageHelper.MergeByteArrays(_received, bytesToAppend);

        internal void AppendBytesToHeader(byte[] bytesToAppend) => _header = MessageHelper.MergeByteArrays(_header, bytesToAppend);

        public void Dispose()
        {
            ReceivingData.Dispose();
            Timeout.Dispose();
            WritingData.Dispose();
            Listener = null;
        }
    }

}