using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClientServer
{
	public class ReceivingItem
	{

		public string SavePath { get; set; }
		public bool Encrypted { get; set; }
		public int TotalBytes { get; set; }
		public int ReceivedBytes { get; set; }
		public bool Done {
			get
			{
				if (TotalBytes == ReceivedBytes)
					return true;

				return false;
			}
		}


		public ReceivingItem(string savePath,bool encrypted, int totalBytes)
		{
			SavePath = savePath;
			Encrypted = encrypted;
			TotalBytes = totalBytes;
		}

		public ReceivingItem()
		{
		}

	}
}
