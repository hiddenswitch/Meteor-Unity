using System;

public interface IClient {
	event Action OnReady;
	void Connect();
	Net.DDP.Client.ILiveData DDPClient {get; set;}
}

