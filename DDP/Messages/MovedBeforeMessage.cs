namespace Net.DDP.Client.Messages
{
	public class MovedBeforeMessage : CollectionMessage
	{
		const string movedBefore = "movedBefore";
		public string id = null;
		public string before = null;

		public MovedBeforeMessage ()
		{
			msg = movedBefore;
		}
	}
}

