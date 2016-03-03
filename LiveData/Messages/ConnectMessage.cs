using Meteor.Extensions;

namespace Meteor.Internal
{
	internal class ConnectMessage : Message
	{
		const string connect = "connect";
		const string versionConst = "pre2";
		public static string connectMessage;
		public string version;
		public string[] support;

		public ConnectMessage()
		{
			msg = connect;
			version = versionConst;
			support = new[] { "pre2", "pre1" };
		}

		static ConnectMessage()
		{
			connectMessage = new ConnectMessage().Serialize();
		}
	}
}

