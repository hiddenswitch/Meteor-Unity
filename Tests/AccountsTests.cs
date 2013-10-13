using System;
using System.Collections;

namespace Meteor.Tests
{
	[TestFixture]
	public class AccountsTests
	{
		public AccountsTests ()
		{

		}


		[Test]
		public IEnumerator ConnectToMeteor() {
			yield return Meteor.Connect ("localhost:3000");

			Assert.IsTrue (Meteor.IsConnected);

			yield break;
		}
	}
}

