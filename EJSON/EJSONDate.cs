using System;

namespace Meteor.EJSON
{
	public class EJSONDate
	{
		[JsonFx.Json.JsonName("$date")]
		public long date;
		public EJSONDate ()
		{
		}

		public EJSONDate (DateTime value) {
			date = value.Ticks;
		}
	}
}

