using System;
using System.Collections;

namespace Meteor.Tests
{
	[TestFixture]
	public class ConnectionTestDependency
	{
		public ConnectionTestDependency ()
		{
		}

		[TestSetup]
		public IEnumerator ConnectToLocalhost() {
			if (!Client.IsConnected) {
				yield return Client.Connect ("ws://127.0.0.1:3000/websocket");
			}

			Assert.IsTrue (Client.IsConnected);

			yield break;
		}
	}
}

