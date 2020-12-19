using Newtonsoft.Json;
using System;

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
		/// Header of the request.
		/// </summary>
		/// <value></value>
		[JsonProperty]
		public string Header { get; internal set; }

		/// <summary>
		/// Data of the request.
		/// </summary>
		[JsonProperty]
		public object Data { get; internal set; }

		/// <summary>
		/// Type of the request
		/// </summary>
		/// <value></value>
		[JsonProperty]
		public Type DataType { get; internal set; }

		internal static Request CustomRequest(int timeInMs,string header, object data) {
			var p = new Request(timeInMs);
			p.Req = RequestType.CustomReq;
			p.Data = data;
			p.DataType = data?.GetType();
			p.Header = header;
			return p;
		}

		internal static Request FileTransferRequest(string filename, int timeInMs) {
			var p = new Request(timeInMs);
			p.Req = RequestType.FileTransfer;
			p.Data = filename;
			p.DataType = filename?.GetType();
			return p;
		}

		internal static Request FileDeletionRequest(string filename, int timeInMs) {
			var p = new Request(timeInMs);
			p.Req = RequestType.FileDelete;
			p.Data = filename;
			p.DataType = filename?.GetType();
			return p;
		}

		internal static Request UdpMessageRequest(string packet, int timeInMs) {
			var p = new Request(timeInMs);
			p.Req = RequestType.UdpMessage;
			p.Data = packet;
			p.DataType = packet?.GetType();
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
