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

		public static PacketBuilder NewPacket => new PacketBuilder();

		private PacketBuilder() {
			_packet = new Packet(null);
			_packet.addDefaultCompression = true;
			_packet.addDefaultEncryption = true;
		}

		internal PacketBuilder SetPacketType(PacketType type) {
			_packet.MessageType = type;
			return this;
		}

		internal PacketBuilder SetBytes(byte[] data) {
			_hasdata = true;
			_packet.Data = data;
			return this;
		}

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

		public PacketBuilder SetPacketString(string message) {
			_packet.Data = Encoding.UTF8.GetBytes(message);
			_hasdata = true;
			_packet.MessageType = PacketType.Message;
			return this;
		}

		public PacketBuilder SetPacketBytes(byte[] data) {
			_packet.Data = data;
			_hasdata = true;
			_packet.MessageType = PacketType.Bytes;
			return this;
		}

		public PacketBuilder SetPacketObject(object obj) {
			bool keyexists = false;
			var bytes = SerializationHelper.SerializeObjectToBytes(obj);

			_hasdata = true;

			if (_internalInfo == null)
				_internalInfo = new Dictionary<object, object>();
			else
				keyexists = _internalInfo.TryGetValue(PacketHelper.OBJECTTYPE, out var val);

			if (keyexists)
				_internalInfo.Remove(PacketHelper.OBJECTTYPE);

			_internalInfo.Add(PacketHelper.OBJECTTYPE, obj.GetType());

			_packet.MessageType = PacketType.Object;
			_packet.Data = bytes;
			return this;
		}

		public PacketBuilder SetMetadata(IDictionary<object, object> metadata) {
			_packet.MessageMetadata = metadata;
			if (metadata != null) {
				_hasdata = true;
			}
			return this;
		}

		public PacketBuilder SetCompression(CompressionType compression) {
			_packet.Compress = (compression != CompressionType.None);
			_packet.CompressMode = compression;
			_packet.addDefaultCompression = false;
			return this;
		}

		public PacketBuilder SetEncryption(EncryptionType encryption) {
			_packet.Encrypt = (encryption != EncryptionType.None);
			_packet.EncryptMode = encryption;
			_packet.addDefaultEncryption = false;
			return this;
		}

		public Packet Build() {
			if (!_hasdata)
				throw new InvalidOperationException("Packet has no data.");
			_packet.AdditionalInternalInfo =_internalInfo;
			return _packet;
		}



	}
}
