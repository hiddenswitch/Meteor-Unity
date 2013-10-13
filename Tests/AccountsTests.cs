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
			yield return Meteor.Connect ("ws://127.0.0.1:3000/websocket");

			Assert.IsTrue (Meteor.IsConnected);

			yield break;
		}
	}
}

