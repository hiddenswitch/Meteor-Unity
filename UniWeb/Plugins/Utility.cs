using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;


namespace HTTP
{
	
	public class URL
	{
		static string safeChars = "-_.~abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		public static string Encode (string value)
		{
			var result = new StringBuilder ();
			foreach (var s in value) {
				if (safeChars.IndexOf (s) != -1) {
					result.Append (s);
				} else {
					result.Append ('%' + String.Format ("{0:X2}", (int)s));
				}
			}
			return result.ToString ();
		}
		
		public static string Decode(string s) {
			return WWW.UnEscapeURL(s);
		}
		
		public static Dictionary<string,string> KeyValue(string queryString) {
			
			var kv = new Dictionary<string,string>();
			if(queryString.Length == 0) return kv;
			var pairs = queryString.Split('&');
			foreach(var i in pairs) {
				var t = i.Split('=');
				if(t.Length < 2) continue;
				kv[Decode(t[0])] = Decode(t[1]);
			}
			return kv;
		}
		
	}
}


