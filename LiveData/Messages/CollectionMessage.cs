using System;
namespace Meteor.Internal
{
	internal class CollectionMessage : Message
	{
		// Disabling the warning here because fields is assigned to, it's just assigned to via reflection
		// so the compiler doesn't know
		#pragma warning disable 0649 
		public string collection;
		#pragma warning restore 0649
		public CollectionMessage() {}
	}
}

