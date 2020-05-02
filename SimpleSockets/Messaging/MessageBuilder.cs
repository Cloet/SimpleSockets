using System;
using System.Collections.Generic;
using System.Text;
using SimpleSockets.Messaging;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Serialization;

namespace SimpleSockets {

    internal class MessageBuilder {

		private readonly LogHelper _logger;

        internal static MessageBuilder Initialize(MessageType type, LogHelper logger) => new MessageBuilder(type, logger);

        internal static MessageBuilder InitializeReceiver(LogHelper logger, byte headerField, out int headerLength) => new MessageBuilder(logger, headerField, out headerLength);

		internal SimpleMessage TheMessage { get; private set; } = null;

		private MessageBuilder(MessageType type, LogHelper logger) {
            TheMessage = new SimpleMessage(type, logger);
            _logger = logger;
        }

        private MessageBuilder(LogHelper logger, byte headerField, out int headerLength) {
            TheMessage = new SimpleMessage(logger);
            TheMessage.DeconstructHeaderField(headerField);
            headerLength = TheMessage.HeaderLength;
            _logger = logger;
        }

		internal MessageBuilder AddPreSharedKey(byte[] preSharedKey) {
			TheMessage.PreSharedKey = preSharedKey;
			return this;
		}

        internal MessageBuilder AddEncryption(byte[] encryptionKey, EncryptionType mode) {
			if (mode != EncryptionType.None)
			{
				if (encryptionKey == null)
				{
					TheMessage.Encrypt = false;
					_logger?.Log("Cannot encrypt a message when no encryptionpassphrase is set.\nNo encryption will be used for this message.", LogLevel.Error);
					return this;
				}
				TheMessage.EncryptionKey = encryptionKey;
				TheMessage.Encrypt = true;
			}
			else
				TheMessage.Encrypt = false;

			TheMessage.EncryptMode = mode;
            return this;
        }

        internal MessageBuilder AddCompression(CompressionType mode) {
			if (mode != CompressionType.None)
				TheMessage.Compress = true;
			else
				TheMessage.Compress = false;

			TheMessage.CompressMode = mode;
            return this;
        }

		internal MessageBuilder AddPassphrase(byte[] encryptionPassphrase) {
			TheMessage.EncryptionKey = encryptionPassphrase;
			return this;
		}

		internal MessageBuilder AddMetadata(IDictionary<object,object> metadata) {
            if (metadata != null)
                TheMessage.MessageMetadata = SerializationHelper.SerializeObjectToBytes(metadata);
            return this;
        }

        internal MessageBuilder AddMessageString(string data) {
            TheMessage.Data = Encoding.UTF8.GetBytes(data);
            return this;
        }

        internal MessageBuilder AddAdditionalInternalInfo(IDictionary<object, object> info) {
            if (info != null)
                TheMessage.AdditionalInternalInfo = SerializationHelper.SerializeObjectToBytes(info);
            return this;
        }

        internal MessageBuilder AddMessageBytes(byte[] data) {
            TheMessage.Data = data;
            return this;
        }

        internal MessageBuilder AppendContentBytes(byte[] data) {
            TheMessage.Content = MessageHelper.MergeByteArrays(TheMessage.Content, data);
            return this;
        }

        internal MessageBuilder AppendHeaderBytes(byte[] data) {
            TheMessage.MessageHeader = MessageHelper.MergeByteArrays(TheMessage.MessageHeader, data);
            return this;
        }

        /// <summary>
        /// Builds a message with the set parameters.
        /// </summary>
        /// <returns>Payload</returns>
        internal byte[] BuildMessage() {
            try {
                return TheMessage.BuildPayload();
            } catch (Exception ex) {
                _logger?.Log("Something went wrong building the message payload.", LogLevel.Error);
                _logger?.Log("Exception: " + ex.ToString(), LogLevel.Error);
                return null;
            }
        }

        internal SimpleMessage BuildFromReceivedPackets(byte[] presharedKey) {
			TheMessage.DeconstructHeaders();
			TheMessage.BuildMessageFromContent(presharedKey);
            return TheMessage;
        }




    }

}