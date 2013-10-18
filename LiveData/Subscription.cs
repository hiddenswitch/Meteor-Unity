using System;
using UnityEngine;
using System.Collections;

namespace Meteor
{
	public class Subscription
	{
		bool _ready;
		public bool ready {
			get {
				return _ready;
			}
			set {
				_ready = value;
				if (_ready && OnReady != null) {
					OnReady (name);
				}
			}
		}

		public event Action<string> OnReady;

		public string name;

		IEnumerator Wait() {
			while (!ready) {
				yield return null;
			}
		}

		public static implicit operator Coroutine(Subscription sub) {
			return CoroutineHost.Instance.StartCoroutine (sub.Wait ());
		}

		public Subscription ()
		{
		}
	}
}

