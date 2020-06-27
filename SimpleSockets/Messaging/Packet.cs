using System;
using System.Collections;
using System.Text;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using System.Linq;
using System.Collections.Generic;
using SimpleSockets.Helpers.Serialization;
using SimpleSockets.Client;
using SimpleSockets.Server;
using System.IO;

namespace SimpleSockets.Messaging {

    public class Packet {

		/// <summary>
		/// Indicates what sort of messagetype this message is
		/// </summary>
        public PacketType MessageType { get; internal set; }

		/// <summary>
		/// Presharedkey of the message
		/// </summary>
		public byte[] PreSharedKey
		{
			get => _preSharedKey;
			internal set
			{

				if (value == null || value.Length == 0)
					HeaderFields[3] = false;
				else
					HeaderFields[3] = true;

				_preSharedKey = value;
			}
		}

		/// <summary>
		/// The metadata of the message
		/// </summary>
		public IDictionary<object,object> MessageMetadata {
			get => _metadata;
			internal set {
				if (value == null || value.Values.Count == 0)
					HeaderFields[0] = false;
				else
					HeaderFields[0] = true;

				_metadata = value;
			}
		}

		/// <summary>
		/// Extra information of a messages, only used within library.
		/// </summary>
        internal IDictionary<object,object> AdditionalInternalInfo {
			get => _internalInfo;
			set
			{
				if (value == null || value.Values.Count == 0)
					HeaderFields[4] = false;
				else
					HeaderFields[4] = true;

				_internalInfo = value;
			}
		}

		/// <summary>
		/// Header bytes of a message
		/// </summary>
        internal byte[] MessageHeader { get; set; }

		/// <summary>
		/// The data of a message
		/// </summary>
		public byte[] Data { get; internal set; }

		/// <summary>
		/// When receiving a message all content will be stored in this variable.
		/// When all bytes are received this will be deconstructed.
		/// </summary>
        internal byte[] Content { get; set; }

		/// <summary>
		/// Indicates if the message is encrypted or not.
		/// </summary>
		internal bool Compress {
			get => HeaderFields[2];
			set => HeaderFields[2] = value;
		}

		/// <summary>
		/// Indicates if the message is encrypted or not.
		/// </summary>
        internal bool Encrypt {
			get => HeaderFields[1];
			set => HeaderFields[1] = value;
		}

		/// <summary>
		/// The key used for encryption/decryption
		/// </summary>
        public byte[] EncryptionKey { get; internal set; }

		/// <summary>
		/// If a message is compressed this indicates what compression is used.
		/// </summary>
        public CompressionMethod CompressMode { get; internal set; }

		/// <summary>
		/// If a message is encrypted this indicates what encryption is used.
		/// </summary>
        public EncryptionMethod EncryptMode { get; internal set; }

		/// <summary>
		/// Length of the header
		/// </summary>
        internal int HeaderLength { get; private set; }

		private IDictionary<object,object> _internalInfo;

		private IDictionary<object,object> _metadata;

		private byte[] _internalInfoBytes;

		private byte[] _metadataBytes;

		private byte[] _preSharedKey;

        internal LogHelper Logger;

        internal long OriginalcontentLength { get; set; }

        internal long ContentLength { get; set; }

		internal bool addDefaultEncryption;

		internal bool addDefaultCompression;

        /// <summary>
        /// 0 - Metadata
        /// 1 - Encrypted
        /// 2 - Compressed
        /// 3 - PresharedKey (First 16 Bytes after HeaderField)
        /// 4 - AdditionalInternalInfo
        /// </summary>
        /// <value></value>
        internal BitArray HeaderFields { get; set;}

		//Construct of receiver
        internal Packet(LogHelper logger) {
			HeaderFields = new BitArray(8, false);
			_metadataBytes = new byte[0];
			_internalInfoBytes = new byte[0];
			PreSharedKey = new byte[0];
			Content = new byte[0];
			Data = new byte[0];
			Encrypt = false;
			Compress = false;
			Logger = logger;
			ContentLength = 0;
			OriginalcontentLength = 0;
		}

		//Constructor of a message
        internal Packet(PacketType type, LogHelper logger = null): this(logger) {
            MessageType = type;
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
			// Compression		     (1  Byte )
            // PreSharedKey          (16 Bytes) (Only when a presharedkey is given)
            // OriginalContentLength (8  Bytes) 
            // ContentLength         (8  Bytes) (Only when compressed or encrypted)
            
            byte[] headerFieldBytes = new byte[4];

            // Write flags (HEADERFIELDS) 1 Byte
            HeaderFields.CopyTo(headerFieldBytes, 0);

            // Writes messagetype 1 byte (256 possibilities)
            headerFieldBytes[1] = (byte) MessageType;
			headerFieldBytes[2] = (byte) CompressMode;
			headerFieldBytes[3] = (byte) EncryptMode;

            // Add Presharedkey [16 Bytes]
            if (HeaderFields[3])
                headerFieldBytes = PacketHelper.MergeByteArrays(headerFieldBytes, PreSharedKey);

			// Write contentlength (8 Bytes)
			byte[] bytes = new byte[8];
			BitConverter.GetBytes(originalContentLength).CopyTo(bytes, 0);
            headerFieldBytes = PacketHelper.MergeByteArrays(headerFieldBytes, bytes);

			// Length of data in encrypted/compressed form. (8 Bytes)
			if (HeaderFields[2] || HeaderFields[1]) {
				bytes = new byte[8];
				BitConverter.GetBytes(contentLength).CopyTo(bytes, 0);
				headerFieldBytes = PacketHelper.MergeByteArrays(headerFieldBytes, bytes);
			}

            return headerFieldBytes;
        }

		/// <summary>
		/// Format Length:Content
		/// 1. Data
		/// 2. Metadata
		/// 3. InternalInfo
		/// </summary>
		/// <returns>Content Byte array</returns>
		private byte[] BuildContent() {

			if (_metadataBytes == null)
				_metadataBytes = new byte[0];

			if (_internalInfoBytes == null)
				_internalInfoBytes = new byte[0];

			var length = 20 + Data.Length;

			if (HeaderFields[0])
				length += _metadataBytes.Length;

			if (HeaderFields[4])
				length += _internalInfoBytes.Length;

			var output = new byte[length];

			var len = BitConverter.GetBytes(Data.LongLength);
			long index = 0;

			// Data
			Array.Copy(len, 0, output, index, len.LongLength); // Length
			index += len.LongLength;
			Array.Copy(Data, 0, output, index, Data.LongLength); // Data
			index += Data.LongLength;

			// Metadata
			len = BitConverter.GetBytes(_metadataBytes.LongLength);
			Array.Copy(len, 0, output, index, len.LongLength);
			index += len.LongLength;
			if (HeaderFields[0]) {
				Array.Copy(_metadataBytes, 0, output, index, _metadataBytes.LongLength);
				index += len.LongLength;
			}

			len = BitConverter.GetBytes(_internalInfoBytes.Length);
			Array.Copy(len, 0, output, index, len.Length);
			index += len.Length;
			if (HeaderFields[4]) {
				Array.Copy(_internalInfoBytes, 0, output, index, _internalInfoBytes.Length);
			}

			return output;
        }

		/// <summary>
		/// Builds the payload of a message.
		/// </summary>
		/// <returns>Byte[] array</returns>
        internal virtual byte[] BuildPayload() {
            
            Logger?.Log("Building a packet.", LogLevel.Trace);

			_internalInfoBytes = SerializationHelper.SerializeObjectToBytes(AdditionalInternalInfo);
			_metadataBytes = SerializationHelper.SerializeObjectToBytes(MessageMetadata);

            // Content contains
            // 1 - Data
            // 2 - MessageMetadata
            // 3 - AdditionalInternalInfo
            var content = BuildContent();
            var originalContentLength = content.Length;
            
            // Compress the content if so desired.
            if (Compress && CompressMode != CompressionMethod.None) {
                Logger?.Log("Compressing content...", LogLevel.Trace);
                content = CompressionHelper.Compress(content, CompressMode);
            }

            // Encrypt the content after potential compression if so desired.
            if (Encrypt && EncryptMode != EncryptionMethod.None) {
                Logger?.Log("Encrypting content...", LogLevel.Trace);
                content = CryptographyHelper.Encrypt(Content, EncryptionKey, EncryptMode);
            }

            byte[] head = BuildMessageHeader(originalContentLength, content.Length);
            // byte[] tail = PacketHelper.PacketDelimiter;
            byte[] payload = new byte[head.Length + content.Length];

            head.CopyTo(payload,0);
            content.CopyTo(payload,head.Length);
			payload = PacketHelper.EncodeByteArray(payload);
            // tail.CopyTo(payload,head.Length + content.Length);

            Logger?.Log("Build finished : " + this.ToString(), LogLevel.Trace);
            return payload;
        }

        #endregion

        #region Set data from received bytes

		/// <summary>
		/// Takes the first byte of a messages and deconstruct this bye.
		/// This byte contains various flags.
		/// </summary>
		/// <param name="headerFieldByte"></param>
        internal void DeconstructHeaderField(byte headerFieldByte) {
			byte[] bytes = new byte[1];
			bytes[0] = headerFieldByte;
			HeaderFields = new BitArray(bytes);
            HeaderLength = 11; // First bit isn't counted.

            // Extra length if using presharedkey
            if (HeaderFields[3])
                HeaderLength += 16;

            // Extra length if encrypted / compressed
            if (HeaderFields[2] || HeaderFields[1])
                HeaderLength += 8;
        }

		/// <summary>
		/// Deconstruct the header fields and set lengths of various message parts.
		/// </summary>
        internal void DeconstructHeaders() {
            var header = MessageHeader;

            // Get the messagetype of the message
            MessageType  = (PacketType)	 header[0];
			CompressMode = (CompressionMethod) header[1];
			EncryptMode  = (EncryptionMethod)  header[2];

            header = header.Skip(3).Take(header.Length -1).ToArray();
            
            // PresharedKey
            if (HeaderFields[3]) {
				PreSharedKey = new byte[16];
				Array.Copy(header, 0, PreSharedKey, 0, 16);
                header = header.Skip(PreSharedKey.Length).Take(header.Length - PreSharedKey.Length).ToArray();
            }

            OriginalcontentLength = BitConverter.ToInt64(header,0);

            if (HeaderFields[2] || HeaderFields[1])
                ContentLength = BitConverter.ToInt64(header, 8);
            else
                ContentLength = OriginalcontentLength;
            
        }

		/// <summary>
		/// Builds a message from the received bytes.
		/// </summary>
		/// <param name="preSharedKey"></param>
		internal virtual void BuildMessageFromContent(byte[] preSharedKey) {

			if (preSharedKey != null && preSharedKey.Length > 0 && HeaderFields[3] == false)
				throw new Exception("Expected a presharedkey but none was found.");

			if (HeaderFields[3] && !PreSharedKey.SequenceEqual(PreSharedKey))
				throw new Exception("The presharedkey does not match between messages.");
			
			if (ContentLength != Content.LongLength)
				throw new Exception($"Expected contentlength differs from expected contentlength.{Environment.NewLine}Expected {ContentLength} bytes but received {Content.Length} bytes");

			// Do decryption
			if (HeaderFields[1]) {
				if (EncryptionKey == null || EncryptionKey.Length == 0)
					throw new Exception($"Tried to decrypt message but no encryptionkey was provided.");
				Content = CryptographyHelper.Decrypt(Content, EncryptionKey, EncryptMode);
			}

			// Do Decompression
			if (HeaderFields[2])
				Content = CompressionHelper.Decompress(Content, CompressMode);

			if (OriginalcontentLength != Content.LongLength)
				throw new Exception($"Expected contentlength differs from received contentlength after decryption/decompression.{Environment.NewLine}Expected {OriginalcontentLength} bytes but received {Content.Length} bytes.");


			long datalength = BitConverter.ToInt64(Content,0);
			long index = 8;

			Data = new byte[datalength];
			if (datalength > 0) {
				Array.Copy(Content, index, Data, 0, Data.Length);
				index += Data.LongLength;
			}

			datalength = BitConverter.ToInt64(Content, (int)index);
			_metadataBytes = new byte[datalength];
			index += 8;
			if (datalength > 0) {
				Array.Copy(Content, index, _metadataBytes, 0, _metadataBytes.LongLength);
				index += _metadataBytes.LongLength;
			}

			datalength = BitConverter.ToInt32(Content, (int)index);
			index += 4;
			_internalInfoBytes = new byte[datalength];
			if (datalength > 0) {
				Array.Copy(Content, index, _internalInfoBytes, 0, _internalInfoBytes.LongLength);
			}

			BuildMetadataFromBytes();
			BuildInternalInfoFromBytes();
		}

		/// <summary>
		/// Build metadata from the stored bytes.
		/// </summary>
		/// <returns></returns>
		private void BuildMetadataFromBytes() {
			try
			{
				if (_metadataBytes == null || _metadataBytes.Length == 0) {
					MessageMetadata = null;
					return;
				}

				MessageMetadata = SerializationHelper.DeserializeJson<IDictionary<object, object>>(_metadataBytes);
			}
			catch (Exception ex) {
				Logger?.Log("Failed to retrieve metadata from bytes.", ex, LogLevel.Warning);
				MessageMetadata = null;
			}
		}

		/// <summary>
		/// Builds the internal info from stored bytes.
		/// </summary>
		/// <returns></returns>
		private void BuildInternalInfoFromBytes()
		{
			try
			{
				if (_internalInfoBytes == null || _internalInfoBytes.Length == 0) {
					AdditionalInternalInfo = null;
					return;
				}

				AdditionalInternalInfo = SerializationHelper.DeserializeJson<IDictionary<object, object>>(_internalInfoBytes);
			}
			catch (Exception)
			{
				AdditionalInternalInfo = null;
				return;
			}
		}

		/// <summary>
		/// Get dynamic eventargs
		/// </summary>
		/// <param name="info"></param>
		/// <param name="events"></param>
		/// <returns></returns>
		internal EventHandler<DataReceivedEventArgs> GetDynamicCallbackClient(IDictionary<object, object> info, 
			IDictionary<string,EventHandler<DataReceivedEventArgs>> events) {
			try
			{
				if (info == null)
					return null;

				var exists = info.TryGetValue(PacketHelper.CALLBACK, out var output);

				if (!exists)
					return null;

				var exists2 = events.TryGetValue(output.ToString(), out var eventH);

				if (!exists2)
					return null;

				return eventH;
			}
			catch (Exception) {
				return null;
			}
		}

		/// <summary>
		/// Get guid from a packet
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		internal Guid GetGuidFromMessage(IDictionary<object, object> info) {
			try
			{
				if (info == null)
					return Guid.Empty;

				var exists = info.TryGetValue(PacketHelper.GUID, out var guid);

				if (!exists)
					return Guid.Empty;

				return (Guid)guid;
			}
			catch (Exception ex) {
				Logger?.Log("Unable to retrieve the guid.", ex, LogLevel.Warning);
				return Guid.Empty;
			}
		}

		/// <summary>
		/// Custom eventargs for server
		/// </summary>
		/// <param name="info"></param>
		/// <param name="events"></param>
		/// <returns></returns>
		internal EventHandler<ClientDataReceivedEventArgs> GetDynamicCallbackServer(IDictionary<object, object> info, IDictionary<string,EventHandler<ClientDataReceivedEventArgs>> events) {
			try
			{
				if (info == null)
					return null;

				var exists = info.TryGetValue(PacketHelper.CALLBACK, out var output);

				if (!exists)
					return null;

				var exists2 = events.TryGetValue(output.ToString(), out var eventH);

				if (!exists2)
					return null;

				return eventH;
			}
			catch (Exception ex) {
				Logger?.Log("Unable to retrieve a dynamic callback.", ex, LogLevel.Warning);
				return null;
			}
		}

		/// <summary>
		/// Build from received bytes
		/// </summary>
		/// <param name="info"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		internal object BuildObjectFromBytes(IDictionary<object,object> info, out Type type) {

			type = null;

			try
			{
				if (info == null)
					return null;

				var exists = info.TryGetValue(PacketHelper.OBJECTTYPE, out var output);

				if (!exists)
					return null;

				type = Type.GetType(output.ToString());

				return SerializationHelper.DeserializeBytesToObject(Data, type);
			}
			catch (Exception ex) {
				Logger?.Log("Failed building object from bytes.", ex, LogLevel.Warning);
				return null;
			}
		}

		/// <summary>
		/// Builds the byte data to a string.
		/// </summary>
		/// <returns></returns>
		internal string BuildDataToString() {
			try
			{
				if (Data == null)
					return string.Empty;

				return Encoding.UTF8.GetString(Data);
			}
			catch (Exception ex) {
				Logger?.Log("Failed converting byte[] array to string.", ex, LogLevel.Warning);
				return null;
			}
		}


		#endregion

		public override string ToString() {
			var stats = $"Packet has {Data.Length} bytes, {(MessageMetadata == null ? 0 : MessageMetadata?.Values.Count)} pieces of metadata.";
			stats += $" uses {(EncryptionMethod.None == EncryptMode ? "no" : Enum.GetName(typeof(EncryptionMethod), EncryptMode))} encryption";
			stats += $" and {(CompressionMethod.None == CompressMode ? "no" : Enum.GetName(typeof(CompressionMethod), CompressMode))} compression.";
			return stats;
		}

		public string PrintInfo()
		{
			var stats = "=======================================================================" + Environment.NewLine;
			stats += "|  Packet                                                             |" + Environment.NewLine;
			stats += "|---------------------------------------------------------------------|" + Environment.NewLine;
			stats += "| - Data           : " + $"{Data.Length} Bytes".ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Metadata       : " + $"{(MessageMetadata == null ? 0 : MessageMetadata?.Values.Count)} pieces of metadata".ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Encryption     : " + $"{Enum.GetName(typeof(EncryptionMethod),EncryptMode)}".ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "| - Compression    : " + $"{Enum.GetName(typeof(CompressionMethod),CompressMode)}".ToString().PadRight(49) + "|" + Environment.NewLine;
			stats += "=======================================================================" + Environment.NewLine;
			return stats;
		}

	}

}