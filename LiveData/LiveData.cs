using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Extensions;
using HTTP;

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
		/// A successful connection event. The first argument is the session ID.
		/// </summary>
		public event Action<string> OnConnected;
		public event Action OnReconnected;

		public LiveData()
		{
			Connector = new WebSocket();
			Collections = new CollectionCollection();
			Subscriptions = new SubscriptionCollection();
			methods = new Dictionary<string, IMethod>();

			uniqueId = 1;
		}

		/// <summary>
		/// Connect to the specified Meteor server.
		/// </summary>
		/// <param name="url">URL.</param>
		public Coroutine Connect(string url)
		{
			return CoroutineHost.Instance.StartCoroutine (ConnectCoroutine (url));
		}

		void HandleOnConnected (string obj)
		{
			Connected = true;
			TimedOut = false;
		}

		public bool Connected {
			get;
			private set;
		}

		public bool TimedOut;

		private IEnumerator TimeoutCoroutine(float timeout) {
			yield return new WaitForSeconds (timeout);
			if (!Connected) {
				TimedOut = true;
			}
		}

		private IEnumerator ConnectCoroutine(string url) {
			TimedOut = false;
			OnConnected += HandleOnConnected;
			CoroutineHost.Instance.StartCoroutine (TimeoutCoroutine (5.0f));
			CoroutineHost.Instance.StartCoroutine(Connector.Dispatcher());
			Connector.Connect(url);
			Connector.OnTextMessageRecv += HandleOnTextMessageRecv;
			Connector.Send (ConnectMessage.connectMessage);

			while (!Connected) {
				if (TimedOut) {
					yield break;
				}
				yield return null;
			}

			yield break;
		}

		/// <summary>
		/// Calls the given method. Calls handler with the error and response.
		/// </summary>
		/// <param name="methodName">Method name.</param>
		/// <param name="handler">Handler.</param>
		/// <param name="arguments">Arguments.</param>
		public Method Call(string methodName, params object[] arguments)
		{
			string requestId = string.Format("{0}-{1}",methodName,this.NextId());

			Method method = new Method () {
				Message = new MethodMessage () {
					method = methodName,
					Params = arguments,
					id = requestId
				}
			};

			methods[requestId] = method;

			return method;
		}

		/// <summary>
		/// Calls the given method. Calls handler with the error and strongly typed response.
		/// </summary>
		/// <param name="methodName">Method name.</param>
		/// <param name="handler">Handler.</param>
		/// <param name="arguments">Arguments.</param>
		/// <typeparam name="ResponseType">The type of the response object.</typeparam>
		public Method<TResponseType> Call<TResponseType>(string methodName, params object[] arguments)
			where TResponseType : new()
		{
			string requestId = string.Format("{0}-{1}",methodName,this.NextId());

			Method<TResponseType> method = new Method<TResponseType> () {
				Message = new MethodMessage () {
					method = methodName,
					Params = arguments,
					id = requestId
				}
			};
			methods[requestId] = method;

			return method;
		}

		public void Send(object obj) {
			Connector.Send (obj.Serialize ());
		}


		#region IClient implementation
		/// <summary>
		/// Subscribe to the given publishing endpoint.
		/// </summary>
		/// <param name="publishName">The name of the publishing endpoint.</param>
		/// <param name="arguments">Arguments to the publish function.</param>
		/// <typeparam name="RecordType">The type of the record in the collection.</typeparam>
		public Subscription Subscribe(string publishName, params object[] arguments)
		{
			string requestId = string.Format("{0}-{1}",publishName,this.NextId());

			// Setup backing store.
			if (Subscriptions.Contains(requestId))
			{
				return Subscriptions [requestId];
			} else {
				Subscriptions.Add(new Subscription () {
					name = requestId
				});
			}

			Send(new SubscribeMessage() {
				name = publishName,
				Params = arguments,
				id = requestId
			});

			return Subscriptions [requestId];
		}
		#endregion
		private int NextId()
		{
			return uniqueId++;
		}

		public int GetCurrentRequestId()
		{
			return uniqueId;
		}

		public void Close()
		{
			Connector.Close (WebSocket.CloseEventCode.CloseEventCodeNormalClosure, null);
		}

		void HandleOnTextMessageRecv (string socketMessage)
		{
			Debug.Log (socketMessage);
			IDictionary m = socketMessage.Deserialize() as IDictionary;
			if (m == null) {
				return;
			}

			var msg = m ["msg"] as string;

			switch (msg)
			{
			case AddedMessage.added:
				var collection = m ["collection"] as string;
				if (Collections.Contains(collection))
				{
					Collections[collection].Added(socketMessage);
				}
				break;
			case ChangedMessage.changed:
				ChangedMessage cm = socketMessage.Deserialize<ChangedMessage>();
				if (Collections.Contains(cm.collection))
				{
					Collections[cm.collection].Changed(cm.id, cm.cleared, cm.fields);
				}
				break;
			case RemovedMessage.removed:
				RemovedMessage rm = socketMessage.Deserialize<RemovedMessage>();
				if (Collections.Contains(rm.collection))
				{
					Collections[rm.collection].Removed(rm.id);
				}
				break;
			case ReadyMessage.ready:
				ReadyMessage readym = socketMessage.Deserialize<ReadyMessage>();
				foreach (string sub in readym.subs) {
					if (Subscriptions.Contains(sub)) {
						Subscriptions [sub].ready = true;
					}
				}
				break;
			case ConnectedMessage.connected:
				ConnectedMessage connm = socketMessage.Deserialize<ConnectedMessage>();
				if (OnConnected != null)
				{
					OnConnected(connm.session);
				}
				break;
			case ResultMessage.result:
				ResultMessage resultm = socketMessage.Deserialize<ResultMessage>();
				if (methods.ContainsKey(resultm.id)) {
					methods[resultm.id].Callback(resultm.error, resultm.methodResult);
				} else {
					Debug.LogError ("DDPClient.ProcessQueue: Result ID not found.");
				}
				break;
			case "updated":
				break;
			default:
				if (!socketMessage.Contains("server_id")) {
					Debug.Log(string.Format("DDPClient.ProcessQueue: Unhandled message.\nMessage:\n{0}",socketMessage));
				}
				break;
			}
		}
	}
}
