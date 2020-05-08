using SimpleSockets.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public class ResponsePacket: Packet
	{

		public Guid ResponseGuid { get; internal set; }

		public DateTime Expiration { get; internal set; }

		public string Exception { get; internal set; }

		public Response Response { get; internal set; }

		private IDictionary<object, object> _internalInfo = new Dictionary<object, object>();

		internal static ResponsePacket NewResponsePacket(LogHelper logger,Guid guid, Response resp, string msg = "") => new ResponsePacket(logger,guid, resp, msg);

		internal static ResponsePacket ReceiverResponsePacket(LogHelper logger) => new ResponsePacket(logger);

		private ResponsePacket(LogHelper logger,Guid guid, Response resp, string errorMsg): base(PacketType.Response, logger) {
			ResponseGuid = guid;
			Response = resp;
			_internalInfo.Add(PacketHelper.RESPONSE, Response);
			_internalInfo.Add(PacketHelper.EXP, Expiration);
			_internalInfo.Add(PacketHelper.RESGUID, ResponseGuid);
			_internalInfo.Add(PacketHelper.EXCEPTION, errorMsg);
		}

		private ResponsePacket(LogHelper logger) : base(PacketType.Response, logger)
		{
		}

		internal void Build() {
			ResponseGuid = GetResponseGuid();
			Expiration = GetExpiration();
			Response = GetResponse();
			Exception = GetException();
		}

		internal override byte[] BuildPayload()
		{
			if (AdditionalInternalInfo == null || AdditionalInternalInfo.Values.Count == 0)
			{
				AdditionalInternalInfo = _internalInfo;
			}
			else {
				lock (AdditionalInternalInfo) {
					foreach (var val in _internalInfo)
					{
						if (!AdditionalInternalInfo.ContainsKey(val.Key))
							AdditionalInternalInfo.Add(val.Key, val.Value);
					}
				}
			}
			return base.BuildPayload();
		}

		internal Response GetResponse() {
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXP))
				throw new InvalidOperationException("No additional information is present.");

			return (Response) int.Parse(AdditionalInternalInfo[PacketHelper.RESPONSE].ToString());
		}

		internal DateTime GetExpiration() {
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXP))
				throw new InvalidOperationException("No additional information is present.");

			return (DateTime) AdditionalInternalInfo[PacketHelper.EXP];
		}

		internal Guid GetResponseGuid() {
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.RESGUID))
				throw new InvalidOperationException("No additional information is present.");

			return Guid.Parse(AdditionalInternalInfo[PacketHelper.RESGUID].ToString());
		}

		internal string GetException()
		{
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXCEPTION))
				throw new InvalidOperationException("No additional information is present.");

			return AdditionalInternalInfo[PacketHelper.EXCEPTION].ToString();
		}

	}
}
