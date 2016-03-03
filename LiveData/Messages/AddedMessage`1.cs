using System;
namespace Meteor.Internal
{
	internal class AddedMessage<TRecordType> : CollectionMessage
	{
		[JsonFx.Json.JsonIgnore]
		public const string added = "added";
		public string id = null;
		public TRecordType fields;

		public AddedMessage ()
		{
			msg = added;
		}
	}
}

