using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using JsonFx.Json;
using Meteor.Extensions;

namespace Meteor.Internal
{
	public class TemporaryCollection : Hashtable, Meteor.Internal.ICollection
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
	
}
