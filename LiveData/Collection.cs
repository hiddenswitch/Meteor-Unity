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
		void AddedBefore(string id, string before, object record);

		void Added(string id, object record);

		void Changed(string id, string[] cleared, IDictionary fields);

		void MovedBefore(string id, string before);

		void SubscriptionReady(string subscription);

		void Removed(string id);

		Type CollectionType { get; }
	}

	public class Collection<TRecordType> : KeyedCollection<string, TRecordType>, ICollection
		where TRecordType : IMongoDocument, new()
	{
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

		public event Action<string,TRecordType> OnAdded;
		public event Action<string,TRecordType> OnChanged;
		public event Action<string> OnSubscriptionReady;
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

		void ICollection.Added(string id, object record)
		{
			TRecordType r = record.Coerce<TRecordType> ();
			Add (r);

			if (OnAdded != null) {
				OnAdded(id, r);
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
			foreach (string clear in cleared) {
				fields[clear] = null;
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

		void ICollection.SubscriptionReady(string subscription)
		{
			ready = true;
			if (OnSubscriptionReady != null)
			{
				OnSubscriptionReady(subscription);
			}
		}

		void ICollection.Removed(string id)
		{
			Remove(id);
			if (OnRemoved != null)
			{
				OnRemoved(id);
			}
		}
		#endregion
	}
}

