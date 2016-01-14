#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2006-2009 Stephen M. McKamey

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.

\*---------------------------------------------------------------------------------*/
#endregion License

using System;

namespace JsonFx.Json
{
	/// <summary>
	/// Controls the deserialization settings for JsonReader
	/// </summary>
	public class JsonReaderSettings
	{
		#region Fields

		internal readonly TypeCoercionUtility Coercion = new TypeCoercionUtility();
		private bool allowUnquotedObjectKeys = false;
		private string typeHintName;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets and sets if ValueTypes can accept values of null
		/// </summary>
		/// <remarks>
		/// Only affects deserialization: if a ValueType is assigned the
		/// value of null, it will receive the value default(TheType).
		/// Setting this to false, throws an exception if null is
		/// specified for a ValueType member.
		/// </remarks>
		public bool AllowNullValueTypes
		{
			get { return this.Coercion.AllowNullValueTypes; }
			set { this.Coercion.AllowNullValueTypes = value; }
		}

		/// <summary>
		/// Gets and sets if objects can have unquoted property names
		/// </summary>
		public bool AllowUnquotedObjectKeys
		{
			get { return this.allowUnquotedObjectKeys; }
			set { this.allowUnquotedObjectKeys = value; }
		}

		/// <summary>
		/// Gets and sets the property name used for type hinting.
		/// </summary>
		public string TypeHintName
		{
			get { return this.typeHintName; }
			set { this.typeHintName = value; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Determines if the specified name is the TypeHint property
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		internal bool IsTypeHintName(string name)
		{
			return
				!String.IsNullOrEmpty(name) &&
				!String.IsNullOrEmpty(this.typeHintName) &&
				StringComparer.Ordinal.Equals(this.typeHintName, name);
		}

		#endregion Methods
	}
}
