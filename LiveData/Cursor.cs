using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using JsonFx.Json;
using Meteor.Extensions;

namespace Meteor
{
	public class Cursor<TRecordType>
		where TRecordType : MongoDocument, new()
	{
		public Collection<TRecordType> collection {
			get;
			protected set;
		}

		public Func<TRecordType, bool> selector {
			get;
			protected set;
		}

		public Cursor (Collection<TRecordType> collection, Func<TRecordType, bool> selector = null)
		{
			this.collection = collection;
			this.selector = selector ?? SelectAll;
		}

		public Observe<TRecordType> Observe (Action<string,TRecordType> added = null, Action<string,TRecordType,IDictionary,string[]> changed = null, Action<string> removed = null)
		{
			return new Observe<TRecordType> (collection: this.collection, added: added, changed: changed, removed: removed, selector: selector);
		}

		public IEnumerable<TRecordType> Fetch ()
		{
			foreach (var record in collection) {
				if (selector (record)) {
					yield return record;
				}
			}

			yield break;
		}

		private bool SelectAll (TRecordType record)
		{
			return true;
		}
	}
}
