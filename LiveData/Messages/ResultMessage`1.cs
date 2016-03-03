using System.Collections;

namespace Meteor.Internal
{
	internal class ResultMessage<TResponseType> : Message
	{
		[JsonFx.Json.JsonIgnore]
		public const string result = "result";
		public Error error;

		public string id;

		[JsonFx.Json.JsonName("result")]
		public TResponseType methodResult;

		public ResultMessage ()
		{
			msg = result;
		}
	}
}

