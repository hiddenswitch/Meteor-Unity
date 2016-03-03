using System.Collections;
namespace Meteor.Internal
{
	internal class ChangedMessage : CollectionMessage
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

