using System;
using UnityEngine;
using System.Collections;

namespace Meteor
{
	public static class Connection
	{
		static Connection ()
		{
			LiveData.Instance.DidConnect += delegate(string obj) {
				if (DidConnect != null) {
					DidConnect ();
				}
			};
		}

		public static string Url {
			get;
			set;
		}

		public static Coroutine Connect (string url)
		{
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

		public static bool Connected {
			get {
				return LiveData.Instance.Connected;
			}
		}

		public static event Action DidConnect;
	}
}

