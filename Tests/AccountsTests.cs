using System;
using System.Collections;
using Meteor;

namespace Meteor.Tests
{
	[TestFixture]
	public class AccountsTests : ConnectionTestDependency
	{
		public AccountsTests ()
		{

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

