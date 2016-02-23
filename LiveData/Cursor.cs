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

		public IEnumerable<string> ids {
			get;
			protected set;
		}

		public Cursor (Collection<TRecordType> collection, Func<TRecordType, bool> selector = null)
		{
			this.collection = collection;
			this.selector = selector ?? SelectAll;
		}

		public Cursor (Collection<TRecordType> collection, string id)
		{
			this.collection = collection;
			this.ids = new string[] { id };
		}

		public Cursor (Collection<TRecordType> collection, IEnumerable<string> ids)
		{
			this.collection = collection;
			this.ids = ids;
		}

		public Observe<TRecordType> Observe (Action<string,TRecordType> added = null, Action<string,TRecordType,IDictionary,string[]> changed = null, Action<string> removed = null, IEnumerable<string> fields = null)
		{
			return new Observe<TRecordType> (collection: this.collection, added: added, changed: changed, removed: removed, selector: selector, fields: fields);
		}

		public IEnumerable<TRecordType> Fetch ()
		{
			if (ids == null) {
				foreach (var record in collection) {
					if (selector (record)) {
						yield return record;
					}
				}
				yield break;
			}

			foreach (var id in ids) {
				yield return collection [id];
			}

			yield break;
		}

		private bool SelectAll (TRecordType record)
		{
			return true;
		}
	}
}
