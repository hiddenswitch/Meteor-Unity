using System;
using System.Collections.Generic;
using System.Text;

namespace Meteor
{
    public interface IDataSubscriber
    {
        void DataReceived(IDictionary<string,object> data);
    }
}
