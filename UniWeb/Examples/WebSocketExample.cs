using UnityEngine;
using System.Collections;

public class WebSocketExample : MonoBehaviour {

	IEnumerator Start () {
		yield return null;
		
		var ws = new HTTP.WebSocket();
		StartCoroutine(ws.Dispatcher());
		
		Debug.Log(ws);
		
		ws.Connect("http://echo.websocket.org");
		
		ws.OnTextMessageRecv += (e) => {
			Debug.Log("Reply came from server -> " + e);
		};
		ws.Send("Hello");
		
		ws.Send("Hello again!");
		
		ws.Send("Goodbye");
	}
	
	
}
