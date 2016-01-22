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
using System.IO;

namespace JsonFx.Json
{
	/// <summary>
	/// Represents a proxy method for serialization of types which do not implement IJsonSerializable
	/// </summary>
	/// <typeparam name="T">the type for this proxy</typeparam>
	/// <param name="writer">the JsonWriter to serialize to</param>
	/// <param name="value">the value to serialize</param>
	public delegate void WriteDelegate<T>(JsonWriter writer, T value);

	/// <summary>
	/// Controls the serialization settings for JsonWriter
	/// </summary>
	public class JsonWriterSettings
	{
		#region Fields

		private WriteDelegate<DateTime> dateTimeSerializer;
		private WriteDelegate<byte[]> byteArraySerializer;
		private int maxDepth = 25;
		private string newLine = Environment.NewLine;
		private bool prettyPrint;
		private string tab = "\t";
		private string typeHintName;
		private bool useXmlSerializationAttributes;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets and sets the property name used for type hinting.
		/// </summary>
		public virtual string TypeHintName
		{
			get { return this.typeHintName; }
			set { this.typeHintName = value; }
		}

		/// <summary>
		/// Gets and sets if JSON will be formatted for human reading.
		/// </summary>
		public virtual bool PrettyPrint
		{
			get { return this.prettyPrint; }
			set { this.prettyPrint = value; }
		}

		/// <summary>
		/// Gets and sets the string to use for indentation
		/// </summary>
		public virtual string Tab
		{
			get { return this.tab; }
			set { this.tab = value; }
		}

		/// <summary>
		/// Gets and sets the line terminator string
		/// </summary>
		public virtual string NewLine
		{
			get { return this.newLine; }
			set { this.newLine = value; }
		}

		/// <summary>
		/// Gets and sets the maximum depth to be serialized.
		/// </summary>
		public virtual int MaxDepth
		{
			get { return this.maxDepth; }
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException("MaxDepth must be a positive integer as it controls the maximum nesting level of serialized objects.");
				}
				this.maxDepth = value;
			}
		}

		/// <summary>
		/// Gets and sets if should use XmlSerialization Attributes.
		/// </summary>
		/// <remarks>
		/// Respects XmlIgnoreAttribute, ...
		/// </remarks>
		public virtual bool UseXmlSerializationAttributes
		{
			get { return this.useXmlSerializationAttributes; }
			set { this.useXmlSerializationAttributes = value; }
		}

		/// <summary>
		/// Gets and sets a proxy formatter to use for DateTime serialization
		/// </summary>
		public virtual WriteDelegate<DateTime> DateTimeSerializer
		{
			get { return this.dateTimeSerializer; }
			set { this.dateTimeSerializer = value; }
		}

		public virtual WriteDelegate<byte[]> ByteArraySerializer
		{
			get { return this.byteArraySerializer; }
			set { this.byteArraySerializer = value; }
		}

		public virtual bool SerializeProperties {
			get; set;
		}

		public virtual bool EncodeEnumsAsNumber {
			get; set;
		}

		#endregion Properties
	}
}
