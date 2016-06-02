using System;
namespace Meteor.Internal
{
	internal class AddedMessage<TRecordType> : CollectionMessage
	{
		[JsonFx.Json.JsonIgnore]
		public const string added = "added";
		public string id = null;
		// Disabling the warning here because fields is assigned to, it's just assigned to via reflection
		// so the compiler doesn't know
		#pragma warning disable 0649 
		public TRecordType fields;
		#pragma warning restore 0649

		public AddedMessage ()
		{
			msg = added;
		}
	}
}

