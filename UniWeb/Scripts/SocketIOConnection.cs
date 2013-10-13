using UnityEngine;
using System.Collections;
using HTTP;
using System.Linq;

public class SocketIOConnection : MonoBehaviour
{
	public string url;
	public string sid;
	public float heartbeatTimeout;
	public float closingTimeout;
	public string[] transports;
	public SocketIOHandler handler = new SocketIOHandler ();
	WebSocket socket;
	int msgUid = 0;
	
	/// <summary>
	/// Gets a value indicating whether this <see cref="SocketIOConnection"/> is ready.
	/// </summary>
	/// <value>
	/// <c>true</c> if ready; otherwise, <c>false</c>.
	/// </value>
	public bool Ready {
		get {
			return socket != null;
		}
	}
	
	/// <summary>
	/// Send a raw SocketIOMessage to the server.
	/// </summary>
	/// <param name='msg'>
	/// Message.
	/// </param>
	public int Send (SocketIOMessage msg)
	{
		msg.id = msgUid++;
		if(socket == null) {
			Debug.LogError("Socket.IO is not initialised yet!");
			return -1;
		} else {
			socket.Send (msg.ToString ());
			return msg.id.Value;
		}
	}
	
	/// <summary>
	/// Send the specified payload as a JSON message to the server.
	/// </summary>
	/// <param name='payload'>
	/// Payload.
	/// </param>
	public int Send(object payload) {
		var m = new SocketIOMessage();
		m.type = SocketIOMessage.FrameType.JSONMESSAGE;
		m.data = HTTP.JsonSerializer.Encode(payload);
		return Send (m);
	}
	
	
	/// <summary>
	/// Sends an event to the server.
	/// </summary>
	/// <param name='eventName'>
	/// Event name.
	/// </param>
	/// <param name='args'>
	/// Arguments.
	/// </param>
	public int Emit(string eventName, params object[] args) {
		var m = new SocketIOMessage();
		m.type = SocketIOMessage.FrameType.EVENT;
		var payload = new Hashtable();
		payload["name"] = eventName;
		payload["args"] = args;
		m.data = HTTP.JsonSerializer.Encode(payload);
		return Send (m);
	}
	
	IEnumerator Start ()
	{
		
		Application.runInBackground = true;
		if (!url.EndsWith ("/")) {
			url = url + "/";
		}
		
		var req = new HTTP.Request ("POST", url + "socket.io/1/");
		req.Send ();
		yield return req.Wait();
		if (req.exception == null) {
			if (req.response.status == 200) {	
				var parts = (from i in req.response.Text.Split (':') select i.Trim ()).ToArray ();
				sid = parts [0];
				float.TryParse (parts [1], out heartbeatTimeout);
				float.TryParse (parts [2], out closingTimeout);
				transports = (from i in parts [3].Split (',') select i.Trim ().ToLower ()).ToArray ();
			}
			if (transports.Contains ("websocket")) {
				socket = new WebSocket ();
				StartCoroutine (socket.Dispatcher ());
				socket.Connect (url + "socket.io/1/websocket/" + sid);
				socket.OnTextMessageRecv += HandleSocketOnTextMessageRecv;
			} else {
				Debug.LogError ("Websocket is not supported with this server.");	
			}
		}
	}

	void HandleSocketOnTextMessageRecv (string message)
	{
		
		var msg = SocketIOMessage.FromString (message);
		msg.socket = this;
		
		switch (msg.type) {
		case SocketIOMessage.FrameType.DISCONNECT:
			StopCoroutine ("Hearbeat");
			if (handler != null)
			if (handler.OnDisconnect != null)
				handler.OnDisconnect (msg);
			break;
		case SocketIOMessage.FrameType.CONNECT:
			if (msg.endPoint == null)
				StartCoroutine ("Heartbeat");
			if (handler.OnConnect != null)
				handler.OnConnect (msg);
			break;
		case SocketIOMessage.FrameType.HEARTBEAT:
			if (handler.OnHearbeat != null)
				handler.OnHearbeat (msg);
			break;
		case SocketIOMessage.FrameType.MESSAGE:
			if (handler.OnMessage != null)
				handler.OnMessage (msg);
			break;
		case SocketIOMessage.FrameType.JSONMESSAGE:
			if (handler.OnJSONMessage != null) {
				var o = HTTP.JsonSerializer.Decode(msg.data);
				handler.OnJSONMessage (msg, o);
			}
			break;
		case SocketIOMessage.FrameType.EVENT:
			if (handler.OnEvent != null) {
				var o = HTTP.JsonSerializer.Decode(msg.data) as Hashtable;
				handler.OnEvent (msg, o["name"] as string, o["args"] as ArrayList);
			}
			break;
		case SocketIOMessage.FrameType.ACK:
			if (handler.OnACK != null)
				handler.OnACK (msg);
			break;
		case SocketIOMessage.FrameType.ERROR:
			if (handler.OnError != null)
				handler.OnError (msg);
			break;
		case SocketIOMessage.FrameType.NOOP:
			if (handler.OnNoop != null)
				handler.OnNoop (msg);
			break;
		default: 
			break;
		}
	}
	
	IEnumerator Heartbeat ()
	{
		var beat = new SocketIOMessage ();
		beat.type = SocketIOMessage.FrameType.HEARTBEAT;
		var delay = new WaitForSeconds (heartbeatTimeout * 0.8f);
		while (socket.connected) {
			socket.Send (beat.ToString ());
			yield return delay;
		}
	}

}
