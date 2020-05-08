using SimpleSockets.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public class _RequestPacket : Packet
	{
		/// <summary>
		/// Guid of the request, same guid will be used for the response.
		/// </summary>
		public Guid RequestGuid { get; internal set; }

		/// <summary>
		/// Expiration of the request
		/// </summary>
		public DateTime Expiration { get; internal set; }

		/// <summary>
		/// Request type.
		/// </summary>
		public Requests Request { get; private set; }

		// Extra info, used for the properties of this object.
		private IDictionary<object, object> _internalInfo = new Dictionary<object, object>();

		/// <summary>
		/// Creates a new packet 
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="filename"></param>
		/// <param name="timeInMs"></param>
		/// <returns></returns>
		internal static _RequestPacket FileTransferRequest(LogHelper logger, string filename, int timeInMs) {
			var p = new _RequestPacket(logger, timeInMs);
			p.RequestFileTransfer(filename);
			return p;
		}

		internal static _RequestPacket FileDeleteRequest(LogHelper logger, string filename, int timeInMs) {
			var p = new _RequestPacket(logger, timeInMs);
			p.RequestFileDeletion(filename);
			return p;
		}

		internal static _RequestPacket RequestPacketFromPacket(LogHelper logger, Packet packet)
		{
			var p = new _RequestPacket(logger);
			p.Map(packet).Build();
			return p;
		}

		private _RequestPacket(LogHelper logger) : base(PacketType.Request, logger) {

		}

		private _RequestPacket(LogHelper logger, int timeInMs) : base(PacketType.Request, logger) {
			RequestGuid = Guid.NewGuid();
			Expiration = (DateTime.Now + new TimeSpan(0, 0, 0, 0, timeInMs));
			_internalInfo.Add(PacketHelper.RESGUID, RequestGuid);
			_internalInfo.Add(PacketHelper.EXP, Expiration);
		}

		// Build properties from extra data.
		private void Build() {
			RequestGuid = GetRequestGuid();
			Expiration = GetExpiration();
			Request = GetRequest();
		}

		private void RequestFileTransfer(string filename) {
			_internalInfo.Add(PacketHelper.REQUEST, Requests.FileTransfer);
			_internalInfo.Add(PacketHelper.REQPATH, filename);
		}

		private void RequestFileDeletion(string filename) {
			_internalInfo.Add(PacketHelper.REQUEST, Requests.FileDelete);
			_internalInfo.Add(PacketHelper.REQPATH, filename);
		}

		private Requests GetRequest() {
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXP))
				throw new InvalidOperationException("No additional information is present.");

			return (Requests)int.Parse(AdditionalInternalInfo[PacketHelper.REQUEST].ToString());
		}

		private DateTime GetExpiration()
		{
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.EXP))
				throw new InvalidOperationException("No additional information is present.");

			return (DateTime)AdditionalInternalInfo[PacketHelper.EXP];
		}

		private Guid GetRequestGuid()
		{
			if (AdditionalInternalInfo == null || !AdditionalInternalInfo.ContainsKey(PacketHelper.RESGUID))
				throw new InvalidOperationException("No additional information is present.");

			return Guid.Parse(AdditionalInternalInfo[PacketHelper.RESGUID].ToString());
		}

		// Write extra info to the message.
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
