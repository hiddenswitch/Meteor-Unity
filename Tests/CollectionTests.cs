using System;
using Meteor;
using UnityEngine;
using Extensions;
using System.Collections;
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
		public IEnumerator SubscribeAndGetRecords() {
			var startSubscribeAndGetRecordsTest = LiveData.Instance.Call ("startSubscribeAndGetRecordsTest");
			yield return (Coroutine)startSubscribeAndGetRecordsTest;

			var collection = Collection<TestCollection1Type>.Create ("testCollection1");

			yield return  (Coroutine)LiveData.Instance.Subscribe ("testCollection1");


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

			bool updated = false;

			collection.OnChanged += (arg1, arg2) => {
				Assert.AreEqual (arg2.field2.field3, 100);
				updated = true;
			};

			var method = LiveData.Instance.Call ("updateRecord");
			yield return (Coroutine)method;

			while (!updated) {
				yield return null;
			}

			yield break;
		}
	}
}

