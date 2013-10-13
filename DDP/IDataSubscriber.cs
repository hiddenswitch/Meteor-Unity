using System;
using System.Collections.Generic;
using System.Text;

namespace Net.DDP.Client
{
    public interface IDataSubscriber
    {
        void DataReceived(IDictionary<string,object> data);
    }
}
