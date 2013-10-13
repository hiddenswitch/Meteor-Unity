using UnityEngine;
using System.Collections;

public class SocketIOHandler {
	
	 public System.Action<SocketIOMessage> OnDisconnect;
	
	 public System.Action<SocketIOMessage>  OnConnect;
	
	 public System.Action<SocketIOMessage>  OnHearbeat;
	
	 public System.Action<SocketIOMessage>  OnMessage;
	
	 public System.Action<SocketIOMessage, object>  OnJSONMessage;
	
	 public System.Action<SocketIOMessage, string, ArrayList>  OnEvent;
	
	 public System.Action<SocketIOMessage>  OnACK;
	
	 public System.Action<SocketIOMessage>  OnError;
	
	 public System.Action<SocketIOMessage>  OnNoop;
	
}
