using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public enum ResponseType
	{
		Error=0,
		ReqFilePathOk=1,
		FileExists=2,
		FileDeleted=3,
		DirectoryInfo=4
	}
}
