using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsyncClientServer.Example.Server.Annotations;
using AsyncClientServer.Server;

namespace AsyncClientServer.Example.Server.Model
{

	public delegate void ReceivedText(string message);

	public class ClientEntry: INotifyPropertyChanged
	{

		private bool _connected;
		private string _remoteIPv4, _remoteIPv6, _localIPv4, _localIPv6;

		public int ListId { get; set; }

		public int Id { get; set; }
		public bool Connected
		{
			get => _connected;
			set
			{
				_connected = value;
				OnPropertyChanged();
			}
		}
		public string RemoteIPv4
		{
			get => _remoteIPv4;
			set
			{
				_remoteIPv4 = value;
				OnPropertyChanged();
			}
		}
		public string RemoteIPv6
		{
			get => _remoteIPv6;
			set
			{
				_remoteIPv6 = value;
				OnPropertyChanged();
			}
		}

		public string LocalIPv4
		{
			get => _localIPv4;
			set
			{
				_localIPv4 = value;
				OnPropertyChanged();
			}
		}
		public string LocalIPv6
		{
			get => _localIPv6;
			set
			{
				_localIPv6 = value;
				OnPropertyChanged();
			}
		}

		public string LogPath { get; set; }

		public event ReceivedText TextReceived;

		public ClientEntry(int id, string localIPv4, string localIPv6, string remoteIPv4, string remoteIPv6)
		{
			Id = id;
			RemoteIPv4 = remoteIPv4;
			RemoteIPv6 = remoteIPv6;

			LocalIPv4 = localIPv4;
			LocalIPv6 = localIPv6;

		}

		public void Read(string message)
		{
			if (LogPath == null)
			{
				LogPath = @"Data\client" + Id + ".txt";
			}

			if (!File.Exists(LogPath))
			{
				FileInfo file = new FileInfo(Path.GetFullPath(LogPath));
				file.Directory?.Create();
			}

			string msg = "[" + ConvertDateTimeToString(DateTime.Now) + "] " + message;
			using (StreamWriter file = new StreamWriter(Path.GetFullPath(LogPath),true))
			{
				file.WriteLine(msg);
			}

			TextReceived?.Invoke(msg);
		}

		//Converts DateTime to a string according to cultureInfo. (uses CurrentCulture.)
		private static string ConvertDateTimeToString(DateTime time)
		{
			var cultureInfo = CultureInfo.CurrentCulture;
			//CultureInfo us = new CultureInfo("en-US");
			var shortDateFormatString = cultureInfo.DateTimeFormat.ShortDatePattern;
			var longTimeFormatString = cultureInfo.DateTimeFormat.LongTimePattern;

			return time.ToString(shortDateFormatString + " " + longTimeFormatString, cultureInfo);

		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		}
	}
}
