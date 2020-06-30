using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public class Response
	{
		/// <summary>
		/// The guid of the response, should match the guid of the request.
		/// </summary>
		[JsonProperty]
		public Guid ResponseGuid { get; internal set; }

		/// <summary>
		/// Gives more info to the connected socket why a response resulted in an error.
		/// </summary>
		[JsonProperty]
		public string ExceptionMessage { get; internal set; }

		/// <summary>
		/// Exception thrown
		/// </summary>
		[JsonProperty]
		public Exception Exception { get; internal set; }

		/// <summary>
		/// The response type.
		/// </summary>
		[JsonProperty]
		public ResponseType Resp { get; internal set; }

		/// <summary>
		/// Data of the response.
		/// </summary>
		[JsonProperty]
		public object Data { get; internal set; }

		/// <summary>
		/// Type of the data.
		/// </summary>
		/// <value></value>
		[JsonProperty]
		public Type DataType {get; internal set;}

		internal static Response CreateResponse(Guid guid, ResponseType response, string errorMsg, Exception ex) {
			return new Response(guid, response, errorMsg, ex);
		}

		internal static Response CreateResponse(Guid guid, ResponseType response, string errorMsg, Exception ex, object data) {
			var resp = new Response(guid, response, errorMsg, ex);
			resp.Data = data;
			resp.DataType = data.GetType();
			return resp;
		}

		private Response(Guid guid, ResponseType resp, string errorMsg, Exception ex) {
			ResponseGuid = guid;
			Resp = resp;
			Exception = ex;
			ExceptionMessage = errorMsg;
		}

		[JsonConstructor]
		private Response()
		{
		}

		internal Packet BuildResponseToPacket() {
			return PacketBuilder.NewPacket
				.SetPacketObject(this)
				.SetMetadata(null)
				.SetPacketType(PacketType.Response)
				.Build();
		}

	}
}
