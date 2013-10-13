using Extensions;

namespace Net.DDP.Client.Messages
{
	public class ConnectedMessage : Message
	{
		public const string connected = "connected";
		public string session;

		public ConnectedMessage()
		{
			msg = connected;
		}
	}
}

