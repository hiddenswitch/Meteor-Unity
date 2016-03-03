namespace Meteor.Internal
{
	internal class UnsubscribeMessage : Message
	{
		const string unsub = "unsub";

		string id;

		public UnsubscribeMessage ()
		{
			msg = unsub;
		}
	}
}

