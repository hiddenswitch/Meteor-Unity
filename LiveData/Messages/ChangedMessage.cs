using System.Collections;
namespace Meteor
{
	public class ChangedMessage : CollectionMessage
	{
		public const string changed = "changed";
		public string id = null;
		public Hashtable fields = null;
		public string[] cleared = null;
		public ChangedMessage ()
		{
			msg = changed;
		}
	}
}

