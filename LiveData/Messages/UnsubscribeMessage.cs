namespace Meteor.Internal
{
	internal class UnsubscribeMessage : Message
	{
		const string unsub = "unsub";

		internal string id;

		public UnsubscribeMessage ()
		{
			msg = unsub;
		}
	}
}

