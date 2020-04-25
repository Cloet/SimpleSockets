using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Helpers;

namespace SimpleSockets.Messaging {

    internal class DataReceiver {

        internal MessageState MessageStatus { get; private set; }

        internal byte[] Buffer { 
            get => _buffer;
            set { 
                _buffer = value;
            }
        }

        internal int BufferSize => 65536;

        internal byte[] ExtraBytes => _extraReceived;

        private byte[] _buffer;

        private byte[] _received;

        private byte[] _extraReceived;

        private LogHelper _logger;

        private FluentMessageBuilder _messageBuilder = null;

        private int _headerLength = 0;

        private long _contentLength = 0;


        internal DataReceiver(LogHelper logger) {
            _buffer = new byte[BufferSize];
            _received = new byte[0];
            MessageStatus = MessageState.Idle;
            _logger = logger;
        }

        internal DataReceiver(LogHelper logger, byte[] extraBytes) {
            _buffer = new byte[BufferSize];
            _received = new byte[0];
            MessageStatus = MessageState.Idle;
            _extraReceived = extraBytes;
            _logger = logger;
        }

        /// <summary>
        /// Returns true if a full message has been read.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        internal MessageState ReceiveData(int amount) {
            if (_extraReceived?.Length > 0) {
                _buffer = MessageHelper.MergeByteArrays(_extraReceived, _buffer);
                amount += _extraReceived.Length;
            }
            
            if (amount == 0) return MessageStatus;

            var readBytes = new byte[0];

            // Error was thrown or invalid message read until delimiter is found and finds a next message.
            if (MessageStatus == MessageState.Skipping) {
                var delimiterFound = SkipMessageReadUntilDelimiter(_buffer.Take(amount).ToArray(), out var extraBytes);
                _extraReceived = extraBytes; // Bytes of the next message.
                
                if (delimiterFound) return MessageState.Completed;         
                return MessageStatus;
            }
            
            try {
                // When the first bytes of a new message are received set header information and get header length.
                // else just get bytes from the buffer.
                if (_messageBuilder == null) {
                    _messageBuilder = FluentMessageBuilder.InitializeReceiver(_logger, _buffer.Take(1).First() , out _headerLength);            
                    MessageStatus = MessageState.ReceivingHeader;

                    if (amount == 1) return MessageStatus;

                    amount -= 1; // Bytes left to process.
                    readBytes = _buffer.Skip(1).Take(amount).ToArray();
                } else
                    readBytes = _buffer.Take(amount).ToArray();
                    
                // Process message headers.
                if (MessageStatus == MessageState.ReceivingHeader) {
                    if (_messageBuilder.TheMessage.MessageHeader == null)
                        _messageBuilder.TheMessage.MessageHeader = new byte[0];

                    if ( (amount + _messageBuilder.TheMessage.HeaderLength) <= _headerLength)
                        _messageBuilder.AppendHeaderBytes(readBytes);
                    else {
                        var temp = readBytes.Take(_headerLength - _messageBuilder.TheMessage.MessageHeader.Length).ToArray();
                        _messageBuilder.AppendHeaderBytes(temp);
                        amount -= temp.Length;
                    }

                    if (_messageBuilder.TheMessage.MessageHeader.Length > _headerLength) {
                        _logger.Log($"Expected {_headerLength} header bytes but read {_messageBuilder.TheMessage.MessageHeader.Length}... skipping message.", LogLevel.Warning);
                        MessageStatus = MessageState.Skipping;
                    }

                    if (MessageStatus != MessageState.Skipping && _messageBuilder.TheMessage.MessageHeader.Length == _headerLength) {
                        _messageBuilder.BuildMessageHeader();
                        _contentLength = _messageBuilder.TheMessage.ContentLength;
                        MessageStatus = MessageState.ReceivingContent;
                    }
                }

                if (amount > 0 && MessageStatus == MessageState.ReceivingContent) {
                    if (_messageBuilder.TheMessage.Content == null)
                        _messageBuilder.TheMessage.Content = new byte[0];

                    if ( (amount + _messageBuilder.TheMessage.Content.Length) <= _contentLength)
                        _messageBuilder.AppendContentBytes(readBytes);
                    else {
                        var temp = readBytes.Take((int) (_contentLength - _messageBuilder.TheMessage.Content.Length)).ToArray();
                        _messageBuilder.AppendContentBytes(temp);
                        amount -= temp.Length;
                        if (amount > 0)
                            _extraReceived = readBytes.Skip(temp.Length).Take(readBytes.Length - temp.Length).ToArray();
                            MessageStatus = MessageState.Completed;
                    }
                }

                _buffer = new byte[BufferSize];
                return MessageStatus;                
            } catch (Exception ex) {
                _logger?.Log("Something went wrong trying to receive data.", ex, LogLevel.Error);
                throw new Exception(ex.ToString());
            }
        }

        internal bool SkipMessageReadUntilDelimiter(byte[] readbytes, out byte[] extra) {         
            extra = new byte[0];

            for (var i = 0 ; i < readbytes.Length; i++) {
                byte[] temp = new byte[0];
                temp[0] = readbytes[i];
                MessageHelper.MergeByteArrays(_received, temp);

                // Check if packet delimiter was found
                if (_received.Length >= 4) {
                    var end = _received.Skip(_received.Length - 4).Take(4).ToArray();
                    if (end.SequenceEqual(MessageHelper.PacketDelimiter)) {
                        _logger?.Log("Packet delimiter found.", LogLevel.Debug);
                        if (i < readbytes.Length) {
                            extra = new byte[readbytes.Length - i];
                            Array.Copy(readbytes, i, extra, 0, extra.Length);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        internal void PacketReceived() {

        }

    }

}