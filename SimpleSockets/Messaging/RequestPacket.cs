using SimpleSockets.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public class RequestPacket : Packet
	{

		public Guid RequestGuid { get; internal set; }

		public DateTime Expiration { get; internal set; }

		public Request Request { get; private set; }

		private IDictionary<object, object> _internalInfo = new Dictionary<object, object>();

		internal static RequestPacket NewRequestPacket(LogHelper logger) => new RequestPacket(logger, false);

		internal static RequestPacket ReceiverRequestPacket(LogHelper logger) =>  new RequestPacket(logger, true);

		private RequestPacket(LogHelper logger, bool build) : base(PacketType.Request, logger) {
			if (build)
			{
			}
			else {
				RequestGuid = Guid.NewGuid();
				_internalInfo.Add(PacketHelper.RESGUID, RequestGuid);
				_internalInfo.Add(PacketHelper.EXP, Expiration);
			}	
		}

		internal void Build() {
			RequestGuid = GetRequestGuid();
			Expiration = GetExpiration();
			Request = GetRequest();
		}

		internal void SetFileTransferRequest(string filename) {
			_internalInfo.Add(PacketHelper.REQUEST, Request.FileTransfer);
			_internalInfo.Add(PacketHelper.REQPATH, filename);
		}

		internal void SetDeleteFileRequest(string filename) {
			_internalInfo.Add(PacketHelper.REQUEST, Request.FileDelete);
			_internalInfo.Add(PacketHelper.REQPATH, filename);
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

		internal Request GetRequest() {
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXP))
				throw new InvalidOperationException("No additional information is present.");

			return (Request)int.Parse(AdditionalInternalInfo[PacketHelper.REQUEST].ToString());
		}

		internal DateTime GetExpiration()
		{
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXP))
				throw new InvalidOperationException("No additional information is present.");

			return (DateTime)AdditionalInternalInfo[PacketHelper.EXP];
		}

		internal Guid GetRequestGuid()
		{
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.RESGUID))
				throw new InvalidOperationException("No additional information is present.");

			return Guid.Parse(AdditionalInternalInfo[PacketHelper.RESGUID].ToString());
		}

	}
}
