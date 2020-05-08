using SimpleSockets.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public class _ResponsePacket: Packet
	{
		/// <summary>
		/// The guid of the response, should match the guid of the request.
		/// </summary>
		public Guid ResponseGuid { get; internal set; }

		/// <summary>
		/// Expiration of the response
		/// </summary>
		public DateTime Expiration { get; internal set; }

		/// <summary>
		/// Gives more info to the connected socket why a response resulted in an error.
		/// </summary>
		public string Exception { get; internal set; }

		/// <summary>
		/// The response type.
		/// </summary>
		public Responses Response { get; internal set; }

		// Extra additional internal info.
		private IDictionary<object, object> _internalInfo = new Dictionary<object, object>();

		// Creates a new responsepacket.
		internal static _ResponsePacket NewResponsePacket(LogHelper logger,Guid guid, Responses resp,int timeInMs, string msg) => new _ResponsePacket(logger,guid, resp, timeInMs, msg);

		// Creates a responsepacket and copy data from normal packet to it.
		internal static _ResponsePacket ReceiverResponsePacket(LogHelper logger, Packet packet) {
			var p = new _ResponsePacket(logger);
			p.Map(packet).Build();
			return p;
		}

		//Constructor
		private _ResponsePacket(LogHelper logger,Guid guid, Responses resp,int timeInMs, string errorMsg): base(PacketType.Response, logger) {
			ResponseGuid = guid;
			Response = resp;
			_internalInfo.Add(PacketHelper.RESPONSE, Response);
			_internalInfo.Add(PacketHelper.EXP, Expiration);
			_internalInfo.Add(PacketHelper.RESGUID, ResponseGuid);
			_internalInfo.Add(PacketHelper.EXCEPTION, errorMsg);
		}

		//Constructor
		private _ResponsePacket(LogHelper logger) : base(PacketType.Response, logger)
		{
		}

		private void Build() {
			ResponseGuid = GetResponseGuid();
			Expiration = GetExpiration();
			Response = GetResponse();
			Exception = GetException();
		}

		private Responses GetResponse() {
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXP))
				throw new InvalidOperationException("No additional information is present.");

			return (Responses) int.Parse(AdditionalInternalInfo[PacketHelper.RESPONSE].ToString());
		}

		private DateTime GetExpiration() {
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXP))
				throw new InvalidOperationException("No additional information is present.");

			return (DateTime) AdditionalInternalInfo[PacketHelper.EXP];
		}

		private Guid GetResponseGuid() {
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.RESGUID))
				throw new InvalidOperationException("No additional information is present.");

			return Guid.Parse(AdditionalInternalInfo[PacketHelper.RESGUID].ToString());
		}

		private string GetException()
		{
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXCEPTION))
				throw new InvalidOperationException("No additional information is present.");

			return AdditionalInternalInfo[PacketHelper.EXCEPTION].ToString();
		}

		internal override byte[] BuildPayload()
		{
			if (AdditionalInternalInfo == null || AdditionalInternalInfo.Values.Count == 0)
			{
				AdditionalInternalInfo = _internalInfo;
			}
			else
			{
				lock (AdditionalInternalInfo)
				{
					foreach (var val in _internalInfo)
					{
						if (!AdditionalInternalInfo.ContainsKey(val.Key))
							AdditionalInternalInfo.Add(val.Key, val.Value);
					}
				}
			}
			return base.BuildPayload();
		}

	}
}
