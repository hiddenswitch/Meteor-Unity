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

		public static bool Logging {
			get {
				return LiveData.Instance.Logging;
			}
			set {
				LiveData.Instance.Logging = value;
			}
		}

		static IEnumerator ReconnectCoroutine() {
			// Resubscribe to all subscriptions
			yield break;
		}
	}
}

