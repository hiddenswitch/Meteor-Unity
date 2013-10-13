using System.Collections;
namespace Net.DDP.Client.Messages
{
	public class ChangedMessage : CollectionMessage
	{
		public const string changed = "changed";
		public string id = null;
		public IDictionary fields = null;
		public string[] cleared = null;
		public ChangedMessage ()
		{
			msg = changed;
		}
	}
}

