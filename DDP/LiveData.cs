using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Net.DDP.Client.Messages;
using Extensions;

namespace Net.DDP.Client
{
	public class LiveData : ILiveData
	{
		protected sealed class LiveDataHost : MonoBehaviour {
			private static LiveDataHost _instance;

			public static LiveDataHost Instance {
				get {
					if (((object)_instance) == null) {
						_instance = (new GameObject ("LiveData Host")).AddComponent<LiveDataHost> ();
					}

					return _instance;
				}
			}
		}

		public LiveConnection Connector;
		int uniqueId;
		Dictionary<string, Net.DDP.Client.ICollection> collections;
		Dictionary<string, List<string>> subscriptionsToCollections;
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
			Connector = new LiveConnection(this);
			jsonItemsQueue = new Queue<string>();
			collections = new Dictionary<string, Net.DDP.Client.ICollection>();
			subscriptionsToCollections = new Dictionary<string, List<string>>();
			methods = new Dictionary<string, IMethod>();

			enqueuedEvent = new ManualResetEvent(false);
			workerThread = new Thread(new ThreadStart(ProcessQueue));
			workerThread.Start();

			uniqueId = 1;
		}

		/// <summary>
		/// Connect to the specified Meteor server.
		/// </summary>
		/// <param name="url">URL.</param>
		public Coroutine Connect(string url)
		{
			OnConnected += HandleOnConnected;

			return LiveDataHost.Instance.StartCoroutine (ConnectCoroutine (url));
		}

		void HandleOnConnected (string obj)
		{
			connected = true;
		}

		bool connected;
		private IEnumerator ConnectCoroutine(string url) {
			Connector.Connect(url);

			while (!connected) {
				yield return null;
			}

			OnConnected -= HandleOnConnected;

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


		#region IClient implementation
		/// <summary>
		/// Subscribe to the given publishing endpoint.
		/// </summary>
		/// <param name="collectionName">The expected collection name.</param>
		/// <param name="publishName">The name of the publishing endpoint.</param>
		/// <param name="arguments">Arguments to the publish function.</param>
		/// <typeparam name="RecordType">The type of the record in the collection.</typeparam>
		public Collection<TRecordType> Subscribe<TRecordType>(string collectionName, string publishName, params object[] arguments)
			where TRecordType : new()
		{
			string requestId = string.Format("{0}-{1}",publishName,this.NextId());

			// Setup backing store.
			if (!subscriptionsToCollections.ContainsKey(requestId))
			{
				subscriptionsToCollections.Add(requestId, new List<string>());
			}

			subscriptionsToCollections[requestId].Add(collectionName);

			Collection<TRecordType> collection;

			if (collections.ContainsKey(collectionName))
			{
				collection = collections[collectionName] as Collection<TRecordType>;
			} else {
				collection = new Collection<TRecordType>() {
					name = collectionName,
					Client = this
				};
				collections[collectionName] = collection;
			}

			Connector.Send(new SubscribeMessage() {
				name = publishName,
				Params = arguments,
				id = requestId
			}.Serialize());

			return collection;
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
			Connector.Close();
		}

		static ManualResetEvent enqueuedEvent;
		static Thread workerThread;
		Queue<string> jsonItemsQueue;
		string currentJsonItem;

		public void QueueMessage(string jsonItem)
		{
			lock (jsonItemsQueue) {
				jsonItemsQueue.Enqueue(jsonItem);
				enqueuedEvent.Set();
			}
			RestartThread();
		}

		private bool Dequeue()
		{
			lock (jsonItemsQueue) {
				if (jsonItemsQueue.Count > 0) {
					enqueuedEvent.Reset();
					currentJsonItem = jsonItemsQueue.Dequeue();
				} else {
					return false;
				}

				return true;
			}
		}

		public void RestartThread()
		{
			if (workerThread.ThreadState == ThreadState.Stopped) {
				workerThread.Abort();
				workerThread = new Thread(new ThreadStart(ProcessQueue));
				workerThread.Start();
			}
		}

		private void ProcessQueue()
		{
			while (Dequeue()) {
				Debug.Log(currentJsonItem);
				IDictionary message = currentJsonItem.Deserialize() as IDictionary;
				if (message == null) {
					return;
				}

				Message m = message.Coerce<Message>();

				switch (m.msg)
				{
				case AddedMessage.added:
					AddedMessage am = message.Coerce<AddedMessage>();
					if (collections.ContainsKey(am.collection))
					{
						collections[am.collection].Added(am.id, am.fields);
					}
					break;
				case ChangedMessage.changed:
					ChangedMessage cm = message.Coerce<ChangedMessage>();
					if (collections.ContainsKey(cm.collection))
					{
						collections[cm.collection].Changed(cm.id, cm.cleared, cm.fields);
					}
					break;
				case RemovedMessage.removed:
					RemovedMessage rm = message.Coerce<RemovedMessage>();
					if (collections.ContainsKey(rm.collection))
					{
						collections[rm.collection].Removed(rm.id);
					}
					break;
				case ReadyMessage.ready:
					ReadyMessage readym = message.Coerce<ReadyMessage>();
					foreach (string sub in readym.subs) {
						foreach (string collection in subscriptionsToCollections[sub])
						{
							collections[collection].SubscriptionReady(sub);
						}
					}
					break;
				case ConnectedMessage.connected:
					ConnectedMessage connm = message.Coerce<ConnectedMessage>();
					if (OnConnected != null)
					{
						OnConnected(connm.session);
					}
					break;
				case ResultMessage.result:
					ResultMessage resultm = message.Coerce<ResultMessage>();
					if (methods.ContainsKey(resultm.id)) {
						methods[resultm.id].Callback(resultm.error, resultm.methodResult);
					} else {
						Debug.LogError ("DDPClient.ProcessQueue: Result ID not found.");
					}
					break;
				case "updated":
					break;
				default:
					if (!message.Contains("server_id")) {
						Debug.Log(string.Format("DDPClient.ProcessQueue: Unhandled message.\nMessage:\n{0}",message.Serialize()));
					}
					break;
				}
			}
		}
	}
}
