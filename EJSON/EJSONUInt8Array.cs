using System;

namespace Meteor.EJSON
{
	public class EJSONUInt8Array
	{
		[JsonFx.Json.JsonName("$binary")]
		public string binary;
		public EJSONUInt8Array ()
		{
		}
		public EJSONUInt8Array(byte[] value) {
			binary = System.Convert.ToBase64String(value);
		}
	}
}

