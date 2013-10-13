using System;
using Meteor;

namespace Meteor.LiveData
{
	public delegate void MethodHandler(Error error, object response);
	public delegate void MethodHandler<TResponseType>(Error error, TResponseType response);

	public interface IMethod
	{
		void Callback(Error error, object response);
		object UntypedResponse { get; }
		Error Error { get; }
		Type ResponseType { get; }
	}
}

