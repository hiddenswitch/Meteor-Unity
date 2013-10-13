using System;
using UnityEngine;
using System.Collections;
using Net.DDP.Client;

[RequireComponent(typeof(Accounts))]
public class Client : MonoBehaviour, IClient {
	public string debugHost = "localhost:3000";
	public string productionHost = "www.redactedonline.com";
	LiveData _client = LiveData.Instance;
	public Accounts accounts;

	void Awake()
	{
		if (accounts == null)
		{
			accounts = gameObject.GetComponent<Accounts> ();
		}

		if (accounts == null)
		{
			accounts = gameObject.AddComponent<Accounts> ();
		}
	}

	// Use this for initialization
	void Start () {
		_client.OnConnected += HandleOnConnected;
		accounts.Client = _client;
		Connect ();
	}

	void HandleOnConnected (string obj)
	{
		accounts.LoginWith("supertest105", "easy");

		if (OnReady != null)
		{
			OnReady ();
		}
	}

	// Update is called once per frame
	void Update () {

	}

	void OnDestroy()
	{
		_client.Close();
	}

	void OnApplicationQuit()
	{
		_client.Close();
	}

	#region IClient Implementation

	public event Action OnReady;

	public void Connect ()
	{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
		DDPClient.Connect(debugHost);
#else
		DDPClient.Connect(productionHost);
#endif
	}

	public Net.DDP.Client.IMeteorClient DDPClient {
		get {
			return _client;
		}
		set {
			throw new NotSupportedException ();
		}
	}

	#endregion
}

