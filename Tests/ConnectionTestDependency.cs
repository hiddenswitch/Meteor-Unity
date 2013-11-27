#if TESTS
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnTested;

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


			if (!LiveData.Instance.Connected) {
				yield return LiveData.Instance.Connect ("ws://127.0.0.1:3000/websocket");
			}

			Assert.IsTrue (LiveData.Instance.Connected);

			yield break;
		}
	}
}
#endif