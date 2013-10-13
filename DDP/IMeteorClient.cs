using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net.DDP.Client
{
    public interface IMeteorClient
    {
		void QueueMessage(string jsonItem);
        void Connect(string url);
		string Call(string methodName, params object[] arguments);
		Method Call (string methodName, MethodHandler handler, params object[] arguments);
		Method<TResponseType> Call<TResponseType>(string methodName, MethodHandler<TResponseType> handler, params object[] arguments)
			where TResponseType : new();
		Collection<RecordType> Subscribe<RecordType>(string collectionName, string publishName, params object[] arguments)
			where RecordType : new();
        int GetCurrentRequestId();
    }
}
