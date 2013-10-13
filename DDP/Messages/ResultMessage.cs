using System.Collections;

namespace Meteor.LiveData
{
	public class ResultMessage : Message
	{
		[JsonFx.Json.JsonIgnore]
		public const string result = "result";
		public Meteor.Error error;

		public string id;

		[JsonFx.Json.JsonName("result")]
		public IDictionary methodResult;

		public string[] subs = null;
		public ResultMessage ()
		{
			msg = result;
		}
	}
}

