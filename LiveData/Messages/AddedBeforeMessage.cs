using System;

namespace Meteor
{
	public class AddedBeforeMessage<RecordType> : CollectionMessage
		where RecordType : new()
	{
		const string adddedBefore = "adddedBefore";

		public string id = null;
		public RecordType fields = default(RecordType);
		public string before = null;

		public AddedBeforeMessage ()
		{
			msg = adddedBefore;
		}
	}
}

