using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using JsonFx.Json;
using Meteor.Extensions;

namespace Meteor.Internal
{
	public interface ICollection : System.Collections.ICollection
	{
		/// <summary>
		/// Add a record before another record in order.
		/// </summary>
		/// <param name="id">Record ID.</param>
		/// <param name="before">The ID of the record to insert before.</param>
		/// <param name="record">The record.</param>
		void AddedBefore (string id, string before, object record);

		/// <summary>
		/// Add the serialized message to the collection.
		/// </summary>
		/// <param name="addedMessage">Added message.</param>
		void Added (string addedMessage);

		/// <summary>
		/// Add the record to the collection.
		/// </summary>
		/// <param name="record">Record.</param>
		void Added (object record);

		/// <summary>
		/// Add the record to the collection with the specified ID
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="record">Record.</param>
		void Added (string id, object record);

		/// <summary>
		/// Notify the collection that a record has changed.
		/// </summary>
		/// <param name="id">Record ID.</param>
		/// <param name="cleared">Fields that are now undefined.</param>
		/// <param name="fields">New values for fields of record.</param>
		void Changed (string id, string[] cleared, IDictionary fields);

		/// <summary>
		/// Move a record before another record.
		/// </summary>
		/// <param name="id">ID of record.</param>
		/// <param name="before">ID of record to move before.</param>
		void MovedBefore (string id, string before);

		/// <summary>
		/// Remove a record.
		/// </summary>
		/// <param name="id">Identifier.</param>
		void Removed (string id);

		/// <summary>
		/// Collection name.
		/// </summary>
		/// <value>The name.</value>
		string Name {
			get;
		}
	}
	
}
