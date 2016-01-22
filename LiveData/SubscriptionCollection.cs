using System;
using System.Collections.ObjectModel;

namespace Meteor
{
	public class SubscriptionCollection : KeyedCollection<string, Subscription>
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

