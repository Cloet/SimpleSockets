namespace SimpleSockets.Messaging
{
	public enum MessageFlag
	{
		Idle=0,
		ProcessingHeader=1,
		ProcessingMetadata=2,
		ProcessingData=3,
		MessageReceivedNoExtraData=4,
		MessageReceivedExtraData=5,
		MetaDataReceivedNoExtraData=6,
		MetaDataReceivedExtraData=7
	}
}
