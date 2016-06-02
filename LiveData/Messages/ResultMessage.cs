using System.Collections;

namespace Meteor.Internal
{
	internal class ResultMessage : Message
	{
		[JsonFx.Json.JsonIgnore]
		public const string result = "result";
		// Disabling the warning here because fields is assigned to, it's just assigned to via reflection
		// so the compiler doesn't know
		#pragma warning disable 0649 
		public Error error;

		public string id;
		#pragma warning restore 0649

		[JsonFx.Json.JsonName("result")]
		public object methodResult;

		public ResultMessage ()
		{
			msg = result;
		}
	}
}

