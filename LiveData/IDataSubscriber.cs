using System;
using System.Collections.Generic;
using System.Text;

namespace Meteor.Internal
{
    internal interface IDataSubscriber
    {
        void DataReceived(IDictionary<string,object> data);
    }
}
