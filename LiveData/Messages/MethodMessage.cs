namespace Meteor.Internal
{
	internal class MethodMessage : Message
	{
		const string _method = "method";

		[JsonFx.Json.JsonName("params")]
		public object[] Params;
		public string id;
		public string method;

		public MethodMessage()
		{
			msg = _method;
		}
	}
}

