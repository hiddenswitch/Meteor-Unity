using System;
using System.Collections;
using UnityEngine;
using Extensions;

namespace Net.DDP.Client
{
	public class Method : IMethod
	{
		public Messages.MethodMessage Message;
		public event MethodHandler OnUntypedResponse;

		public virtual void Callback(Meteor.Error error, object response)
		{
			if (OnUntypedResponse != null)
			{
				OnUntypedResponse(error, response);
			}
		}

		public virtual Type ResponseType {
			get {
				return typeof(IDictionary);
			}
		}

		#region IMethod implementation

		public object UntypedResponse {
			get;
			protected set;
		}

		public Meteor.Error Error {
			get;
			protected set;
		}

		#endregion

		protected sealed class MethodHost : MonoBehaviour {
			private static MethodHost _instance;

			public static MethodHost Instance {
				get {
					if (((object)_instance) == null) {
						_instance = (new GameObject ()).AddComponent<MethodHost> ();
					}

					return _instance;
				}
			}
		}
	}

	public class Method<TResponseType> : Method
		where TResponseType : new()
	{
		public event MethodHandler<TResponseType> OnResponse;

		private bool complete;
		public TResponseType Response
		{
			get {
				return UntypedResponse == null ? default(TResponseType) : (TResponseType)UntypedResponse;
			}
			set {
				UntypedResponse = value;
			}
		}

		#region IMethod implementation

		public override void Callback(Meteor.Error error, object response)
		{
			TResponseType r = response.Coerce<TResponseType>();

			if (OnResponse != null)
			{
				OnResponse(error, r);
			} else {
				base.Callback (error, response);
			}
		}

		public override Type ResponseType {
			get {
				return typeof(TResponseType);
			}
		}

		public static implicit operator Coroutine(Method<TResponseType> method) {
			method.OnResponse += method.completed;
			return MethodHost.Instance.StartCoroutine (method.Execute ());
		}

		private void completed(Meteor.Error error, TResponseType response) {
			Response = response;
			Error = error;
			complete = true;
		}

		private IEnumerator Execute() {
			// Send the method message over the wire.
			LiveData.Instance.Connector.Send (Message.Serialize ());

			// Wait until we get a response.
			while (!complete) {
				yield return null;
			}

			// Clear the completed handler.
			OnResponse -= completed;

			yield break;
		}

		#endregion
	}
}

