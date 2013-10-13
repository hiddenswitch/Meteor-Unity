using System;

namespace Net.DDP.Client
{
	public delegate void MethodHandler(Meteor.Error error, object response);
	public delegate void MethodHandler<TResponseType>(Meteor.Error error, TResponseType response);

	public interface IMethod
	{
		void Callback(Meteor.Error error, object response);
		object UntypedResponse { get; }
		Meteor.Error Error { get; }
		Type ResponseType { get; }
		IMeteorClient Client { get; }
	}
}

