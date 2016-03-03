using System;
using Meteor;

namespace Meteor.Internal
{
	public delegate void MethodHandler(Error error, object response);
	public delegate void MethodHandler<TResponseType>(Error error, TResponseType response);

	internal interface IMethod
	{
		void Callback(Error error, object response);
		bool Updated { get; set; }
		object UntypedResponse { get; }
		Error Error { get; }
		Type ResponseType { get; }
	}
}

