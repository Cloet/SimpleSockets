namespace SimpleSockets.Messaging
{
    public enum ResponseType
	{
		Error=0,
		ReqFilePathOk=1,
		FileExists=2,
		FileDeleted=3,
		CustomResponse=4,
		UdpResponse=5
	}
}
