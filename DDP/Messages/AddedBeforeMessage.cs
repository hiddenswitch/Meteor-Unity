using System;

namespace Net.DDP.Client.Messages
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

