using System;
namespace Meteor.Internal
{
	internal class RemovedMessage : CollectionMessage
	{
		public const string removed = "removed";

		public string id = null;
		public RemovedMessage ()
		{
			msg = removed;
		}
	}
}

