using System;
using System.Collections;

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

		protected string idToRemove;

		public Observe (Collection<TRecordType> collection, Action<string,TRecordType> added = null, Action<string,TRecordType,IDictionary,string[]> changed = null, Action<string> removed = null, Func<TRecordType, bool> selector = null)
		{
			this.Collection = collection;

			if (selector == null) {
				RecordSelector = SelectAll;
			} else {
				RecordSelector = selector;
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
			if (RecordSelector (arg2)
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
			Collection.DidAddRecord -= Collection_DidAddRecord;
			Collection.DidChangeRecord -= Collection_DidChangeRecord;
			Collection.WillRemoveRecord -= Collection_WillRemoveRecord;
			Collection.DidRemoveRecord -= Collection_DidRemoveRecord;
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			Stop ();
		}

		#endregion
	}
}

