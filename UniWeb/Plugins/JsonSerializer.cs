using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.Linq;

/* Taken from http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JsonSerializer.html
 * MIT Licensed: http://www.opensource.org/licenses/mit-license.php
 */

namespace HTTP
{
	public class JsonSerializer
	{
		public const int TOKEN_NONE = 0;
		public const int TOKEN_CURLY_OPEN = 1;
		public const int TOKEN_CURLY_CLOSE = 2;
		public const int TOKEN_SQUARED_OPEN = 3;
		public const int TOKEN_SQUARED_CLOSE = 4;
		public const int TOKEN_COLON = 5;
		public const int TOKEN_COMMA = 6;
		public const int TOKEN_STRING = 7;
		public const int TOKEN_NUMBER = 8;
		public const int TOKEN_TRUE = 9;
		public const int TOKEN_FALSE = 10;
		public const int TOKEN_NULL = 11;
		
		public static object Decode (byte[] json)
		{
			return Decode (System.Text.ASCIIEncoding.ASCII.GetString (json));
		}

		public static object Decode (string json)
		{
			bool success = true;
			return Decode (json, ref success);
		}
	
		public static void Decode (object instance, string json)
		{
			var obj = Decode (json);
			PopulateObject (instance.GetType (), obj, instance);
		}

		public static object Decode (string json, ref bool success)
		{
			success = true;
			if (json != null) {
				char[] charArray = json.ToCharArray ();
				int index = 0;
				object value = ParseValue (charArray, ref index, ref success);
				return value;
			} else {
				return null;
			}
		}

		public static string Encode (object json)
		{
			var builder = new StringBuilder ();
			var success = SerializeValue (json, builder);
			return (success ? builder.ToString () : null);
		}

		public static T Decode<T> (byte[] json) where T : class, new()
		{
			return Decode<T> (System.Text.ASCIIEncoding.ASCII.GetString (json));
		}

		public static T Decode<T> (string json) where T : class, new()
		{
			var success = true;
			var obj = Decode (json, ref success);
			return PopulateObject (typeof(T), obj) as T;
		}
	
		static object PopulateObject (Type T, object obj)
		{
			return PopulateObject (T, obj, null);
		}
	
		static object PopulateObject (Type T, object obj, object instance)
		{
			if (obj == null)
				return null;
			if (T.IsAssignableFrom (obj.GetType ())) {
				instance = obj;
			} else if (obj is Hashtable) {
				var h = (Hashtable)obj;
				if (instance == null)
					instance = Activator.CreateInstance (T);
				foreach (var fi in T.GetFields ()) {
					if (h.ContainsKey (fi.Name)) {
						fi.SetValue (instance, PopulateObject (fi.FieldType, h [fi.Name]));
					}
				}
			} else if (obj is IEnumerable) {
				if (instance == null)
					instance = Activator.CreateInstance (T);
				var list = instance as IList;
				if (list != null) {
					Type containerType = typeof(object);
					var IT = instance.GetType ();
					if (IT.IsGenericType) {
						var args = IT.GetGenericArguments ();
						if (args.Length != 1)
							return null;
						containerType = args [0];
					}
					foreach (var i in (IEnumerable)obj) {
						list.Add (PopulateObject (containerType, i));
					}
				}
			}
			return instance;
		}

		protected static Hashtable ParseObject (char[] json, ref int index, ref bool success)
		{
			var table = new Hashtable ();
			int token;
		
			// {
			NextToken (json, ref index);
		
			var done = false;
			while (!done) {
				token = LookAhead (json, index);
				if (token == JsonSerializer.TOKEN_NONE) {
					success = false;
					return null;
				} else if (token == JsonSerializer.TOKEN_COMMA) {
					NextToken (json, ref index);
				} else if (token == JsonSerializer.TOKEN_CURLY_CLOSE) {
					NextToken (json, ref index);
					return table;
				} else {
				
					// name
					var name = ParseString (json, ref index, ref success);
					if (!success) {
						success = false;
						return null;
					}
				
					// :
					token = NextToken (json, ref index);
					if (token != JsonSerializer.TOKEN_COLON) {
						success = false;
						return null;
					}
				
					// value
					var value = ParseValue (json, ref index, ref success);
					if (!success) {
						success = false;
						return null;
					}
				
					table [name] = value;
				}
			}
		
			return table;
		}

		protected static ArrayList ParseArray (char[] json, ref int index, ref bool success)
		{
			var array = new ArrayList ();
		
			// [
			NextToken (json, ref index);
		
			var done = false;
			while (!done) {
				var token = LookAhead (json, index);
				if (token == JsonSerializer.TOKEN_NONE) {
					success = false;
					return null;
				} else if (token == JsonSerializer.TOKEN_COMMA) {
					NextToken (json, ref index);
				} else if (token == JsonSerializer.TOKEN_SQUARED_CLOSE) {
					NextToken (json, ref index);
					break;
				} else {
					var value = ParseValue (json, ref index, ref success);
					if (!success) {
						return null;
					}
					array.Add (value);
				}
			}
		
			return array;
		}

		protected static object ParseValue (char[] json, ref int index, ref bool success)
		{
			switch (LookAhead (json, index)) {
			case JsonSerializer.TOKEN_STRING:
				return ParseString (json, ref index, ref success);
			case JsonSerializer.TOKEN_NUMBER:
				return ParseNumber (json, ref index, ref success);
			case JsonSerializer.TOKEN_CURLY_OPEN:
				return ParseObject (json, ref index, ref success);
			case JsonSerializer.TOKEN_SQUARED_OPEN:
				return ParseArray (json, ref index, ref success);
			case JsonSerializer.TOKEN_TRUE:
				NextToken (json, ref index);
				return true;
			case JsonSerializer.TOKEN_FALSE:
				NextToken (json, ref index);
				return false;
			case JsonSerializer.TOKEN_NULL:
				NextToken (json, ref index);
				return null;
			case JsonSerializer.TOKEN_NONE:
				break;
			}
		
			success = false;
			return null;
		}

		protected static string ParseString (char[] json, ref int index, ref bool success)
		{
			var s = new StringBuilder ();
			char c;
		
			EatWhitespace (json, ref index);
		
			// "
			c = json [index++];
		
			var complete = false;
			while (!complete) {
			
				if (index == json.Length) {
					break;
				}
			
				c = json [index++];
				if (c == '"') {
					complete = true;
					break;
				} else if (c == '\\') {
				
					if (index == json.Length) {
						break;
					}
					c = json [index++];
					if (c == '"') {
						s.Append ('"');
					} else if (c == '\\') {
						s.Append ('\\');
					} else if (c == '/') {
						s.Append ('/');
					} else if (c == 'b') {
						s.Append ('\b');
					} else if (c == 'f') {
						s.Append ('\f');
					} else if (c == 'n') {
						s.Append ('\n');
					} else if (c == 'r') {
						s.Append ('\r');
					} else if (c == 't') {
						s.Append ('\t');
					} else if (c == 'u') {
						var remainingLength = json.Length - index;
						if (remainingLength >= 4) {
							// parse the 32 bit hex into an integer codepoint
							uint codePoint;
							if (!(success = UInt32.TryParse (new string (json, index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint))) {
								return "";
							}
							// convert the integer codepoint to a unicode char and add to string
							s.Append (Char.ConvertFromUtf32 ((int)codePoint));
							// skip 4 chars
							index += 4;
						} else {
							break;
						}
					}
				
				} else {
					s.Append (c);
				}
			
			}
		
			if (!complete) {
				success = false;
				return null;
			}
		
			return s.ToString ();
		}

		protected static object ParseNumber (char[] json, ref int index, ref bool success)
		{
			EatWhitespace (json, ref index);
		
			var lastIndex = GetLastIndexOfNumber (json, index);
			var charLength = (lastIndex - index) + 1;
		
			
			var token = new string (json, index, charLength);
			index = lastIndex + 1;
			if (token.Contains (".")) {
				float number;
				if(float.TryParse (token, NumberStyles.Any, CultureInfo.InvariantCulture, out number)) {
					return (float)number;
				} else {
					return (string)token;	
				}
			} else {
				long number;
				if(long.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out number)) {
					return (long)number;
				} else {
					return (string)token;
				}
			}
		}

		protected static int GetLastIndexOfNumber (char[] json, int index)
		{
			int lastIndex;
		
			for (lastIndex = index; lastIndex < json.Length; lastIndex++) {
				if ("0123456789+-.eE".IndexOf (json [lastIndex]) == -1) {
					break;
				}
			}
			return lastIndex - 1;
		}

		protected static void EatWhitespace (char[] json, ref int index)
		{
			for (; index < json.Length; index++) {
				if (" \t\n\r".IndexOf (json [index]) == -1) {
					break;
				}
			}
		}

		protected static int LookAhead (char[] json, int index)
		{
			var saveIndex = index;
			return NextToken (json, ref saveIndex);
		}

		protected static int NextToken (char[] json, ref int index)
		{
			EatWhitespace (json, ref index);
		
			if (index == json.Length) {
				return JsonSerializer.TOKEN_NONE;
			}
		
			var c = json [index];
			index++;
			switch (c) {
			case '{':
				return JsonSerializer.TOKEN_CURLY_OPEN;
			case '}':
				return JsonSerializer.TOKEN_CURLY_CLOSE;
			case '[':
				return JsonSerializer.TOKEN_SQUARED_OPEN;
			case ']':
				return JsonSerializer.TOKEN_SQUARED_CLOSE;
			case ',':
				return JsonSerializer.TOKEN_COMMA;
			case '"':
				return JsonSerializer.TOKEN_STRING;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
			case '-':
				return JsonSerializer.TOKEN_NUMBER;
			case ':':
				return JsonSerializer.TOKEN_COLON;
			}
			index--;
		
			var remainingLength = json.Length - index;
		
			// false
			if (remainingLength >= 5) {
				if (json [index] == 'f' && json [index + 1] == 'a' && json [index + 2] == 'l' && json [index + 3] == 's' && json [index + 4] == 'e') {
					index += 5;
					return JsonSerializer.TOKEN_FALSE;
				}
			}
		
			// true
			if (remainingLength >= 4) {
				if (json [index] == 't' && json [index + 1] == 'r' && json [index + 2] == 'u' && json [index + 3] == 'e') {
					index += 4;
					return JsonSerializer.TOKEN_TRUE;
				}
			}
		
			// null
			if (remainingLength >= 4) {
				if (json [index] == 'n' && json [index + 1] == 'u' && json [index + 2] == 'l' && json [index + 3] == 'l') {
					index += 4;
					return JsonSerializer.TOKEN_NULL;
				}
			}
		
			return JsonSerializer.TOKEN_NONE;
		}

		protected static bool SerializeValue (object value, StringBuilder builder)
		{
			var success = true;
			if (value is string) {
				success = SerializeString ((string)value, builder);
			} else if (value is Hashtable) {
				success = SerializeObject ((Hashtable)value, builder);
			} else if (value is IEnumerable) {
				success = SerializeArray ((IEnumerable)value, builder);
			} else if (value is float) {
				success = SerializeNumber (Convert.ToSingle (value), builder);
			} else if (value is int || value is long || value is uint) {
				success = SerializeNumber (Convert.ToInt64 (value), builder);
			} else if (value is double) {
				success = SerializeNumber (Convert.ToDouble (value), builder);
			} else if ((value is Boolean) && ((Boolean)value == true)) {
				builder.Append ("true");
			} else if ((value is Boolean) && ((Boolean)value == false)) {
				builder.Append ("false");
			} else if (value == null) {
				builder.Append ("null");
			} else if (value is DateTime) {
				builder.Append (((DateTime)value).ToString ("o"));
			} else {
				var h = new Hashtable ();
				foreach (var i in value.GetType ().GetFields ()) {
					if (i.IsNotSerialized)
						continue;
					h [i.Name] = i.GetValue (value);
				}
				foreach (var i in value.GetType ().GetProperties()) {
					h [i.Name] = i.GetValue (value, null);
				}
				SerializeObject (h, builder);
			}
			return success;
		}

		protected static bool SerializeObject (Hashtable anObject, StringBuilder builder)
		{
			builder.Append ("{");
			IDictionaryEnumerator e = anObject.GetEnumerator ();
			var first = true;
			while (e.MoveNext ()) {
				var key = e.Key.ToString ();
				var value = e.Value;
				if (!first) {
					builder.Append (", ");
				}
				SerializeString (key, builder);
				builder.Append (":");
				if (!SerializeValue (value, builder)) {
					return false;
				}
				first = false;
			}
			builder.Append ("}");
			return true;
		}

		protected static bool SerializeArray (IEnumerable anArray, StringBuilder builder)
		{
			builder.Append ("[");
			var first = true;
			foreach (var value in anArray) {
				if (!first) {
					builder.Append (", ");
				}
				if (!SerializeValue (value, builder)) {
					return false;
				}
				first = false;
			}
			builder.Append ("]");
			return true;
		}

		protected static bool SerializeString (string aString, StringBuilder builder)
		{
			builder.Append ("\"");
		
			var charArray = aString.ToCharArray ();
			for (var i = 0; i < charArray.Length; i++) {
				var c = charArray [i];
				if (c == '"') {
					builder.Append ("\\\"");
				} else if (c == '\\') {
					builder.Append ("\\\\");
				} else if (c == '\b') {
					builder.Append ("\\b");
				} else if (c == '\f') {
					builder.Append ("\\f");
				} else if (c == '\n') {
					builder.Append ("\\n");
				} else if (c == '\r') {
					builder.Append ("\\r");
				} else if (c == '\t') {
					builder.Append ("\\t");
				} else {
					int codepoint = Convert.ToInt32 (c);
					if ((codepoint >= 32) && (codepoint <= 126)) {
						builder.Append (c);
					} else {
						builder.Append ("\\u" + Convert.ToString (codepoint, 16).PadLeft (4, '0'));
					}
				}
			}
		
			builder.Append ("\"");
			return true;
		}

		protected static bool SerializeNumber (int number, StringBuilder builder)
		{
			builder.Append (Convert.ToString (number, CultureInfo.InvariantCulture));
			return true;
		}

		protected static bool SerializeNumber (float number, StringBuilder builder)
		{
			builder.Append (Convert.ToString (number, CultureInfo.InvariantCulture));
			return true;
		}
		
		protected static bool SerializeNumber (long number, StringBuilder builder)
		{
			builder.Append (Convert.ToString (number, CultureInfo.InvariantCulture));
			return true;
		}
		
		protected static bool SerializeNumber (double number, StringBuilder builder)
		{
			builder.Append (Convert.ToString (number, CultureInfo.InvariantCulture));
			return true;
		}


		/// <summary>
		/// Determines if a given object is numeric in any way
		/// (can be integer, double, null, etc). 
		/// 
		/// Thanks to mtighe for pointing out Double.TryParse to me.
		/// </summary>
		protected static bool IsNumeric (object o)
		{
			float result;
		
			return (o == null) ? false : float.TryParse (o.ToString (), out result);
		}

	
	}

}