using System;

namespace Meteor.Internal
{
	internal class PingMessage : Message
	{
		public const string ping = "ping";
		public string id;

		public PingMessage ()
		{
			msg = ping;
		}
	}
}

