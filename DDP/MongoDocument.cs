using System;
using System.Collections;
namespace Meteor
{
	public class MongoDocument : Hashtable
	{
		[JsonFx.Json.JsonIgnore]
		public string Id {
			get {
				return this ["_id"] as string;
			}
			set {
				this ["_id"] = value;
			}
		}

		public MongoDocument ()
		{
		}
	}
}

