using System;

namespace Meteor.Internal
{
	internal class PingMessage : Message
	{
		public const string ping = "ping";
		// Disabling the warning here because fields is assigned to, it's just assigned to via reflection
		// so the compiler doesn't know
		#pragma warning disable 0649 
		public string id;
		#pragma warning restore 0649

		public PingMessage ()
		{
			msg = ping;
		}
	}
}

