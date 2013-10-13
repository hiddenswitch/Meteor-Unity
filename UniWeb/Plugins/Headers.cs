using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HTTP
{
	/// <summary>
	/// Headers is a collection class for HTTP style headers.
	/// </summary>
	public class Headers
	{
		
		/// <summary>
		/// Add a header to the collection.
		/// </summary>
		public void Add (string name, string value)
		{
			GetAll (name).Add (value);
		}
		
		/// <summary>
		/// Get the header specified by name from the collection. Returns the first value if more than one is available.
		/// </summary>
		public string Get (string name)
		{
			List<string> header = GetAll (name);
			if (header.Count == 0) {
				return "";
			}
			return header [0];
		}
		
		/// <summary>
		/// Returns true if the collection contains the header.
		/// </summary>
		public bool Contains (string name)
		{
			List<string> header = GetAll (name);
			if (header.Count == 0) {
				return false;
			}
			return true;
		}
		
		/// <summary>
		/// Gets all the values of a header.
		/// </summary>
		public List<string> GetAll (string name)
		{
			foreach (string key in headers.Keys) {
				if (string.Compare (name, key, true) == 0) {
					return headers [key];
				}
			}
			List<string> newHeader = new List<string> ();
			headers.Add (name, newHeader);
			return newHeader;
		}
		
		/// <summary>
		/// Set the specified header to have a single value.
		/// </summary>
		public void Set (string name, string value)
		{
			List<string> header = GetAll (name);
			header.Clear ();
			header.Add (value);
		}
		
		/// <summary>
		/// Removes a header from the collection.
		/// </summary>
		public void Pop (string name)
		{
			if (headers.ContainsKey (name)) {
				headers.Remove (name);
			}
		}
		
		
		/// <summary>
		/// Write the headers out to a stream.
		/// </summary>
		public void Write (BinaryWriter stream)
		{
			foreach (string name in headers.Keys) {
				foreach (string value in headers[name]) {
					stream.Write (System.Text.ASCIIEncoding.ASCII.GetBytes (name + ": " + value));
					stream.Write (Request.EOL);
				}
			}
		}
		
		/// <summary>
		/// Gets the header names present in the collection.
		/// </summary>
		public List<string> Keys {
			get {
				return headers.Keys.ToList();		
			}
		}
		
		/// <summary>
		/// Removes all headers and values from the collection.
		/// </summary>
		public void Clear() {
			headers.Clear();
		}
	
		Dictionary<string, List<string>> headers = new Dictionary<string, List<string>> ();
	
	}
}