using UnityEngine;
using System.Collections;

public class SocketIOMessage
	{
		public enum FrameType
		{
			DISCONNECT,
			CONNECT,
			HEARTBEAT,
			MESSAGE,
			JSONMESSAGE,
			EVENT,
			ACK,
			ERROR,
			NOOP
		}
		public SocketIOConnection socket;
		public FrameType type;
		public int? id;
		public bool isUser;
		public string endPoint;
		public string data;
		
		public static SocketIOMessage FromString (string msg)
		{
			
			
			var m = new SocketIOMessage ();
			var t = 0;
			if (int.TryParse (NextPart (msg, out msg), out t)) {
				m.type = (FrameType)t;	
			}
			var id = NextPart (msg, out msg);
			if (id == null) {
				m.id = null;
				m.isUser = false;
			} else {
				if (id.EndsWith ("+")) {
					m.isUser = true;
					id = id.Substring (0, id.Length - 1);
				}
				int i;
				if (int.TryParse (id, out i)) {
					m.id = i;	
				}
			}
			m.endPoint = NextPart (msg, out msg);
			if(msg.Length > 0)
				m.data = msg.Substring(1);
			
			return m;
		}
		
		static string NextPart (string parts, out string remainder)
		{
			if (parts [0] == ':') {
				remainder = parts.Substring (1);
				return null;	
			} 
			var next = parts.IndexOf (':');
			var part = parts.Substring (0, next);
			remainder = parts.Substring (next);
			return part;
		}
		
		public override string ToString ()
		{
			return string.Format ("{0}:{1}:{2}:{3}", (int)type, isUser ? id.ToString () + "+" : id.ToString (), endPoint, data);
		}
		
	}