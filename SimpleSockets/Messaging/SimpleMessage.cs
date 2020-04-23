using System;
using System.Collections;
using System.Text;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;

namespace SimpleSockets.Messaging {

    internal class SimpleMessage {

        internal MessageType MessageType { get; set; }

        internal byte[] MessageMetadata { get; set;}

        internal byte[] Data { get; set; }

        internal byte[] PreSharedKey { get; set; }

        internal byte[] AdditionalInternalInfo { get; set; }

        internal bool Compress { get; set;}

        internal bool Encrypt { get; set; }

        internal byte[] EncryptionKey { get; set; }

        internal CompressionType CompressMode { get; set; }

        internal EncryptionType EncryptMode { get; set; }

        private readonly LogHelper _logger;

        /// <summary>
        /// 0 - Metadata
        /// 1 - Encrypted
        /// 2 - Compressed
        /// 3 - PresharedKey (First 16 Bytes after HeaderField)
        /// 4 - AdditionalInternalInfo
        /// </summary>
        /// <value></value>
        internal BitArray HeaderFields { get; set;}


        internal SimpleMessage(MessageType type, LogHelper logger = null) {
            MessageMetadata = new byte[0];
            PreSharedKey = new byte[0];
            Data = new byte[0];
            Encrypt = false;
            Compress = false;
            _logger = logger;

            HeaderFields = new BitArray(8, false);
        }
        
        /// <summary>
        /// Builds the packet header
        /// </summary>
        /// <returns></returns>
        private byte[] BuildMessageHeader(int originalContentLength, int contentLength) {
            // First 5 Bytes are always
            // 0     -> HeaderFields  (1 Byte)
            // 1 - 5 -> Original ContentLength (4 Bytes) (No compression / Encryption)
            var headerLength = 6;
            int index = 0;
            
            //MessageMetadata ContentLength (4 Bytes)
            if (HeaderFields[0]) 
                headerLength += 4;

            byte[] headerFieldBytes = new byte[headerLength];

            // Write flags
            HeaderFields.CopyTo(headerFieldBytes, index);
            index += 1;

            // Writes messagetype 1 byte (256 possibilities)
            headerFieldBytes[index] = (byte) MessageType;

            // Add Presharedkey [16 Bytes]
            if (HeaderFields[3]) {
                PreSharedKey.CopyTo(headerFieldBytes, index);
                index += 16;
            }

            // Write contentlength
            BitConverter.GetBytes(originalContentLength).CopyTo(headerFieldBytes, index);
            index += 4;

            // Length of data in encrypted/compressed form.
            if (HeaderFields[2] || HeaderFields[1]) {
                BitConverter.GetBytes(contentLength).CopyTo(headerFieldBytes, index);
                index += 4;
            }


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

    }

}