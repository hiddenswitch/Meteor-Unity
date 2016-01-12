using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Meteor.Extensions;
using WebSocketSharp.Net.WebSockets;

namespace Meteor
{
	public class LiveData : ILiveData
	{
		WebSocket Connector;
		int uniqueId;

		public CollectionCollection Collections { get; private set; }

		public SubscriptionCollection Subscriptions { get; private set; }

		Dictionary<string, IMethod> methods;
		string serverId;

		static LiveData _instance;

		public static LiveData Instance {
			get {
				if (_instance == null) {
					_instance = new LiveData ();
				}

				return _instance;
			}
		}

		/// <summary>
		/// Enable packet logging.
		/// </summary>
		/// <value><c>true</c> if logging is enabled; otherwise, <c>false</c>.</value>
		public bool Logging {
			get;
			set;
		}

		/// <summary>
		/// A successful connection event. The first argument is the session ID.
		/// </summary>
		public event Action<string> WillConnect;
		public event Action<string> DidConnect;
		public event Action OnReconnected;

		public LiveData ()
		{
			Connector = null;
			Collections = new CollectionCollection ();
			Subscriptions = new SubscriptionCollection ();
			methods = new Dictionary<string, IMethod> ();

			uniqueId = 1;
		}

		/// <summary>
		/// Connect to the specified Meteor server.
		/// </summary>
		/// <param name="url">URL.</param>
		public Coroutine Connect (string url)
		{
			return CoroutineHost.Instance.StartCoroutine (ConnectCoroutine (url));
		}

		void HandleWillConnect (string obj)
		{
			Connected = true;
			TimedOut = false;
		}

		public bool Connected {
			get;
			private set;
		}

		public bool TimedOut;

		private IEnumerator TimeoutCoroutine (float timeout)
		{
			yield return new WaitForSeconds (timeout);
			if (!Connected) {
				TimedOut = true;
			}
		}

		void SendConnectMessage (string version = null)
		{
			var message = "";

			if (version == null) {
				message = ConnectMessage.connectMessage;
			} else {
				message = (new ConnectMessage () { version = version }).Serialize ();
			}

			Connector.Send (System.Text.Encoding.UTF8.GetBytes (message));
		}

		private IEnumerator ConnectCoroutine (string url)
		{
			TimedOut = false;
			WillConnect += HandleWillConnect;
			CoroutineHost.Instance.StartCoroutine (TimeoutCoroutine (5.0f));
			CoroutineHost.Instance.StartCoroutine (Dispatcher ());
			Connector = new WebSocket (new Uri (url));
			yield return Connector.Connect ();
			SendConnectMessage ();

			while (!Connected) {
				if (TimedOut) {
					yield break;
				}
				yield return null;
			}

			yield break;
		}

		private IEnumerator Dispatcher ()
		{
			while (!Connected) {
				yield return null;
			}

			while (Connected) {
				var received = Connector.RecvString ();
				if (received == null) {
					yield return null;
				}
				HandleOnTextMessageRecv (received);
			}
		}

		/// <summary>
		/// Calls the given method. Calls handler with the error and response.
		/// </summary>
		/// <param name="methodName">Method name.</param>
		/// <param name="handler">Handler.</param>
		/// <param name="arguments">Arguments.</param>
		public Method Call (string methodName, params object[] arguments)
		{
			string requestId = string.Format ("{0}-{1}", methodName, this.NextId ());

			Method method = new Method () {
				Message = new MethodMessage () {
					method = methodName,
					Params = arguments,
					id = requestId
				}
			};

			methods [requestId] = method;

			return method;
		}

		/// <summary>
		/// Calls the given method. Calls handler with the error and strongly typed response.
		/// </summary>
		/// <param name="methodName">Method name.</param>
		/// <param name="handler">Handler.</param>
		/// <param name="arguments">Arguments.</param>
		/// <typeparam name="ResponseType">The type of the response object.</typeparam>
		public Method<TResponseType> Call<TResponseType> (string methodName, params object[] arguments)
		{
			string requestId = string.Format ("{0}-{1}", methodName, this.NextId ());

			Method<TResponseType> method = new Method<TResponseType> () {
				Message = new MethodMessage () {
					method = methodName,
					Params = arguments,
					id = requestId
				}
			};
			methods [requestId] = method;

			return method;
		}

		public void Send (object obj)
		{
			var s = System.Text.Encoding.UTF8.GetBytes (obj.Serialize ());

			if (Logging) {
				Debug.Log (s);
			}

			Connector.Send (s);
		}


		#region IClient implementation

		/// <summary>
		/// Subscribe to the given publishing endpoint.
		/// </summary>
		/// <param name="publishName">The name of the publishing endpoint.</param>
		/// <param name="arguments">Arguments to the publish function.</param>
		/// <typeparam name="RecordType">The type of the record in the collection.</typeparam>
		public Subscription Subscribe (string publishName, params object[] arguments)
		{
			string requestId = string.Format ("{0}-{1}", publishName, this.NextId ());

			// Setup backing store.
			if (Subscriptions.Contains (requestId)) {
				return Subscriptions [requestId];
			} else {
				Subscriptions.Add (new Subscription () {
					name = requestId
				});
			}

			Send (new SubscribeMessage () {
				name = publishName,
				Params = arguments,
				id = requestId
			});

			return Subscriptions [requestId];
		}

		#endregion

		private int NextId ()
		{
			return uniqueId++;
		}

		public int GetCurrentRequestId ()
		{
			return uniqueId;
		}

		public void Close ()
		{
			Connector.Close ();
		}

		void HandleOnTextMessageRecv (string socketMessage)
		{
			if (Logging) {
				Debug.Log (socketMessage);
			}

			IDictionary m = socketMessage.Deserialize () as IDictionary;
			if (m == null) {
				return;
			}

			var msg = m ["msg"] as string;

			switch (msg) {
			case AddedMessage.added:
				var collection = m ["collection"] as string;
				if (Collections.Contains (collection)) {
					Collections [collection].Added (socketMessage);
				} else {
					Debug.Log (string.Format ("LiveData: Unhandled record add. Creating a collection to handle it.\nMessage:\n{0}", socketMessage));
					var handlingCollection = Meteor.Collection<MongoDocument>.Create (collection) as ICollection;
					handlingCollection.Added (socketMessage);
				}
				break;
			case ChangedMessage.changed:
				ChangedMessage cm = socketMessage.Deserialize<ChangedMessage> ();
				if (Collections.Contains (cm.collection)) {
					Collections [cm.collection].Changed (cm.id, cm.cleared, cm.fields);
				} else {
					Debug.LogWarning (string.Format ("LiveData: Unhandled record change. Cannot recover this record later.\nMessage:\n{0}", socketMessage));
				}
				break;
			case RemovedMessage.removed:
				RemovedMessage rm = socketMessage.Deserialize<RemovedMessage> ();
				if (Collections.Contains (rm.collection)) {
					Collections [rm.collection].Removed (rm.id);
				} else {
					Debug.LogWarning (string.Format ("LiveData: Unhandled record remove.\nMessage:\n{0}", socketMessage));
				}
				break;
			case ReadyMessage.ready:
				ReadyMessage readym = socketMessage.Deserialize<ReadyMessage> ();
				foreach (string sub in readym.subs) {
					if (Subscriptions.Contains (sub)) {
						Subscriptions [sub].ready = true;
					} else {
						Debug.LogError (string.Format ("LiveData: A subscription ready message was received, but the subscription could not be found.\nSubscription: {0}", sub));
					}
				}
				break;
			case ConnectedMessage.connected:
				ConnectedMessage connm = socketMessage.Deserialize<ConnectedMessage> ();

				if (WillConnect != null) {
					WillConnect (connm.session);
				}

				if (DidConnect != null) {
					DidConnect (connm.session);
				}

				break;
			case FailedMessage.failed:
				FailedMessage failedMessage = socketMessage.Deserialize<FailedMessage> ();
				SendConnectMessage (failedMessage.version);
				break;
			case ResultMessage.result:
				ResultMessage resultm = null;
				resultm = socketMessage.Deserialize<ResultMessage> ();
				if (methods.ContainsKey (resultm.id)) {
					methods [resultm.id].Callback (resultm.error, resultm.methodResult);
				} else {
					Debug.LogError (string.Format ("LiveData: A result message was received, but the method could not be found.\nMethod: {0}", resultm.id));
				}
				break;
			case UpdatedMessage.updated:
				UpdatedMessage updatedm = socketMessage.Deserialize<UpdatedMessage> ();
				foreach (var method in updatedm.methods) {
					if (methods.ContainsKey (method)) {
						methods [method].Updated = true;
					} else {
						Debug.LogError (string.Format ("LiveData: An updated message was received, but the method could not be found.\nMethod: {0}", method));
					}
				}
				break;
			case PingMessage.ping:
				PingMessage pingMessage = socketMessage.Deserialize<PingMessage> ();
				var pongMessage = new PongMessage () {
					id = pingMessage.id
				};
				Send (pingMessage);
				break;
			default:
				if (!socketMessage.Contains ("server_id")) {
					Debug.LogWarning (string.Format ("LiveData: Unhandled message.\nMessage:\n{0}", socketMessage));
				}
				break;
			}
		}
	}
}
