using System;
using UnityEngine;
using System.Collections;
using Meteor.Internal;

namespace Meteor
{
	/// <summary>
	/// A subscription handle.
	/// </summary>
	public class Subscription
	{
		bool _ready;
		/// <summary>
		/// Gets a value specifying whether or not this subscription is ready (all the documents on the first request have been received).
		/// </summary>
		/// <value><c>true</c> if ready; otherwise, <c>false</c>.</value>
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

		/// <summary>
		/// The name of the subscription. Corresponds to your <code>Meteor.publish(name, ...)</code> statement in your Meteor code.
		/// </summary>
		public string name;
		/// <summary>
		/// A unique identifier for the request used to fulfill this subscription. You generally will not need this.
		/// </summary>
		public string requestId;
		/// <summary>
		/// The arguments used in this subscription.
		/// </summary>
		public object[] args;

		IEnumerator Wait() {
			while (!ready) {
				yield return null;
			}
		}

		public static implicit operator Coroutine(Subscription sub) {
			return CoroutineHost.Instance.StartCoroutine (sub.Wait ());
		}

		/// <summary>
		/// Subscribe to a record set. Returns a handle that provides a ready property.
		/// You must call <code>yield return (Coroutine)subscribeInstance;</code> in an IEnumerator/Coroutine to actually execute the subscription.
		/// </summary>
		/// <example>
		/// Example:
		/// <code>
		/// var subscriptionInstance = Meteor.Subscription.Subscribe("PublishName");
		/// yield return (Coroutine)subscriptionInstance;
		/// </code>
		/// </example>
		/// <param name="name">Name. Corresponds to your <code>Meteor.publish(name, ...)</code> statement in your Meteor code.</param>
		/// <param name="args">Arguments. Corresponds to the arguments in the function provided to your <code>Meteor.publish</code> call.</param>
		public static Subscription Subscribe(string name, params object[] args) {
			return LiveData.Instance.Subscribe (name, args);
		}

		public Subscription ()
		{
		}
	}
}

