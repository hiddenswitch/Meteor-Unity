using System;
using System.Collections;
using UnityEngine;
using Meteor.Extensions;

namespace Meteor
{
	public class Method : IMethod
	{
		public Method() {
			Updated = false;
		}

		public MethodMessage Message;
		public event MethodHandler OnUntypedResponse;

		public static Method Call(string name, params object[] args) {
			return LiveData.Instance.Call (name, args);
		}

		public virtual void Callback(Error error, object response)
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

		public Error Error {
			get;
			protected set;
		}

		public bool Updated {
			get;
			set;
		}

		#endregion

		protected bool complete;

		protected void completed(Error error, object response) {
			UntypedResponse = response;
			Error = error;
			complete = true;
		}

		protected virtual IEnumerator Execute() {
			// Send the method message over the wire.
			LiveData.Instance.Send (Message);

			// Wait until we get a response.
			while (!(complete && Updated)) {
				yield return null;
			}

			// Clear the completed handler.
			OnUntypedResponse -= completed;

			yield break;
		}

		public static implicit operator Coroutine(Method method) {
			if (method == null) {
				return null;
			}
			method.OnUntypedResponse += method.completed;
			return CoroutineHost.Instance.StartCoroutine (method.Execute ());
		}

		protected sealed class MethodHost : MonoSingleton<MethodHost> {}
	}

	public class Method<TResponseType> : Method
	{
		public Method() {
			Updated = false;
		}

		public static new Method<TResponseType> Call(string name, params object[] args) {
			return LiveData.Instance.Call<TResponseType> (name, args);
		}

		public event MethodHandler<TResponseType> OnResponse;

		public TResponseType Response
		{
			get {
				return UntypedResponse == null ? default(TResponseType) : (TResponseType)UntypedResponse;
			}
			private set {
				UntypedResponse = value;
			}
		}

		#region IMethod implementation

		public override void Callback(Error error, object response)
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

		protected void typedCompleted(Error error, TResponseType response) {
			Response = response;
			Error = error;
			complete = true;
		}

		protected override IEnumerator Execute() {
			// Send the method message over the wire.
			LiveData.Instance.Send (Message);

			// Wait until we get a response.
			while (!(complete && Updated)) {
				yield return null;
			}

			// Clear the completed handler.
			OnResponse -= typedCompleted;

			yield break;
		}

		public static implicit operator Coroutine(Method<TResponseType> method) {
			if (method == null) {
				return null;
			}
			method.OnResponse += method.typedCompleted;
			return CoroutineHost.Instance.StartCoroutine (method.Execute ());
		}

		#endregion
	}
}

