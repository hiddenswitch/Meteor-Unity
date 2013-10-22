using System;
using Meteor;
using UnityEngine;
using Extensions;
using System.Collections;
using System.Linq;
using UnTested;

namespace Meteor.Tests
{
	[TestFixture]
	public class CollectionTests : ConnectionTestDependency
	{
		public class TestCollection1Type : MongoDocument {
			public string field1;
			public TestCollection1Subtype field2;

			public TestCollection1Type() {}
		}

		public class TestCollection1Subtype {
			public int field3;
			public object field4;

			public TestCollection1Subtype() {}
		}

		public CollectionTests ()
		{
		}

		[Test]
		public IEnumerator NoSideEffects() {
			var noSideEffectsCall = Method<string>.Call ("noSideEffects");
			yield return (Coroutine)noSideEffectsCall;

			Assert.AreEqual (noSideEffectsCall.Response, "done");
		}

		public class Clam : MongoDocument {
			public Clam() {}
			public string name;
			public double? clamIdentity;
		}

		[Test]
		public IEnumerator GetClams() {
			var clams = Collection<Clam>.Create("clams");

			clams.OnAdded += (string arg1, Clam arg2) => {
				Debug.Log(string.Format("The clams. Name: {0}",arg2.name));
			};

			yield return (Coroutine)Subscription.Subscribe ("theClams");

			Debug.Log ("All clams received");

			var clamsMethod = Method<int>.Call ("joeClams", 10);
			yield return (Coroutine)clamsMethod;
			Debug.Log (string.Format ("Number of clams total: {0}", clamsMethod.Response));
//			Assert.AreEqual (clamsMethod.Response, 10);

			clamsMethod = Method<int>.Call ("joeClams", 10);
			yield return (Coroutine)clamsMethod;
			Debug.Log (string.Format ("Number of clams total: {0}", clamsMethod.Response));

			Debug.Log (string.Format ("clams in my database: {0}",clams.Count));

			Debug.Log ("clam identities less than 0.5:");
			foreach(var clam in clams.Where(c => c.clamIdentity != null && c.clamIdentity.GetValueOrDefault() < 0.5)) {
				Debug.Log (string.Format ("clam name: {0}, identity: {1}", clam.name, clam.clamIdentity));
			}

			yield return (Coroutine)Method.Call ("makeClamsThisIdentity", 0.95);

			yield break;
		}

		[Test]
		public IEnumerator SubscribeAndGetRecords() {
			var startSubscribeAndGetRecordsTest = Method.Call ("startSubscribeAndGetRecordsTest");
			yield return (Coroutine)startSubscribeAndGetRecordsTest;

			var collection = Collection<TestCollection1Type>.Create ("testCollection1");

			yield return  (Coroutine)Subscription.Subscribe ("testCollection1");


			foreach (var item in collection) {
				Debug.Log (item.Serialize ());
				if (item.field1 == "string 1") {
					Assert.AreEqual (item.field2.field3, 3);
				} else if (item.field1 == "string 2") {
					Assert.AreEqual (item.field2.field3, 30);
				} else {
					Assert.IsTrue (false);
				}
			}

			collection.OnAdded += (string arg1, TestCollection1Type arg2) => {
				Assert.IsNotNull(arg2.field2);
			};

			collection.OnChanged += (arg1, arg2) => {
				Assert.AreEqual (arg2.field2.field3, 100);
			};

			var method = Method<string>.Call ("updateRecord");
			yield return (Coroutine)method;

			Assert.IsNotNull (method.Response);

			yield break;
		}
	}
}

