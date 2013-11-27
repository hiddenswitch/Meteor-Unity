using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using JsonFx.Json;
using Extensions;


namespace Meteor
{
	public interface ICollection
	{
		/// <summary>
		/// Add a record before another record in order.
		/// </summary>
		/// <param name="id">Record ID.</param>
		/// <param name="before">The ID of the record to insert before.</param>
		/// <param name="record">The record.</param>
		void AddedBefore(string id, string before, object record);

		/// <summary>
		/// Add the serialized message to the collection.
		/// </summary>
		/// <param name="addedMessage">Added message.</param>
		void Added(string addedMessage);

		/// <summary>
		/// Add the record to the collection.
		/// </summary>
		/// <param name="record">Record.</param>
		void Added(object record);

		/// <summary>
		/// Notify the collection that a record has changed.
		/// </summary>
		/// <param name="id">Record ID.</param>
		/// <param name="cleared">Fields that are now undefined.</param>
		/// <param name="fields">New values for fields of record.</param>
		void Changed(string id, string[] cleared, IDictionary fields);

		/// <summary>
		/// Move a record before another record.
		/// </summary>
		/// <param name="id">ID of record.</param>
		/// <param name="before">ID of record to move before.</param>
		void MovedBefore(string id, string before);

		/// <summary>
		/// Remove a record.
		/// </summary>
		/// <param name="id">Identifier.</param>
		void Removed(string id);

		/// <summary>
		/// Collection name.
		/// </summary>
		/// <value>The name.</value>
		string Name {
			get;
		}

		/// <summary>
		/// Record type.
		/// </summary>
		/// <value>The type of the collection.</value>
		Type CollectionType { get; }
	}

	public class Collection<TRecordType> : KeyedCollection<string, TRecordType>, ICollection
		where TRecordType : MongoDocument, new()
	{
		protected Collection() : base() {}

		public static Collection<TRecordType> Create(string name)
		{
			if (string.IsNullOrEmpty(name)) {
				return new Collection<TRecordType>();
			}

			// Check if we already have this collection defined, otherwise make it
			if (!LiveData.Instance.Collections.Contains(name)) {
				LiveData.Instance.Collections.Add(new Collection<TRecordType> () {name = name} as ICollection);
			}

			return LiveData.Instance.Collections [name] as Collection<TRecordType>;
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

		public Type CollectionType
		{
			get
			{
				return typeof(TRecordType);
			}
		}

		TypeCoercionUtility typeCoercionUtility = new TypeCoercionUtility();

		/// <summary>
		/// Raised every document added. The first paramter is the Id and the second is the document.
		/// </summary>
		public event Action<string,TRecordType> OnAdded;
		public event Action<string,TRecordType> OnChanged;
		public event Action<string> OnRemoved;

		#region ICollection implementation
		void ICollection.AddedBefore(string id, string before, object record)
		{
			TRecordType r = record.Coerce<TRecordType> ();
			Insert (IndexOf (this [id]), r);

			if (OnAdded != null) {
				OnAdded (id, r);
			}
		}

		void ICollection.Added(string messageText)
		{
			var message = messageText.Deserialize<AddedMessage<TRecordType>> ();
			var r = message.fields;
			r._id = message.id;
			((ICollection)this).Added (r);
		}

		void ICollection.Added(object record)
		{
			var r = record.Coerce<TRecordType> ();

			Add (r);

			if (OnAdded != null) {
				OnAdded(r._id, r);
			}
		}

		void ICollection.Changed(string id, string[] cleared, IDictionary fields)
		{
			// Allow this to throw an exception.
			TRecordType record = this[id];

			// Record the member map
			Dictionary<string, MemberInfo> memberMap = new Dictionary<string, MemberInfo>();

			if (fields == null)
			{
				fields = new Dictionary<string, object>();
			}

			// Add the cleared fields as nulls or defaults
			if (cleared != null) {
				foreach (string clear in cleared) {
					fields[clear] = null;
				}
			}


			// Update the fields in r with the content of fields
			typeCoercionUtility.CoerceType(typeof(TRecordType), fields, record, out memberMap);

			if (OnChanged != null) {
				OnChanged(id, this[id]);
			}
		}

		void ICollection.MovedBefore(string id, string before)
		{
			var record = this [id];
			Remove (id);
			Insert (IndexOf (this [before]), record);
		}

		void ICollection.Removed(string id)
		{
			Remove(id);
			if (OnRemoved != null)
			{
				OnRemoved(id);
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

