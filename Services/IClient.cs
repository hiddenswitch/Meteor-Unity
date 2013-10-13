using System;

public interface IClient {
	event Action OnReady;
	void Connect();
	Net.DDP.Client.IMeteorClient DDPClient {get; set;}
}

