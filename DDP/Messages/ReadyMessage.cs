namespace Net.DDP.Client.Messages
{
	public class ReadyMessage : Message
	{
		public const string ready = "ready";

		public string[] subs = null;
		public ReadyMessage ()
		{
			msg = ready;
		}
	}
}

