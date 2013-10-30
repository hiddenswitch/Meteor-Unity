using System;
using UnityEngine;
using System.Collections;

namespace Meteor
{
	public static class Connection
	{
		public static string Url {
			get;
			set;
		}

		public static Coroutine Connect(string url) {
			Url = url;
			return LiveData.Instance.Connect (url);
		}

		static IEnumerator ReconnectCoroutine() {
			// Resubscribe to all subscriptions
			yield break;
		}
	}
}

