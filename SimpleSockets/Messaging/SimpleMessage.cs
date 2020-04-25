using System;
using System.Collections;
using System.Text;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using System.Linq;

namespace SimpleSockets.Messaging {

    internal class SimpleMessage {

        internal MessageType MessageType { get; set; }

        internal byte[] MessageMetadata { get; set;}

        internal byte[] Data { get; set; }

        internal byte[] PreSharedKey { get; set; }

        internal byte[] AdditionalInternalInfo { get; set; }

        internal byte[] MessageHeader { get; set; }

        internal byte[] Content { get; set; }

        internal bool Compress { get; set;}

        internal bool Encrypt { get; set; }

        internal byte[] EncryptionKey { get; set; }

        internal CompressionType CompressMode { get; set; }

        internal EncryptionType EncryptMode { get; set; }

        internal int HeaderLength => _headerLength;

        private LogHelper _logger;

        private int _headerLength = 0;

        internal long OriginalcontentLength { get; set; }

        internal long ContentLength { get; set; }

        /// <summary>
        /// 0 - Metadata
        /// 1 - Encrypted
        /// 2 - Compressed
        /// 3 - PresharedKey (First 16 Bytes after HeaderField)
        /// 4 - AdditionalInternalInfo
        /// </summary>
        /// <value></value>
        internal BitArray HeaderFields { get; set;}

        internal SimpleMessage(LogHelper logger) {
            Initialize(logger);
        }

        internal SimpleMessage(MessageType type, LogHelper logger = null) {
            MessageType = type;
            Initialize(logger);
        }

        private void Initialize(LogHelper logger = null) {
            MessageMetadata = new byte[0];
            PreSharedKey = new byte[0];
            Data = new byte[0];
            Encrypt = false;
            Compress = false;
            _logger = logger;
            ContentLength = 0;
            OriginalcontentLength = 0;

            HeaderFields = new BitArray(8, false);
        }
        
        #region Build Payload from set data

        /// <summary>
        /// Builds the packet header
        /// </summary>
        /// <returns></returns>
        private byte[] BuildMessageHeader(int originalContentLength, int contentLength) {
            // First Bytes are always
            // HeaderFields          (1  Byte )
            // MessageType           (1  Byte )
            // PreSharedKey          (16 Bytes) (Only when a presharedkey is given)
            // OriginalContentLength (8  Bytes) 
            // ContentLength         (8  Bytes) (Only when compressed or encrypted)
            
            byte[] headerFieldBytes = new byte[1];

            // Write flags (HEADERFIELDS) 1 Byte
            HeaderFields.CopyTo(headerFieldBytes, 0);

            // Writes messagetype 1 byte (256 possibilities)
            headerFieldBytes[1] = (byte) MessageType;

            // Add Presharedkey [16 Bytes]
            if (HeaderFields[3])
                headerFieldBytes = MessageHelper.MergeByteArrays(headerFieldBytes, PreSharedKey);

            // Write contentlength (8 Bytes)
            headerFieldBytes = MessageHelper.MergeByteArrays(headerFieldBytes, BitConverter.GetBytes(originalContentLength));

            // Length of data in encrypted/compressed form. (8 Bytes)
            if (HeaderFields[2] || HeaderFields[1])
                headerFieldBytes = MessageHelper.MergeByteArrays(headerFieldBytes, BitConverter.GetBytes(contentLength));


            return null;
        }
        
        /// <summary>
        /// Content is converted from bytes to hex and written in netstring format.
        /// https://en.wikipedia.org/wiki/Netstring
        /// </summary>
        /// <returns>Content Byte array</returns>
        private byte[] BuildContent() {
            string content = String.Empty;
            
            var converted = MessageHelper.ByteArrayToString(Data);
            content = $"{converted.Length}:{converted},";

            if (HeaderFields[0]) {
                converted = MessageHelper.ByteArrayToString(MessageMetadata);
                content += $"{converted.Length}:{converted},";
            }

            if (HeaderFields[4]) {
                converted = MessageHelper.ByteArrayToString(AdditionalInternalInfo);
                content += $"{converted.Length}:{converted},";
            }
         
            return Encoding.UTF8.GetBytes(content);
        }

        internal byte[] BuildPayload() {
            
            _logger.Log("===========================================", LogLevel.Debug);
            _logger.Log("Building Message.",LogLevel.Debug);
            // Content contains
            // 1 - Data
            // 2 - MessageMetadata
            // 3 - AdditionalInternalInfo
            var content = BuildContent();
            var originalContentLength = content.Length;
            
            // Compress the content if so desired.
            if (Compress && CompressMode != CompressionType.None) {
                _logger?.Log("Compressing content...", LogLevel.Debug);
                content = CompressionHelper.Compress(content, CompressMode);
            }

            // Encrypt the content after potential compression if so desired.
            if (Encrypt && EncryptMode != EncryptionType.None) {
                _logger?.Log("Encrypting content...", LogLevel.Debug);
                content = CryptographyHelper.Encrypt(Data, EncryptionKey, EncryptMode);
            }

            byte[] head = BuildMessageHeader(originalContentLength, content.Length);
            byte[] tail = MessageHelper.PacketDelimiter;
            byte[] Payload = new byte[head.Length + content.Length + tail.Length];

            _logger?.Log("Messageheader is " + head.Length + " bytes long.", LogLevel.Debug);
            _logger?.Log("Message content is " + content.Length + " bytes long.", LogLevel.Debug);

            head.CopyTo(Payload,0);
            content.CopyTo(Payload,head.Length);
            tail.CopyTo(Payload,head.Length + content.Length);

            _logger?.Log("The message has been built.", LogLevel.Debug);
            _logger?.Log("===========================================", LogLevel.Debug);
            return Payload;
        }

        #endregion

        #region Set data from received bytes

        internal void DeconstructHeaderField(byte headerFieldByte) {
            HeaderFields = new BitArray(headerFieldByte);
            _headerLength = 10;

            // Extra length if using presharedkey
            if (HeaderFields[3])
                _headerLength += 16;

            // Extra length is encrypted / compressed
            if (HeaderFields[2] || HeaderFields[1])
                _headerLength += 8;
        }

        internal void DeconstructHeaders() {
            var header = MessageHeader;

            // Get the messagetype of the message
            MessageType = (MessageType)header[0];
            header = header.Skip(1).Take(header.Length -1).ToArray();
            
            // PresharedKey
            if (HeaderFields[3]) {
                Array.Copy(header, 0, PreSharedKey, 0, 16);
                header = header.Skip(PreSharedKey.Length).Take(header.Length - PreSharedKey.Length).ToArray();
            }

            OriginalcontentLength = BitConverter.ToInt64(header,0);

            if (HeaderFields[2] || HeaderFields[1])
                ContentLength = BitConverter.ToInt64(header, 8);
            else
                ContentLength = OriginalcontentLength;

            
        }

        #endregion

    }

}