using System.Collections;

namespace Meteor.Internal
{
	internal class NosubMessage : Message
	{
		public const string nosub = "nosub";

		[JsonFx.Json.JsonName("id")]
		public string id;

		[JsonFx.Json.JsonName("error")]
		public Hashtable Error;

		public NosubMessage()
		{
			msg = nosub;
		}
	}
}

