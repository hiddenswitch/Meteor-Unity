namespace Net.DDP.Client.Messages
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

