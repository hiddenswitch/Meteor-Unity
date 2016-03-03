using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using JsonFx.Json;
using Meteor.Extensions;
using Meteor.Internal;

namespace Meteor
{
	/// <summary>
	/// A Mongo collection corresponding to your <code>new Mongo.Collection</code> statements in Meteor.
	/// Calling this function is analogous to declaring a model in a traditional ORM (Object-Relation Mapper)-centric framework. It sets up a collection (a storage space for records, or "documents") that can be used to store a particular type of information, like users, posts, scores, todo items, or whatever matters to your application. Each document is a EJSON object. It includes an _id property whose value is unique in the collection, which Meteor will set when you first create the document.
	/// </summary>
	public class Collection<TRecordType> : KeyedCollection<string, TRecordType>, Meteor.Internal.ICollection
		where TRecordType : MongoDocument, new()
	{
		protected Collection () : base ()
		{
		}

		/// <summary>
		/// Creates a new Mongo-style collection.
		/// Throws an exception if a collection with the given name already exists. If you want a way to get an
		/// existing collection instance if it already exists, use Collection&lt;TRecordType&gt;.Create(name)
		/// </summary>
		/// <param name="name">Name corresponding to your Meteor code's new Mongo.Collection(name) statement. If null, returns a local-only collection.</param>
		public Collection (string name) : base ()
		{
			var doesCollectionAlreadyExist = LiveData.Instance.Collections.Contains (name);
			var isNameEmpty = string.IsNullOrEmpty (name);
			var isCollectionTemporary = doesCollectionAlreadyExist && LiveData.Instance.Collections [name] as TemporaryCollection != null;
			if (!isNameEmpty
			    && doesCollectionAlreadyExist
			    && !isCollectionTemporary) {
				throw new ArgumentException (string.Format ("A collection with name {0} already exists", name));
			}

			Collection<TRecordType>.Create (name, instance: this);
		}

		/// <summary>
		/// Finds documents that match the specified selector.
		/// </summary>
		/// <param name="selector">Selector. For example, record =&gt; record.type == 1 returns all documents whose "type" field
		/// matches "1". This is a standard function that should return true when the document matches. </param>
		public Cursor<TRecordType> Find (Func<TRecordType, bool> selector = null)
		{
			return new Cursor<TRecordType> (collection: this, selector: selector);
		}

		/// <summary>
		/// Returns a cursor matching the single ID. Useful for a subsequent observe.
		/// </summary>
		/// <param name="id">Document ID.</param>
		public Cursor<TRecordType> Find (string id)
		{
			return new Cursor<TRecordType> (collection: this, id: id);
		}

		/// <summary>
		/// Returns a cursor matching an array of document IDs.
		/// </summary>
		/// <param name="ids">Document IDs.</param>
		public Cursor<TRecordType> Find (IEnumerable<string> ids)
		{
			return new Cursor<TRecordType> (collection: this, ids: ids);
		}

		/// <summary>
		/// Finds and returns a single document matching the ID.
		/// </summary>
		/// <returns>A matching document. Throws an exception if none is found.</returns>
		/// <exception cref="System.Collections.Generic.KeyNotFoundException">Throws if the specified document does not exist. </exception>
		/// <param name="id">The document ID.</param>
		public TRecordType FindOne (string id)
		{
			
			return this [id];
		}

		/// <summary>
		/// Finds documents matching the selector and returns the first one matching the selector, in arbitrary order. Throws an exception if none is found.
		/// </summary>
		/// <returns>A matching document.</returns>
		/// <exception cref="System.Collections.Generic.KeyNotFoundException">Throws if the specified document does not exist. </exception>
		/// <param name="selector">Selector.</param>
		public TRecordType FindOne (Func<TRecordType, bool> selector = null)
		{
			selector = selector ?? delegate(TRecordType arg) {
				return true;
			};

			foreach (var record in this) {
				if (selector (record)) {
					return record;
				}
			}
			return null;
		}

		/// <summary>
		/// Creates a collection for the specified name. You can call this multiple times and it will return the same instance of a collection if it was
		/// already declared somewhere else.
		/// </summary>
		/// <param name="name">The name of the collection corresponding to your Meteor code's new Mongo.Collection(name) statement.</param>
		public static Collection<TRecordType> Create (string name)
		{
			return Create (name, new Collection<TRecordType> ());
		}

		protected static Collection<TRecordType> Create (string name, Collection<TRecordType> instance)
		{
			instance = instance ?? new Collection<TRecordType> ();
			if (string.IsNullOrEmpty (name)) {
				return instance;
			}

			// Check if we already have this collection defined, otherwise make it
			if (!LiveData.Instance.Collections.Contains (name)) {
				instance.name = name;
				LiveData.Instance.Collections.Add (instance as Meteor.Internal.ICollection);
			}

			var collection = LiveData.Instance.Collections [name] as Collection<TRecordType>;

			// The collection may already exist, but it may be of the wrong type
			if (collection == null) {
				// Convert the collection to the requested type
				collection = Convert (name, instance);
			}

			return collection;
		}

		protected static Collection<TRecordType> Convert (string name, Collection<TRecordType> instance)
		{
			var oldCollection = LiveData.Instance.Collections [name];
			var typedCollection = instance ?? new Collection<TRecordType> ();
			typedCollection.name = name;
			foreach (DictionaryEntry doc in oldCollection) {
				var value = doc.Value.Coerce<TRecordType> ();
				value._id = (string)doc.Key;
				typedCollection.Add (value);
			}

			LiveData.Instance.Collections.Remove (name);
			LiveData.Instance.Collections.Add (typedCollection);
			return typedCollection;
		}

		protected override string GetKeyForItem (TRecordType item)
		{
			return item._id;
		}

		/// <summary>
		/// The collection name corresponding to your Meteor code's new Mongo.Collection(name) statement
		/// </summary>
		public string name;

		TypeCoercionUtility typeCoercionUtility = new TypeCoercionUtility ();

		/// <summary>
		/// Raised before a documented is added. The first parameter is the document's id, and the second is the record data.
		/// </summary>
		public event Action<string,TRecordType> WillAddRecord;
		/// <summary>
		/// Raised after a documented is added. The first parameter is the document's id, and the second is the record data.
		/// </summary>
		public event Action<string,TRecordType> DidAddRecord;
		/// <summary>
		/// Raised before a document is changed. The first parameter is the record id, the second is the record before changes, the third is a dictionary of changes and the last is the list of cleared fields, if any.
		/// </summary>
		public event Action<string,TRecordType,IDictionary,string[]> WillChangeRecord;
		/// <summary>
		/// Raised after a document is changed. The first parameter is the record id, the second is the new record, the third is a dictionary of changes and the last is the list of cleared fields, if any.
		/// </summary>
		public event Action<string,TRecordType,IDictionary,string[]> DidChangeRecord;
		/// <summary>
		/// Raised before a document is removed. The first parameter is the record's id.
		/// </summary>
		public event Action<string> WillRemoveRecord;
		/// <summary>
		/// Raised after a document is removed. The first parameter is the record's id.
		/// </summary>
		public event Action<string> DidRemoveRecord;

		#region ICollection implementation

		void Meteor.Internal.ICollection.AddedBefore (string id, string before, object record)
		{
			TRecordType r = record.Coerce<TRecordType> ();

			if (WillAddRecord != null) {
				WillAddRecord (id, r);
			}

			Insert (IndexOf (this [id]), r);

			if (DidAddRecord != null) {
				DidAddRecord (id, r);
			}
		}

		void Meteor.Internal.ICollection.Added (string messageText)
		{
			var message = messageText.Deserialize<AddedMessage<TRecordType>> ();
			var r = message.fields;
			r._id = message.id;
			((Meteor.Internal.ICollection)this).Added (r);
		}

		void Meteor.Internal.ICollection.Added (object record)
		{
			var r = record.Coerce<TRecordType> ();

			if (WillAddRecord != null) {
				WillAddRecord (r._id, r);
			}

			if (!Contains (r._id)) {
				Add (r);
			}

			if (DidAddRecord != null) {
				DidAddRecord (r._id, r);
			}
		}

		void Meteor.Internal.ICollection.Added (string id, object record)
		{
			var r = record.Coerce<TRecordType> ();

			r._id = id;

			if (WillAddRecord != null) {
				WillAddRecord (r._id, r);
			}

			if (!Contains (r._id)) {
				Add (r);
			}

			if (DidAddRecord != null) {
				DidAddRecord (r._id, r);
			}
		}

		void Meteor.Internal.ICollection.Changed (string id, string[] cleared, IDictionary fields)
		{
			// Allow this to throw an exception.
			TRecordType record = this [id];

			// Record the member map
			Dictionary<string, MemberInfo> memberMap = null;

			if (fields == null) {
				fields = new Dictionary<string, object> ();
			}

			// Add the cleared fields as nulls or defaults
			if (cleared != null) {
				foreach (string clear in cleared) {
					fields [clear] = null;
				}
			}

			if (WillChangeRecord != null) {
				WillChangeRecord (id, record, fields, cleared);
			}

			// Update the fields in r with the content of fields
			typeCoercionUtility.CoerceType (typeof(TRecordType), fields, record, out memberMap);

			if (DidChangeRecord != null) {
				DidChangeRecord (id, record, fields, cleared);
			}
		}

		void Meteor.Internal.ICollection.MovedBefore (string id, string before)
		{
			var record = this [id];
			Remove (id);
			Insert (IndexOf (this [before]), record);
		}

		void Meteor.Internal.ICollection.Removed (string id)
		{
			if (WillRemoveRecord != null) {
				WillRemoveRecord (id);
			}

			Remove (id);

			if (DidRemoveRecord != null) {
				DidRemoveRecord (id);
			}
		}

		string Meteor.Internal.ICollection.Name {
			get {
				return name;
			}
		}

		#endregion

		/// <summary>
		/// Inserts a record. Currently not supported. This is a client only representation of the collection. In order to insert documents, define
		/// a method on the server that performs the insert, and call that method. The features of the <code>insecure</code> package, like client-side inserts,
		/// are not supported.
		/// </summary>
		/// <param name="record">Record.</param>
		public void Insert (TRecordType record)
		{
			throw new NotSupportedException ();
		}

		/// <summary>
		/// Updates a record. Currently not supported. This is a client only representation of the collection. In order to update documents, define
		/// a method on the server that performs the update, and call that method. The features of the <code>insecure</code> package, like client-side updates,
		/// are not supported.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="mongoUpdateCommand">Mongo update command.</param>
		/// <param name="mongoUpdateOptions">Mongo update options.</param>
		public void Update (string id, IDictionary mongoUpdateCommand, IDictionary mongoUpdateOptions)
		{
			throw new NotSupportedException ();
		}
	}
}

