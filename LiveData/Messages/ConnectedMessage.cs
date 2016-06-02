using Meteor.Extensions;

namespace Meteor.Internal
{
	internal class ConnectedMessage : Message
	{
		public const string connected = "connected";
		// Disabling the warning here because fields is assigned to, it's just assigned to via reflection
		// so the compiler doesn't know
		#pragma warning disable 0649 
		public string session;
		#pragma warning restore 0649

		public ConnectedMessage()
		{
			msg = connected;
		}
	}
}

