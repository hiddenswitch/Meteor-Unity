namespace Meteor.LiveData
{
	public class UnsubscribeMessage : Message
	{
		const string unsub = "unsub";

		string id;

		public UnsubscribeMessage ()
		{
			msg = unsub;
		}
	}
}

