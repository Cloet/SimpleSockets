using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public class Request
	{
		/// <summary>
		/// The guid of the request.
		/// </summary>
		[JsonProperty]
		public Guid RequestGuid { get; internal set; }

		/// <summary>
		/// Expiration of the request.
		/// </summary>
		[JsonProperty]
		public DateTime Expiration { get; internal set; }

		/// <summary>
		/// Request type
		/// </summary>
		[JsonProperty]
		public RequestType Req { get; internal set; }

		/// <summary>
		/// Data of the request.
		/// </summary>
		[JsonProperty]
		public string Data { get; internal set; }

		internal static Request FileTransferRequest(string filename, int timeInMs) {
			var p = new Request(timeInMs);
			p.Req = RequestType.FileTransfer;
			p.Data = filename;
			return p;
		}

		internal static Request FileDeletionRequest(string filename, int timeInMs) {
			var p = new Request(timeInMs);
			p.Req = RequestType.FileDelete;
			p.Data = filename;
			return p;
		}

		internal static Request DirectoryInfoRequest(string directory, int timeInMs) {
			var p = new Request(timeInMs);
			p.Req = RequestType.DirectoryInfo;
			p.Data = directory;
			return p;
		}

		private Request(int timeInMs) {
			RequestGuid = Guid.NewGuid();
			Expiration = (DateTime.Now + new TimeSpan(0, 0, 0, 0, timeInMs));
		}

		[JsonConstructor]
		private Request() {
		}

		internal Packet BuildRequestToPacket()
		{
			return PacketBuilder.NewPacket
				.SetPacketObject(this)
				.SetMetadata(null)
				.SetPacketType(PacketType.Request)
				.Build();
		}

	}
}
