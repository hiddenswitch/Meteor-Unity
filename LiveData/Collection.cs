using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using JsonFx.Json;
using Meteor.Extensions;

namespace Meteor
{
	public class TemporaryCollection : Hashtable, Meteor.ICollection
	{
		public TemporaryCollection () : base ()
		{
		}

		public TemporaryCollection (string name) : this ()
		{
			Name = name;
		}

		public void AddedBefore (string id, string before, object record)
		{
			this.Added (id, record);
		}

		public void Added (string addedMessage)
		{
			var message = addedMessage.Deserialize<AddedMessage<Hashtable>> ();
			message.fields ["_id"] = message.id;
			this.Added ((object)message.fields);
		}

		public void Added (object record)
		{
			string _id = null;

			var recordDictionary = record as IDictionary;

			if (recordDictionary != null) {
				_id = (string)(recordDictionary ["_id"]);
			} else {
				return;
			}

			if (ContainsKey (_id)) {
				this.Remove (_id);
			}

			this.Add (_id, record);
		}

		public void Added (string id, object record)
		{
			var obj = record as IDictionary;
			obj ["_id"] = id;
			this.Added ((object)obj);
		}

		public void Changed (string id, string[] cleared, IDictionary fields)
		{
			IDictionary existingDoc = null;
			if (!this.ContainsKey (id)) {
				this.Add (id, fields);
				return;
			}
			existingDoc = this [id] as IDictionary;
			if (existingDoc == null) {
				// Cannot interpret as dictionary
				return;
			}
			var enumerator = fields.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				existingDoc [enumerator.Key] = enumerator.Value;
			}
		}

		public void MovedBefore (string id, string before)
		{
			return;
		}

		public void Removed (string id)
		{
			this.Remove (id);
		}

		public string Name {
			get;
			private set;
		}

		public Type CollectionType {
			get {
				return typeof(IDictionary);
			}
		}
	}

	public class Collection<TRecordType> : KeyedCollection<string, TRecordType>, Meteor.ICollection
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
		/// <param name="name">Name. If null, returns a local-only collection.</param>
		public Collection (string name) : base ()
		{
			var doesCollectionAlreadyExist = LiveData.Instance.Collections.Contains (name);
			var isNameEmpty = string.IsNullOrEmpty (name);
			var isCollectionTemporary = LiveData.Instance.Collections [name] as TemporaryCollection != null;
			if (!isNameEmpty
			    && doesCollectionAlreadyExist
			    && !isCollectionTemporary) {
				throw new ArgumentException (string.Format ("A collection with name {0} already exists", name));
			}


			Collection<TRecordType>.Create (name, instance: this);
		}

		public Cursor<TRecordType> Find (Func<TRecordType, bool> selector = null)
		{
			return new Cursor<TRecordType> (collection: this, selector: selector);
		}

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
				LiveData.Instance.Collections.Add (instance as ICollection);
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

		public string name;

		public bool ready {
			get;
			private set;
		}

		public Type CollectionType {
			get {
				return typeof(TRecordType);
			}
		}

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

		void ICollection.AddedBefore (string id, string before, object record)
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

		void ICollection.Added (string messageText)
		{
			var message = messageText.Deserialize<AddedMessage<TRecordType>> ();
			var r = message.fields;
			r._id = message.id;
			((ICollection)this).Added (r);
		}

		void ICollection.Added (object record)
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

		void ICollection.Added (string id, object record)
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

		void ICollection.Changed (string id, string[] cleared, IDictionary fields)
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

		void ICollection.MovedBefore (string id, string before)
		{
			var record = this [id];
			Remove (id);
			Insert (IndexOf (this [before]), record);
		}

		void ICollection.Removed (string id)
		{
			if (WillRemoveRecord != null) {
				WillRemoveRecord (id);
			}

			Remove (id);

			if (DidRemoveRecord != null) {
				DidRemoveRecord (id);
			}
		}

		string ICollection.Name {
			get {
				return name;
			}
		}

		#endregion
	}
}

