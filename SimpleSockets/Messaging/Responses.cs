using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public enum Responses
	{
		Error=0,
		ReqFilePathOk=0,
		FileExists=1,
		FileDeleted=2
	}
}
