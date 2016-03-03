using System.Collections;

namespace Meteor.Internal
{
	internal class ResultMessage : Message
	{
		[JsonFx.Json.JsonIgnore]
		public const string result = "result";
		public Error error;

		public string id;

		[JsonFx.Json.JsonName("result")]
		public object methodResult;

		public ResultMessage ()
		{
			msg = result;
		}
	}
}

