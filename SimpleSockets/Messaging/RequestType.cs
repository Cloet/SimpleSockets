using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public enum RequestType
	{
		FileTransfer=0,
		FileDelete=1,
		DirectoryInfo=2,
		DriveInfo=3,
		CustomReq=4
	}
}
