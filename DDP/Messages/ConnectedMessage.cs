using Extensions;

namespace Meteor.LiveData
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

