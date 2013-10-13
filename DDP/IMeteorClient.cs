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
		Method Call (string methodName, params object[] arguments);
		Method<TResponseType> Call<TResponseType> (string methodName, params object[] arguments)
			where TResponseType : new();
		Collection<TRecordType> Subscribe<TRecordType>(string collectionName, string publishName, params object[] arguments)
			where TRecordType : new();
        int GetCurrentRequestId();
    }
}
