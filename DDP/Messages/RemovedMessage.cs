using System;
namespace Net.DDP.Client.Messages
{
	public class RemovedMessage : CollectionMessage
	{
		public const string removed = "removed";

		public string id = null;
		public RemovedMessage ()
		{
			msg = removed;
		}
	}
}

