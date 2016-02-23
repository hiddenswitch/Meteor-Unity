using System;
using System.Collections;
using System.Collections.Generic;

namespace Meteor
{
	public class Observe<TRecordType> : IDisposable
		where TRecordType : MongoDocument, new()
	{
		/// <summary>
		/// Raised after a documented is added. The first parameter is the document's id, and the second is the record data.
		/// </summary>
		public Action<string,TRecordType> Added {
			get;
			private set;
		}

		/// <summary>
		/// Raised after a document is changed. The first parameter is the record id, the second is the new record, the third is a dictionary of changes and the last is the list of cleared fields, if any.
		/// </summary>
		public Action<string,TRecordType,IDictionary,string[]> Changed {
			get;
			private set;
		}

		/// <summary>
		/// Raised after a document is removed. The first parameter is the record's id.
		/// </summary>
		public Action<string> Removed {
			get;
			private set;
		}

		public Func<TRecordType, bool> RecordSelector {
			get;
			private set;
		}

		public bool Initializing {
			get;
			protected set;
		}

		public Collection<TRecordType> Collection {
			get;
			protected set;
		}

		public ICollection<String> Fields {
			get;
			protected set;
		}

		protected string idToRemove;

		public Observe (Collection<TRecordType> collection, Action<string,TRecordType> added = null, Action<string,TRecordType,IDictionary,string[]> changed = null, Action<string> removed = null, Func<TRecordType, bool> selector = null, IEnumerable<string> fields = null)
		{
			this.Collection = collection;

			RecordSelector = selector ?? SelectAll;

			if (fields != null) {
				Fields = new HashSet<string> (fields);
			}

			Initializing = true;

			// Call added on all the existing records
			if (added != null) {
				foreach (var record in collection) {
					if (RecordSelector (record)) {
						added (record._id, record);
					}
				}
			}

			Initializing = false;

			Added = added;
			Changed = changed;
			Removed = removed;

			collection.DidAddRecord += Collection_DidAddRecord;
			collection.DidChangeRecord += Collection_DidChangeRecord;
			collection.WillRemoveRecord += Collection_WillRemoveRecord;
			collection.DidRemoveRecord += Collection_DidRemoveRecord;
		}

		void Collection_WillRemoveRecord (string obj)
		{
			// Check if this record would have matched the selector
			if (RecordSelector (Collection [obj])) {
				// If it matched the selector, make sure to raise did remove record next
				idToRemove = obj;
			} else {
				idToRemove = null;
			}
		}

		void Collection_DidRemoveRecord (string obj)
		{
			if (obj == idToRemove
			    && Removed != null) {
				Removed (obj);
			}

			idToRemove = null;
		}

		void Collection_DidChangeRecord (string arg1, TRecordType arg2, IDictionary arg3, string[] arg4)
		{
			// Are any of the fields part of this change?
			var fieldsInterested = true;
			if (Fields != null) {
				fieldsInterested = false;
				foreach (var field in arg4) {
					if (Fields.Contains (field)) {
						fieldsInterested = true;
					}
				}
			}
			if (fieldsInterested
			    && RecordSelector (arg2)
			    && Changed != null) {
				Changed (arg1, arg2, arg3, arg4);
			}
		}

		bool SelectAll (TRecordType record)
		{
			return true;
		}

		void Collection_DidAddRecord (string arg1, TRecordType arg2)
		{
			if (RecordSelector (arg2)
			    && Added != null) {
				Added (arg1, arg2);
			}
		}

		public void Stop ()
		{
			if (Collection != null) {
				Collection.DidAddRecord -= Collection_DidAddRecord;
				Collection.DidChangeRecord -= Collection_DidChangeRecord;
				Collection.WillRemoveRecord -= Collection_WillRemoveRecord;
				Collection.DidRemoveRecord -= Collection_DidRemoveRecord;
			}
		}

		#region IDisposable implementation

		~Observe ()
		{
			Dispose ();
		}

		public void Dispose ()
		{
			Stop ();
		}

		#endregion
	}
}

