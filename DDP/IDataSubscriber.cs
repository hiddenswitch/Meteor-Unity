using System;
using System.Collections.Generic;
using System.Text;

namespace Meteor.LiveData
{
    public interface IDataSubscriber
    {
        void DataReceived(IDictionary<string,object> data);
    }
}
