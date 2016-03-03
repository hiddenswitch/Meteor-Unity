using System;
using System.Collections.ObjectModel;

namespace Meteor.Internal
{
	internal class SubscriptionCollection : KeyedCollection<string, Subscription>
	{
		public SubscriptionCollection ()
		{
		}

		protected override string GetKeyForItem (Subscription item)
		{
			return item.requestId;
		}
	}
}

