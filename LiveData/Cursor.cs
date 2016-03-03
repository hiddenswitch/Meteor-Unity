using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using JsonFx.Json;
using Meteor.Extensions;

namespace Meteor
{
	/// <summary>
	/// A handle to a query against your client-side document collections.
	/// </summary>
	public class Cursor<TRecordType>
		where TRecordType : MongoDocument, new()
	{

		/// <summary>
		/// The collection references in this cursor
		/// </summary>
		/// <value>The collection.</value>
		public Collection<TRecordType> collection {
			get;
			protected set;
		}

		/// <summary>
		/// The selector used for this cursor
		/// </summary>
		/// <value>The selector.</value>
		public Func<TRecordType, bool> selector {
			get;
			protected set;
		}

		/// <summary>
		/// If specified, the array of document IDs that this cursor will match.
		/// </summary>
		/// <value>The identifiers.</value>
		public IEnumerable<string> ids {
			get;
			protected set;
		}

		internal Cursor (Collection<TRecordType> collection, Func<TRecordType, bool> selector = null)
		{
			this.collection = collection;
			this.selector = selector ?? SelectAll;
		}

		internal Cursor (Collection<TRecordType> collection, string id)
		{
			this.collection = collection;
			this.ids = new string[] { id };
		}

		internal Cursor (Collection<TRecordType> collection, IEnumerable<string> ids)
		{
			this.collection = collection;
			this.ids = ids;
		}

		/// <summary>
		/// Observes changes for the query specified by the selector. Behaves like Meteor's observe.
		/// An observe invokes callbacks when the result of the query changes. The callbacks receive the entire contents of the document that was affected, as well as its old contents, if applicable.
		/// Before observe returns, added will be called zero or more times to deliver the initial results of the query.
		/// This method returns an Observe instance, which is an object with a stop method. Call stop with no arguments to stop calling the callback functions and tear down the query. The query will run forever until you call this.
		/// </summary>
		/// <param name="added">A new document document entered the result set. </param>
		/// <param name="changed">A callback for changes to documents. The first argument is the ID, the second the entire record, the third a dictionary of fields that were changed as keys and the new values as values, and an array of strings specifying any deleted fields.
		/// The second argument, the entire document, has the changes already applied to it.</param>
		/// <param name="removed">A document with the given ID was removed. The callback is called after the document is removed from the collection.</param>
		public Observe<TRecordType> Observe (Action<string,TRecordType> added = null, Action<string,TRecordType,IDictionary,string[]> changed = null, Action<string> removed = null)
		{
			return new Observe<TRecordType> (collection: this.collection, added: added, changed: changed, removed: removed, selector: selector, fields: null);
		}

		/// <summary>
		/// Returns the array of documents matched by this cursor.
		/// </summary>
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
