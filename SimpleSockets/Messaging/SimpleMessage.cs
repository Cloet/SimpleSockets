using System;
using System.Collections;
using System.Text;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using System.Linq;
using System.Collections.Generic;
using SimpleSockets.Helpers.Serialization;

namespace SimpleSockets.Messaging {

    internal class SimpleMessage {

		/// <summary>
		/// Indicates what sort of messagetype this message is
		/// </summary>
        internal MessageType MessageType { get; set; }

		/// <summary>
		/// The metadata of the message
		/// </summary>
        internal byte[] MessageMetadata {
			get => _metadata;
			set {
				if (value == null || value.Length == 0)
					HeaderFields[0] = false;
				else
					HeaderFields[0] = true;

				_metadata = value;
			}
		}

		/// <summary>
		/// Presharedkey of the message
		/// </summary>
        internal byte[] PreSharedKey {
			get => _preSharedKey;
			set {

				if (value == null || value.Length == 0)
					HeaderFields[3] = false;
				else
					HeaderFields[3] = true;

				_preSharedKey = value;
			}
		}

		/// <summary>
		/// Extra information of a messages, only used within library.
		/// </summary>
        internal byte[] AdditionalInternalInfo {
			get => _internalInfo;
			set
			{
				if (value == null || value.Length == 0)
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
		internal byte[] Data { get; set; }

		/// <summary>
		/// When receiving a message all content will be stored here in netstring format
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
        internal byte[] EncryptionKey { get; set; }

		/// <summary>
		/// If a message is compressed this indicates what compression is used.
		/// </summary>
        internal CompressionType CompressMode { get; set; }

		/// <summary>
		/// If a message is encrypted this indicates what encryption is used.
		/// </summary>
        internal EncryptionType EncryptMode { get; set; }

		/// <summary>
		/// Length of the header
		/// </summary>
        internal int HeaderLength { get; private set; }

		private byte[] _internalInfo;

		private byte[] _metadata;

		private byte[] _preSharedKey;

        private LogHelper _logger;

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

		//Construct of receiver
        internal SimpleMessage(LogHelper logger) {
			HeaderFields = new BitArray(8, false);
			MessageMetadata = new byte[0];
			PreSharedKey = new byte[0];
			Content = new byte[0];
			Data = new byte[0];
			Encrypt = false;
			Compress = false;
			_logger = logger;
			ContentLength = 0;
			OriginalcontentLength = 0;
		}

		//Constructor of a message
        internal SimpleMessage(MessageType type, LogHelper logger = null): this(logger) {
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
                headerFieldBytes = MessageHelper.MergeByteArrays(headerFieldBytes, PreSharedKey);

			// Write contentlength (8 Bytes)
			byte[] bytes = new byte[8];
			BitConverter.GetBytes(originalContentLength).CopyTo(bytes, 0);
            headerFieldBytes = MessageHelper.MergeByteArrays(headerFieldBytes, bytes);

			// Length of data in encrypted/compressed form. (8 Bytes)
			if (HeaderFields[2] || HeaderFields[1]) {
				bytes = new byte[8];
				BitConverter.GetBytes(contentLength).CopyTo(bytes, 0);
				headerFieldBytes = MessageHelper.MergeByteArrays(headerFieldBytes, bytes);
			}

            return headerFieldBytes;
        }
        
        /// <summary>
        /// Content is converted from bytes to hex and written in netstring format.
        /// https://en.wikipedia.org/wiki/Netstring
        /// </summary>
        /// <returns>Content Byte array</returns>
        private byte[] BuildContent() {
            
            var converted = MessageHelper.ByteArrayToString(Data);

			var content = string.Empty;

			if (converted.Length == 0)
				content = "0:,";
			else
				content = $"{converted.Length}:{converted},";

			if (HeaderFields[0])
			{
				converted = MessageHelper.ByteArrayToString(MessageMetadata);
				if (converted.Length == 0)
					content += "0:,";
				else
					content += $"{converted.Length}:{converted},";
			}
			else
				content += "0:,";

			if (HeaderFields[4])
			{
				converted = MessageHelper.ByteArrayToString(AdditionalInternalInfo);
				if (converted.Length == 0)
					content += "0:,";
				else
					content += $"{converted.Length}:{converted}";
			}
			else
				content += "0:";
         
            return Encoding.UTF8.GetBytes(content);
        }

		/// <summary>
		/// Builds the payload of a message.
		/// </summary>
		/// <returns>Byte[] array</returns>
        internal byte[] BuildPayload() {
            
            _logger?.Log("===========================================", LogLevel.Trace);
            _logger?.Log("Building Message.",LogLevel.Trace);
            // Content contains
            // 1 - Data
            // 2 - MessageMetadata
            // 3 - AdditionalInternalInfo
            var content = BuildContent();
            var originalContentLength = content.Length;
            
            // Compress the content if so desired.
            if (Compress && CompressMode != CompressionType.None) {
                _logger?.Log("Compressing content...", LogLevel.Trace);
                content = CompressionHelper.Compress(content, CompressMode);
            }

            // Encrypt the content after potential compression if so desired.
            if (Encrypt && EncryptMode != EncryptionType.None) {
                _logger?.Log("Encrypting content...", LogLevel.Debug);
                content = CryptographyHelper.Encrypt(Content, EncryptionKey, EncryptMode);
            }

            byte[] head = BuildMessageHeader(originalContentLength, content.Length);
            byte[] tail = MessageHelper.PacketDelimiter;
            byte[] Payload = new byte[head.Length + content.Length + tail.Length];

            _logger?.Log("Messageheader is " + head.Length + " bytes long.", LogLevel.Trace);
            _logger?.Log("Message content is " + content.Length + " bytes long.", LogLevel.Trace);

            head.CopyTo(Payload,0);
            content.CopyTo(Payload,head.Length);
            tail.CopyTo(Payload,head.Length + content.Length);

            _logger?.Log("The message has been built.", LogLevel.Trace);
            _logger?.Log("===========================================", LogLevel.Trace);
            return Payload;
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
            HeaderLength = 11; //Eerste bit wordt niet meegeteld.

            // Extra length if using presharedkey
            if (HeaderFields[3])
                HeaderLength += 16;

            // Extra length is encrypted / compressed
            if (HeaderFields[2] || HeaderFields[1])
                HeaderLength += 8;
        }

		/// <summary>
		/// Deconstruct the header fields and set lengths of various message parts.
		/// </summary>
        internal void DeconstructHeaders() {
            var header = MessageHeader;

            // Get the messagetype of the message
            MessageType  = (MessageType)	 header[0];
			CompressMode = (CompressionType) header[1];
			EncryptMode  = (EncryptionType)  header[2];

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
		internal void BuildMessageFromContent(byte[] preSharedKey) {

			if (preSharedKey != null && preSharedKey.Length > 0 && HeaderFields[3] == false)
				throw new Exception("Expected a presharedkey but none was found.");

			if (HeaderFields[3] && !PreSharedKey.SequenceEqual(PreSharedKey))
				throw new Exception("The presharedkey does not match between messages.");
			
			if (ContentLength != Content.Length)
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

			if (OriginalcontentLength != Content.Length)
				throw new Exception($"Expected contentlength differs from received contentlength after decryption/decompression.{Environment.NewLine}Expected {OriginalcontentLength} bytes but received {Content.Length} bytes.");

			var netstring = Encoding.UTF8.GetString(Content);
			var arr = netstring.Split(',');

			for (int i = 0; i < arr.Length; i++)
			{
				if (i == 0)
					Data = DataFromOneNetString(arr[i]);
				if (i == 1)
					MessageMetadata = DataFromOneNetString(arr[i]);
				if (i == 2)
					AdditionalInternalInfo = DataFromOneNetString(arr[2]);
				if (i > 2)
					_logger?.Log("Found more netstrings then possible.", LogLevel.Error);
			}
		}

		/// <summary>
		/// Converts one Netstring to byte array.
		/// Netstring format: 'Length:Content,'
		/// Example: 5:Hello,
		/// For these messages the content will be converted from bytes to a hex string.
		/// </summary>
		/// <param name="netstring"></param>
		/// <returns></returns>
		private byte[] DataFromOneNetString(string netstring) {
			var netr = netstring.Split(':');

			if (netr.Length != 2)
				throw new Exception("Netstring must have 2 arguments.");

			if (int.Parse(netr[0]) != netr[1].Length)
				throw new Exception("Netstring predifined length is different then actual length.");

			return MessageHelper.StringToByteArray(netr[1]);
		}

		/// <summary>
		/// Build metadata from the stored bytes.
		/// </summary>
		/// <returns></returns>
		internal IDictionary<object, object> BuildMetadataFromBytes() {
			try
			{
				if (MessageMetadata == null || MessageMetadata.Length == 0)
					return null;

				return SerializationHelper.DeserializeJson<IDictionary<object, object>>(MessageMetadata);
			}
			catch (Exception ex) {
				_logger?.Log("Failed to retrieve metadata from bytes.", ex, LogLevel.Warning);
				return null;
			}
		}

		/// <summary>
		/// Builds the internal info from stored bytes.
		/// </summary>
		/// <returns></returns>
		internal IDictionary<object, object> BuildInternalInfoFromBytes()
		{
			try
			{
				if (AdditionalInternalInfo == null || AdditionalInternalInfo.Length == 0)
					return null;

				return SerializationHelper.DeserializeJson<IDictionary<object, object>>(AdditionalInternalInfo);
			}
			catch (Exception ex)
			{
				_logger?.Log("Failed to retrieve internal info from bytes.", ex, LogLevel.Warning);
				return null;
			}
		}

		internal object BuildObjectFromBytes(out Type type) {

			type = null;

			try
			{
				var info = BuildInternalInfoFromBytes();

				if (info == null)
					return null;

				var exists = info.TryGetValue("Type", out var output);

				if (!exists)
					return null;

				type = Type.GetType(output.ToString());

				return SerializationHelper.DeserializeBytesToObject(Data, type);
			}
			catch (Exception ex) {
				_logger?.Log("Failed building object from bytes.", ex, LogLevel.Warning);
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
				_logger?.Log("Failed converting byte[] array to string.", ex, LogLevel.Warning);
				return null;
			}
		}


        #endregion

    }

}