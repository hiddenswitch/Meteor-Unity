using Meteor.Extensions;

namespace Meteor.Internal
{
	internal class FailedMessage : Message
	{
		public const string failed = "failed";
		public string version;

		public FailedMessage()
		{
			msg = failed;
		}
	}
}

