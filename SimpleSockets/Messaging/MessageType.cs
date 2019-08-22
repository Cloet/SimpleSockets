using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public enum MessageType
	{
		Message=0,
		Bytes = 1,
		CustomHeader = 2,
		MessageContract =3,
		File=4,
		Folder=5,
		Object=6,
		Auth=7,
		BasicAuth=8
	}
}
