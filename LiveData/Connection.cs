using System;
using UnityEngine;
using System.Collections;
using Meteor.Internal;

namespace Meteor
{
	/// <summary>
	/// Manages your Meteor connection.
	/// </summary>
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

		/// <summary>
		/// The URL to connect to.
		/// Note, Meteor hosted sites do NOT support <code>wss</code> (secured Webscokets) protocols, while Modulus hosted sites do.
		/// </summary>
		/// <example>
		/// Examples:
		/// <code>
		/// ws://localhost:3000/websocket
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// wss://yourmeteorapp.com/websocket
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// ws://yourexampleapp.meteor.com/websocket
		/// </code>
		/// </example>
		/// <value>The URL.</value>
		public static string Url {
			get;
			set;
		}

		/// <summary>
		/// Connects to the specified URL.
		/// Note, Meteor hosted sites do NOT support <code>wss</code> (secured Webscokets) protocols, while Modulus hosted sites do.
		/// </summary>
		/// <example>
		/// Examples:
		/// <code>
		/// yield return Meteor.Connection.Connect("ws://localhost:3000/websocket");
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// yield return Meteor.Connection.Connect("wss://yourmeteorapp.com/websocket");
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// yield return Meteor.Connection.Connect("ws://yourexampleapp.meteor.com/websocket");
		/// </code>
		/// </example>
		/// <param name="url">URL.</param>
		public static Coroutine Connect (string url)
		{
			Url = url;
			return LiveData.Instance.Connect (url);
		}

		/// <summary>
		/// Reconnect to the server. This is useful to call in an OnApplicationPause(bool pause) when pause is false (resuming)
		/// </summary>
		public static Coroutine Reconnect ()
		{
			return LiveData.Instance.Reconnect ();
		}

		/// <summary>
		/// Should logging of all messages be enabled for this connection?
		/// </summary>
		/// <value><c>true</c> if logging; otherwise, <c>false</c>.</value>
		public static bool Logging {
			get {
				return LiveData.Instance.Logging;
			}
			set {
				LiveData.Instance.Logging = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether you have successfully connected to the Meteor server.
		/// </summary>
		/// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
		public static bool Connected {
			get {
				return LiveData.Instance.Connected;
			}
		}

		/// <summary>
		/// Raised when we succcessfully connect.
		/// </summary>
		public static event Action DidConnect;
	}
}

