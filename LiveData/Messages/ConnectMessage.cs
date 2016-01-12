using Meteor.Extensions;

namespace Meteor
{
	public class ConnectMessage : Message
	{
		const string connect = "connect";
		const string pre = "pre1";
		public static string connectMessage;
		public string version;
		public string[] support;

		public ConnectMessage()
		{
			msg = connect;
			version = pre;
			support = new[] { "pre1" };
		}

		static ConnectMessage()
		{
			connectMessage = new ConnectMessage().Serialize();
		}
	}
}

