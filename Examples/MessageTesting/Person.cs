using System;
using System.Collections.Generic;
using System.Text;

namespace MessageTesting
{
	[Serializable]
	public class Person
	{

		public string Name { get; set; }

		public string FirstName { get; set; }

		public string Street { get; set; }

		public Person()
		{

		}

		public Person(string name, string firstName, string street)
		{
			Name = name;
			FirstName = firstName;
			Street = street;
		}

	}
}
