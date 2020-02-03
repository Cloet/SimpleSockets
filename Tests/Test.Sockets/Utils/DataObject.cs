using System;
using System.Collections.Generic;
using System.Text;

namespace Test.Sockets.Utils
{
	public class DataObject
	{

		public string Name { get; private set; }

		public string Text { get; private set; }

		public double Number { get; private set; }

		public DateTime Date { get; private set; }

		public DataObject(string name, string text, double numbervalue, DateTime date) {
			Name = name;
			Text = text;
			Number = numbervalue;
			Date = date;
		}

	}
}
