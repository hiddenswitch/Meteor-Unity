using System;
namespace Meteor.LiveData
{
	public class AddedMessage : CollectionMessage
	{
		[JsonFx.Json.JsonIgnore]
		public const string added = "added";
		public string id = null;
		public object fields = null;

		public AddedMessage ()
		{
			msg = added;
		}
	}
}

