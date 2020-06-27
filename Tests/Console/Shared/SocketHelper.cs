using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shared
{
	public class SocketHelper
	{
		public byte[] GetCertFileContents()
		{
			var path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "/Security/TestCertificate.pfx";
			return File.ReadAllBytes(path);
		}

	}
}
