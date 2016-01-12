using Meteor.Extensions;

namespace Meteor
{
	public class ConnectMessage : Message
	{
		const string connect = "connect";
		const string versionConst = "1";
		public static string connectMessage;
		public string version;
		public string[] support;

		public ConnectMessage()
		{
			msg = connect;
			version = versionConst;
			support = new[] { "pre2", "1" };
		}

		static ConnectMessage()
		{
			connectMessage = new ConnectMessage().Serialize();
		}
	}
}

