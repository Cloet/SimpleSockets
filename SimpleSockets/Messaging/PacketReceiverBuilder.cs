using System;
using System.Collections.Generic;
using System.Text;
using SimpleSockets.Messaging;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Serialization;

namespace SimpleSockets {

	/// <summary>
	/// Used to build a <see cref="Packet"/>
	/// </summary>
    internal class PacketReceiverBuilder {

		// The logger has been handed down from a socket.
		private readonly LogHelper _logger;

		// The packet stored in the builder
		internal Packet ThePacket { get; private set; } = null;

		/// <summary>
		/// Initializes a <see cref="Packet"/> object.
		/// This <see cref="Packet"/> object is used to receive bytes and convert to usable data.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="headerField"></param>
		/// <param name="headerLength"></param>
		/// <returns></returns>
        internal static PacketReceiverBuilder InitializeReceiver(LogHelper logger, byte headerField, out int headerLength) => new PacketReceiverBuilder(logger, headerField, out headerLength);

		//private constructor for a receiving packet.
        private PacketReceiverBuilder(LogHelper logger, byte headerField, out int headerLength) {
            ThePacket = new Packet(logger);
            ThePacket.DeconstructHeaderField(headerField);
            headerLength = ThePacket.HeaderLength;
            _logger = logger;
        }

		//Start Builder methods.

		// These methods are only used when receiving bytes and building a packet.
		#region Packet-Receiver-Methods

		//Adds an encryption passphrase to the packet.
		internal PacketReceiverBuilder AddPassphrase(byte[] encryptionPassphrase)
		{
			ThePacket.EncryptionKey = encryptionPassphrase;
			return this;
		}

		//Sets the received bytes from the other socket.
        internal PacketReceiverBuilder AppendContentBytes(byte[] data) {
            ThePacket.Content = PacketHelper.MergeByteArrays(ThePacket.Content, data);
            return this;
        }

		//Sets the header bytes of a message.
        internal PacketReceiverBuilder AppendHeaderBytes(byte[] data) {
            ThePacket.MessageHeader = PacketHelper.MergeByteArrays(ThePacket.MessageHeader, data);
			return this;
        }
		#endregion

		/// <summary>
		/// Builds a message from received bytes.
		/// </summary>
		/// <param name="presharedKey"></param>
		/// <returns></returns>
        internal Packet Build(byte[] presharedKey) {
			ThePacket.DeconstructHeaders();
			ThePacket.BuildMessageFromContent(presharedKey);
            return ThePacket;
        }


	}

}