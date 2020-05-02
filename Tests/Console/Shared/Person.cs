using System;

namespace Shared
{
	public class Person
	{

		public Person(string firstname, string lastname) {
			FirstName = firstname;
			LastName = lastname;
		}

		public string FirstName { get; private set; }

		public string LastName { get; private set; }

	}
}
