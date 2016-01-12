using System;

namespace Meteor
{
	public class PingMessage : Message
	{
		public const string ping = "ping";
		public string id;

		public PingMessage ()
		{
			msg = ping;
		}
	}
}

