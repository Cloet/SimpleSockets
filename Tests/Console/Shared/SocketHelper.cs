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
			using (var stream = this.GetType().Assembly.GetManifestResourceStream("Shared.Security.cert.pfx"))
			{
				byte[] buffer = new byte[16 * 1024];
				using (MemoryStream ms = new MemoryStream())
				{
					int read;
					while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
					{
						ms.Write(buffer, 0, read);
					}
					return ms.ToArray();
				}
			}

		}

	}
}
