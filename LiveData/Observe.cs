using System;
using System.Collections;
using System.Collections.Generic;

namespace Meteor
{
	/// <summary>
	/// An observe handle. Returned by <see cref="Meteor.Cursor.Find"/>.
	/// </summary>
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

		/// <summary>
		/// Is this observe handle receiving its first matching set of documents to add?
		/// </summary>
		/// <value><c>true</c> if initializing; otherwise, <c>false</c>.</value>
		public bool Initializing {
			get;
			protected set;
		}

		/// <summary>
		/// Gets the collection referenced by this observe handle.
		/// </summary>
		/// <value>The collection.</value>
		public Collection<TRecordType> Collection {
			get;
			protected set;
		}

		protected HashSet<String> Fields;

		protected string idToRemove;

		/// <summary>
		/// Creates a new observe handle without a cursor. It is recommended to use <see cref="Meteor.Collection`1.Find"/> to get a cursor and then call <code>Observe</code> on that cursor.
		/// An observe invokes callbacks when the result of the query changes. The callbacks receive the entire contents of the document that was affected, as well as its old contents, if applicable.
		/// Before observe returns, added will be called zero or more times to deliver the initial results of the query.
		/// This method returns an Observe instance, which is an object with a stop method. Call stop with no arguments to stop calling the callback functions and tear down the query. The query will run forever until you call this.
		/// </summary>
		/// <param name="added">A new document document entered the result set. </param>
		/// <param name="changed">A callback for changes to documents. The first argument is the ID, the second the entire record, the third a dictionary of fields that were changed as keys and the new values as values, and an array of strings specifying any deleted fields.
		/// The second argument, the entire document, has the changes already applied to it.</param>
		/// <param name="removed">A document with the given ID was removed. The callback is called after the document is removed from the collection.</param>
		/// <param name="collection">Collection.</param>
		/// <param name="selector">Selector.</param>
		/// <param name="fields">Fields. Currently not supported.</param>
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

		void Collection_DidChangeRecord (string id, TRecordType record, IDictionary fields, string[] cleared)
		{
			// Are any of the fields part of this change?
			var fieldsInterested = true;

			if (fieldsInterested
			    && RecordSelector (record)
			    && Changed != null) {
				Changed (id, record, fields, cleared);
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

		/// <summary>
		/// Stops observing changes to the collection with the given selector.
		/// </summary>
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

		/// <summary>
		/// Releases all resource used by the <see cref="Meteor.Observe`1"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Meteor.Observe`1"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Meteor.Observe`1"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="Meteor.Observe`1"/> so the garbage
		/// collector can reclaim the memory that the <see cref="Meteor.Observe`1"/> was occupying.</remarks>
		public void Dispose ()
		{
			Stop ();
		}

		#endregion
	}
}

