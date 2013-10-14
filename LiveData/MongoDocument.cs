using System;
using System.Collections.Generic;
namespace Meteor
{
	public class MongoDocument : Dictionary<string, object>, IMongoDocument
	{
		[JsonFx.Json.JsonIgnore]
		public string _id {
			get {
				return this ["_id"] as string;
			}
		}

		public MongoDocument ()
		{
		}
	}
}

