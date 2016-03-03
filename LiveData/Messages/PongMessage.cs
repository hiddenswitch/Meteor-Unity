using System;

namespace Meteor.Internal
{
	internal class PongMessage : Message
	{
		public const string pong = "pong";
		public string id;

		public PongMessage ()
		{
			msg = pong;
		}
	}
}

