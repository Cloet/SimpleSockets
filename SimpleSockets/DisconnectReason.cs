using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets
{
	public enum DisconnectReason
	{
		Normal=0,
		Forceful=1,
		Timeout=2,
		Unknown=3
	}
}
