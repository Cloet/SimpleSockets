using System;
using System.Collections.Generic;
using System.Text;
using SimpleSockets.Messaging;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Serialization;

namespace SimpleSockets {

    internal class FluentMessageBuilder {

        private SimpleMessage _theMessage = null;
        private readonly LogHelper _logger;

        internal static FluentMessageBuilder Initialize(MessageType type, LogHelper logger) => new FluentMessageBuilder(type, logger);

        internal static FluentMessageBuilder InitializeReceiver(LogHelper logger, byte headerField, out int headerLength) => new FluentMessageBuilder(logger, headerField, out headerLength);

        internal SimpleMessage TheMessage => _theMessage;

        private FluentMessageBuilder(MessageType type, LogHelper logger) {
            _theMessage = new SimpleMessage(type, logger);
            _logger = logger;
        }

        private FluentMessageBuilder(LogHelper logger, byte headerField, out int headerLength) {
            _theMessage = new SimpleMessage(logger);
            _theMessage.DeconstructHeaderField(headerField);
            headerLength = _theMessage.HeaderLength;
            _logger = logger;
        }

        internal FluentMessageBuilder AddEncryption(byte[] encryptionKey, EncryptionType mode) {
            if (mode != EncryptionType.None) {
                if (encryptionKey == null) {
                    _logger?.Log("Cannot encrypt a message when no encryptionpassphrase is set.\nNo encryption will be used for this message.", LogLevel.Error);
                    return this;
                }
                _theMessage.EncryptionKey = encryptionKey;
                _theMessage.Encrypt = true;
            }
            return this;
        }

        internal FluentMessageBuilder AddCompression(CompressionType mode) {
            if (mode != CompressionType.None)
                _theMessage.Compress = true;
            return this;
        }

        internal FluentMessageBuilder AddMetadata(IDictionary<object,object> metadata) {
            if (metadata != null)
                _theMessage.MessageMetadata = SerializationHelper.SerializeObjectToBytes(metadata);
            return this;
        }

        internal FluentMessageBuilder AddMessageString(string data) {
            _theMessage.Data = Encoding.UTF8.GetBytes(data);
            return this;
        }

        internal FluentMessageBuilder AddAdditionalInternalInfo(IDictionary<object, object> info) {
            if (info != null)
                _theMessage.AdditionalInternalInfo = SerializationHelper.SerializeObjectToBytes(info);
            return this;
        }

        internal FluentMessageBuilder AddMessageBytes(byte[] data) {
            _theMessage.Data = data;
            return this;
        }

        internal FluentMessageBuilder AppendMessageBytes(byte[] data) {
            _theMessage.Data = MessageHelper.MergeByteArrays(_theMessage.Data, data);
            return this;
        }

        internal FluentMessageBuilder AppendAdditionalInternalInfoBytes(byte[] data) {
            _theMessage.AdditionalInternalInfo = MessageHelper.MergeByteArrays(_theMessage.AdditionalInternalInfo, data);
            return this;
        }

        internal FluentMessageBuilder AppendMetadataBytes(byte[] data) {
            _theMessage.MessageMetadata = MessageHelper.MergeByteArrays(_theMessage.MessageMetadata, data);
            return this;
        }

        internal FluentMessageBuilder AppendHeaderBytes(byte[] data) {
            _theMessage.MessageHeader = MessageHelper.MergeByteArrays(_theMessage.MessageHeader, data);
            return this;
        }

        /// <summary>
        /// Builds a message with the set parameters.
        /// </summary>
        /// <returns>Payload</returns>
        internal byte[] BuildMessage() {
            try {
                return _theMessage.BuildPayload();
            } catch (Exception ex) {
                _logger?.Log("Something went wrong building the message payload.", LogLevel.Error);
                _logger?.Log("Exception: " + ex.ToString(), LogLevel.Error);
                return null;
            }
        }

        internal void BuildMessageHeader() {
            _theMessage.DeconstructHeaders();
        }

        internal SimpleMessage BuildFromReceivedPackets() {
            return _theMessage;
        }




    }

}