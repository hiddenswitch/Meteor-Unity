using Meteor.Extensions;

namespace Meteor.Internal
{
	internal class FailedMessage : Message
	{
		public const string failed = "failed";
		// Disabling the warning here because fields is assigned to, it's just assigned to via reflection
		// so the compiler doesn't know
		#pragma warning disable 0649 
		public string version;
		#pragma warning restore 0649

		public FailedMessage()
		{
			msg = failed;
		}
	}
}

