using Extensions;

namespace Net.DDP.Client.Messages
{
	public class ConnectMessage : Message
	{
		const string connect = "connect";
		const string pre = "pre1";
		public static string connectMessage;
		public string version;

		public ConnectMessage()
		{
			msg = connect;
			version = pre;
		}

		static ConnectMessage()
		{
			connectMessage = new ConnectMessage().Serialize();
		}
	}
}

