#if TESTS
using System;
using System.Collections;
using Meteor;
using UnTested;

namespace Meteor.Tests
{
	[TestFixture]
	public class AccountsTests : ConnectionTestDependency
	{
		// Make sure that the accounts-password package is added to your meteor server
		[Test]
		public IEnumerator LoginAsGuest() {
			yield return Accounts.LoginAsGuest ();
			Assert.IsNotNull (Accounts.UserId);
			yield break;
		}

		[Test]
		public IEnumerator CreateAndLoginUser() {
			yield return Accounts.CreateAndLoginWith (string.Format ("test{0}@gtest.com", UnityEngine.Random.Range (0, 100000000)),
			                                          string.Format ("test{0}", UnityEngine.Random.Range (0, 100000000)),
			                                          "testpassword");
			Assert.IsNotNull (Accounts.UserId);
			yield break;
		}
	}
}
#endif
