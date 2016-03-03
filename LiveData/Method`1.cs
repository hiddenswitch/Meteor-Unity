using System.Collections;
using UnityEngine;
using System;
using Meteor.Extensions;
using Meteor.Internal;

namespace Meteor
{
	/// <summary>
	/// Methods are remote functions that Meteor clients can invoke.
	/// Use <see cref="Method.Call"/> to call a method by name with the specified arguments. Returns a Method object.
	/// You must call <code>yield return (Coroutine)instance; </code> inside an IEnumerator/Coroutine function to actually execute the call. You can also call <code>ExecuteAsync</code> on the
	/// method instance to execute asynchronously outside of an IEnumerator.
	/// The name of the method to call corresponds to the key in your <code>Meteor.Methods({key: function value()})</code> statement.
	/// </summary>
	/// <example>
	/// Examples:
	/// <code>
	/// var methodCall = Method<string>.Call("MyMethodWhichREturnsString");
	/// yield return (Coroutine)methodCall;
	/// string result = methodCall.Result;
	/// </code>
	/// </example>
	/// <example>
	/// <code>
	/// var callAndForgetAboutResult = Method<string>.Call("MyMethodWhichReturnsString").ExecuteAsync();
	/// </code>
	/// </example>
	public class Method<TResponseType> : Method
	{
		public Method ()
		{
			Updated = false;
		}

		/// <summary>
		/// Calls a method by name with the specified arguments. Returns a Method`1 object.
		/// You must call <code>yield return (Coroutine)instance; </code> inside an IEnumerator/Coroutine function to actually execute the call. You can also call <code>ExecuteAsync</code> on the
		/// method instance to execute asynchronously outside of an IEnumerator.
		/// </summary>
		/// <example>
		/// Examples:
		/// <code>
		/// var methodCall = Method<string>.Call("MyMethodWhichREturnsString");
		/// yield return (Coroutine)methodCall;
		/// string result = methodCall.Result;
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// var callAndForgetAboutResult = Method<string>.Call("MyMethodWhichReturnsString").ExecuteAsync();
		/// </code>
		/// </example>
		/// <param name="name">Name of the method to call. Corresponds to the key in your Meteor.Methods({key: function value()}) statement.</param>
		/// <param name="args">Arguments. This can be an array of any typed objects. They will be faithfully converted to Meteor, including e.g. Vector3's. It is recommended to use
		/// a single argument with a typed class in C# instead of multiple primitive arguments.</param>
		public static new Method<TResponseType> Call (string name, params object[] args)
		{
			var methodCall = LiveData.Instance.Call<TResponseType> (name, args);
			methodCall.Name = name;
			return methodCall;
		}

		/// <summary>
		/// This event is raised when a response is received for this method.
		/// </summary>
		public event MethodHandler<TResponseType> OnResponse;

		/// <summary>
		/// Gets the value of the response of this method.
		/// </summary>
		/// <value>The response.</value>
		public TResponseType Response {
			get {
				return UntypedResponse == null ? default(TResponseType) : (TResponseType)UntypedResponse;
			}
			private set {
				UntypedResponse = value;
			}
		}

		#region IMethod implementation

		/// <summary>
		/// Used by LiveData to signal that a response has arrived for this method call.
		/// </summary>
		/// <param name="error">Error.</param>
		/// <param name="response">Response.</param>
		public override void Callback (Error error, object response)
		{
			TResponseType r = default(TResponseType);
			try {
				if (response != null) {
					r = response.Coerce<TResponseType> ();
				} else if (response == null
				           && typeof(TResponseType).IsValueType
				           && error == null) {
					Debug.LogError (string.Format ("Returned null when a value type was expected and no error was found.\nMethod: {0}", this));
				}
				#pragma warning disable 0168
			} catch (JsonFx.Json.JsonTypeCoercionException ex) {
				if (error == null) {
					Debug.LogWarning (string.Format ("Failed to convert method response type to specified type in call and no error was found.\nMethod: {0}", this));
				}
			}
			#pragma warning restore 0168

			if (OnResponse != null) {
				OnResponse (error, r);
			} else {
				base.Callback (error, response);
			}
		}

		/// <summary>
		/// The type of the response.
		/// </summary>
		/// <value>The type of the response.</value>
		public override Type ResponseType {
			get {
				return typeof(TResponseType);
			}
		}

		protected void typedCompleted (Error error, TResponseType response)
		{
			Response = response;
			Error = error;
			complete = true;
		}

		protected override IEnumerator Execute ()
		{
			// Send the method message over the wire.
			while (!Connection.Connected
			       && !LiveData.Instance.TimedOut) {
				yield return null;
			}

			if (LiveData.Instance.TimedOut) {
				Callback (new Error () { error = -1, details = "Connection timed out." }, null);
				yield break;
			}

			LiveData.Instance.Send (Message);

			// Wait until we get a response.
			while (!(complete && Updated)) {
				yield return null;
			}

			// Clear the completed handler.
			OnResponse -= typedCompleted;

			yield break;
		}

		/// <summary>
		/// Executes the method immediately, using a coroutine host global to your Unity game.
		/// </summary>
		/// <returns>A coroutine reference.</returns>
		/// <param name="callback">An optional callback when this method returns (which may never happen).</param>
		public virtual Coroutine ExecuteAsync (MethodHandler<TResponseType> callback = null)
		{
			this.OnResponse += callback;
			return (Coroutine)this;
		}

		/// <summary>
		/// Casts this method call instance to a Coroutine, allowing you to yield and execute it.
		/// </summary>
		/// <param name="method">Method.</param>
		public static implicit operator Coroutine (Method<TResponseType> method)
		{
			if (method == null) {
				return null;
			}
			method.OnResponse += method.typedCompleted;
			return CoroutineHost.Instance.StartCoroutine (method.Execute ());
		}

		public override string ToString ()
		{
			return string.Format ("[Method: Name={0}, Response={1}, ResponseType={2}]", Name, Response, ResponseType.Name);
		}

		#endregion
	}
}