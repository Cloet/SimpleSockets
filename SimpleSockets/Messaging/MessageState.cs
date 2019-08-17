using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSockets.Messaging
{
	public enum MessageState
	{
		Beginning=0,
		ReceivingData=1,
		Decompressing=2,
		Decrypting=3,
		Completed=4,
		Compressing=5,
		Encrypting=6,
		Transmitting=7,
		CompressingDone=8,
		EncryptingDone=9,
		DecompressingDone=10,
		DecryptingDone
	}
}
