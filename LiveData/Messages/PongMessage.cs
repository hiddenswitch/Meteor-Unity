using System;

namespace Meteor
{
	public class PongMessage : Message
	{
		public const string pong = "pong";
		public string id;

		public PongMessage ()
		{
			msg = pong;
		}
	}
}

