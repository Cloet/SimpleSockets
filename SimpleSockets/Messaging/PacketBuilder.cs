using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Helpers.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public class PacketBuilder
	{

		private Packet _packet;

		protected IDictionary<object, object> _internalInfo;

		private bool _hasdata = false;

		/// <summary>
		/// Creates a new Packet.
		/// </summary>
		public static PacketBuilder NewPacket => new PacketBuilder();

		private PacketBuilder() {
			_packet = new Packet(null);
			_packet.addDefaultCompression = true;
			_packet.addDefaultEncryption = true;
		}

		/// <summary>
		/// Sets the <see cref="PacketType"/> of the message.
		/// This way a message can easily be identified by the receiving socket.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal PacketBuilder SetPacketType(PacketType type) {
			_packet.MessageType = type;
			return this;
		}

		/// <summary>
		/// Set Data bytes of the <see cref="Packet"/>
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		internal PacketBuilder SetBytes(byte[] data) {
			_hasdata = true;
			_packet.Data = data;
			return this;
		}

		/// <summary>
		/// Set part numbers
		/// </summary>
		/// <param name="part"></param>
		/// <param name="total"></param>
		/// <returns></returns>
		internal PacketBuilder SetPartNumber(int part, int total) {
			bool keyexists = false;
			bool keyexists2 = false;

			if (_internalInfo == null)
				_internalInfo = new Dictionary<object, object>();
			else {
				keyexists = _internalInfo.TryGetValue(PacketHelper.PACKETPART, out var val);
				keyexists2 = _internalInfo.TryGetValue(PacketHelper.TOTALPACKET, out var vals);
			}

			if (keyexists)
				_internalInfo.Remove(PacketHelper.PACKETPART);

			if (keyexists2)
				_internalInfo.Remove(PacketHelper.TOTALPACKET);

			_internalInfo.Add(PacketHelper.PACKETPART, part);
			_internalInfo.Add(PacketHelper.TOTALPACKET, total);
			return this;
		}

		/// <summary>
		/// The path where the file will be saved on the receiving socket.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		internal PacketBuilder SetDestinationPath(string path) {
			bool keyexists = false;
			if (_internalInfo == null)
				_internalInfo = new Dictionary<object, object>();
			else
				keyexists = _internalInfo.TryGetValue(PacketHelper.DESTPATH, out var val);

			if (keyexists)
				_internalInfo.Remove(PacketHelper.DESTPATH);

			_internalInfo.Add(PacketHelper.DESTPATH, path);
			return this;
		}

		/// <summary>
		/// Sets the object type of the packet.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal PacketBuilder SetObjectType(Type type) {
			bool keyexists = false;
			if (_internalInfo == null)
				_internalInfo = new Dictionary<object, object>();
			else
				keyexists = _internalInfo.TryGetValue(PacketHelper.OBJECTTYPE, out var val);

			if (keyexists)
				_internalInfo.Remove(PacketHelper.OBJECTTYPE);

			_internalInfo.Add(PacketHelper.OBJECTTYPE, type);
			return this;
		}

		/// <summary>
		/// Adds a dynamic callback to the message.
		/// When a socket receives a message with a request to trigger a custom event it will try to find this event based on the key.
		/// if no event is found the default event will be invoked.
		/// </summary>
		/// <param name="callbackKey"></param>
		/// <returns></returns>
		public PacketBuilder SetDynamicCallback(string callbackKey) {
			bool keyexists = false;

			if (string.IsNullOrEmpty(callbackKey))
				return this;

			if (_internalInfo == null)
				_internalInfo = new Dictionary<object, object>();
			else
				keyexists = _internalInfo.TryGetValue(PacketHelper.CALLBACK, out var val);

			if (keyexists)
				_internalInfo.Remove(PacketHelper.CALLBACK);

			_internalInfo.Add(PacketHelper.CALLBACK, callbackKey);

			return this;
		}

		/// <summary>
		/// Sets a message
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public PacketBuilder SetPacketString(string message) {
			_packet.Data = Encoding.UTF8.GetBytes(message);
			_hasdata = true;
			_packet.MessageType = PacketType.Message;
			return this;
		}

		/// <summary>
		/// Sets bytes
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public PacketBuilder SetPacketBytes(byte[] data) {
			_packet.Data = data;
			_hasdata = true;
			_packet.MessageType = PacketType.Bytes;
			return this;
		}

		/// <summary>
		/// Sets an object
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public PacketBuilder SetPacketObject(object obj) {
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);

			_hasdata = true;
			SetObjectType(obj.GetType());

			_packet.MessageType = PacketType.Object;
			_packet.Data = bytes;
			return this;
		}

		/// <summary>
		/// Sets the metadata of a packet
		/// </summary>
		/// <param name="metadata"></param>
		/// <returns></returns>
		public PacketBuilder SetMetadata(IDictionary<object, object> metadata) {
			_packet.MessageMetadata = metadata;
			if (metadata != null) {
				_hasdata = true;
			}
			return this;
		}

		/// <summary>
		/// Define what compression should be used.
		/// If none is defined the default compression of the socket will be used.
		/// </summary>
		/// <param name="compression"></param>
		/// <returns></returns>
		public PacketBuilder SetCompression(CompressionMethod compression) {
			_packet.Compress = (compression != CompressionMethod.None);
			_packet.CompressMode = compression;
			_packet.addDefaultCompression = false;
			return this;
		}

		/// <summary>
		/// Define what encryption should be used.
		/// If none is defined the default encryption of the socket will be used.
		/// If encryption is used both the client and server should be assigned the same EncryptionPassphrase.
		/// </summary>
		/// <param name="encryption"></param>
		/// <returns></returns>
		public PacketBuilder SetEncryption(EncryptionMethod encryption) {
			_packet.Encrypt = (encryption != EncryptionMethod.None);
			_packet.EncryptMode = encryption;
			_packet.addDefaultEncryption = false;
			return this;
		}

		/// <summary>
		/// A packet requires to at least have called <see cref="SetPacketString(string)"/>, <see cref="SetPacketBytes(byte[])"/> or
		/// <see cref="SetPacketObject(object)"/> if none of these methods have been called an <see cref="InvalidOperationException"/> will be thrown.
		/// </summary>
		/// <returns></returns>
		public Packet Build() {
			if (!_hasdata)
				throw new InvalidOperationException("Packet has no data.");
			_packet.AdditionalInternalInfo = _internalInfo;
			return _packet;
		}



	}
}
