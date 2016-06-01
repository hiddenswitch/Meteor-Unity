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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace JsonFx.Json
{
	/// <summary>
	/// Reader for consuming JSON data
	/// </summary>
	public class JsonReader
	{
		#region Constants

		internal const string LiteralFalse = "false";
		internal const string LiteralTrue = "true";
		internal const string LiteralNull = "null";
		internal const string LiteralUndefined = "undefined";
		internal const string LiteralNotANumber = "NaN";
		internal const string LiteralPositiveInfinity = "Infinity";
		internal const string LiteralNegativeInfinity = "-Infinity";

		internal const char OperatorNegate = '-';
		internal const char OperatorUnaryPlus = '+';
		internal const char OperatorArrayStart = '[';
		internal const char OperatorArrayEnd = ']';
		internal const char OperatorObjectStart = '{';
		internal const char OperatorObjectEnd = '}';
		internal const char OperatorStringDelim = '"';
		internal const char OperatorStringDelimAlt = '\'';
		internal const char OperatorValueDelim = ',';
		internal const char OperatorNameDelim = ':';
		internal const char OperatorCharEscape = '\\';

		private const string CommentStart = "/*";
		private const string CommentEnd = "*/";
		private const string CommentLine = "//";
		private const string LineEndings = "\r\n";

		internal const string TypeGenericIDictionary = "System.Collections.Generic.IDictionary`2";

		private const string ErrorUnrecognizedToken = "Illegal JSON sequence.";
		private const string ErrorUnterminatedComment = "Unterminated comment block.";
		private const string ErrorUnterminatedObject = "Unterminated JSON object.";
		private const string ErrorUnterminatedArray = "Unterminated JSON array.";
		private const string ErrorUnterminatedString = "Unterminated JSON string.";
		private const string ErrorIllegalNumber = "Illegal JSON number.";
		private const string ErrorExpectedString = "Expected JSON string.";
		private const string ErrorExpectedObject = "Expected JSON object.";
		private const string ErrorExpectedArray = "Expected JSON array.";
		private const string ErrorExpectedPropertyName = "Expected JSON object property name.";
		private const string ErrorExpectedPropertyNameDelim = "Expected JSON object property name delimiter.";
		private const string ErrorGenericIDictionary = "Types which implement Generic IDictionary<TKey, TValue> also need to implement IDictionary to be deserialized. ({0})";
		private const string ErrorGenericIDictionaryKeys = "Types which implement Generic IDictionary<TKey, TValue> need to have string keys to be deserialized. ({0})";

		#endregion Constants

		#region Fields

		private readonly JsonReaderSettings Settings = new JsonReaderSettings();
		private readonly string Source = null;
		private readonly int SourceLength = 0;
		private int index;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="input">TextReader containing source</param>
		public JsonReader(TextReader input)
			: this(input, new JsonReaderSettings())
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="input">TextReader containing source</param>
		/// <param name="settings">JsonReaderSettings</param>
		public JsonReader(TextReader input, JsonReaderSettings settings)
		{
			this.Settings = settings;
			this.Source = input.ReadToEnd();
			this.SourceLength = this.Source.Length;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="input">Stream containing source</param>
		public JsonReader(Stream input)
			: this(input, new JsonReaderSettings())
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="input">Stream containing source</param>
		/// <param name="settings">JsonReaderSettings</param>
		public JsonReader(Stream input, JsonReaderSettings settings)
		{
			this.Settings = settings;

			using (StreamReader reader = new StreamReader(input, true))
			{
				this.Source = reader.ReadToEnd();
			}
			this.SourceLength = this.Source.Length;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="input">string containing source</param>
		public JsonReader(string input)
			: this(input, new JsonReaderSettings())
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="input">string containing source</param>
		/// <param name="settings">JsonReaderSettings</param>
		public JsonReader(string input, JsonReaderSettings settings)
		{
			this.Settings = settings;
			this.Source = input;
			this.SourceLength = this.Source.Length;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="input">StringBuilder containing source</param>
		public JsonReader(StringBuilder input)
			: this(input, new JsonReaderSettings())
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="input">StringBuilder containing source</param>
		/// <param name="settings">JsonReaderSettings</param>
		public JsonReader(StringBuilder input, JsonReaderSettings settings)
		{
			this.Settings = settings;
			this.Source = input.ToString();
			this.SourceLength = this.Source.Length;
		}

		#endregion Init

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
		[Obsolete("This has been deprecated in favor of JsonReaderSettings object")]
		public bool AllowNullValueTypes
		{
			get { return this.Settings.AllowNullValueTypes; }
			set { this.Settings.AllowNullValueTypes = value; }
		}

		/// <summary>
		/// Gets and sets the property name used for type hinting.
		/// </summary>
		[Obsolete("This has been deprecated in favor of JsonReaderSettings object")]
		public string TypeHintName
		{
			get { return this.Settings.TypeHintName; }
			set { this.Settings.TypeHintName = value; }
		}

		#endregion Properties

		#region Parsing Methods

		/// <summary>
		/// Convert from JSON string to Object graph
		/// </summary>
		/// <returns></returns>
		public object Deserialize()
		{
			return this.Deserialize((Type)null);
		}

		/// <summary>
		/// Convert from JSON string to Object graph
		/// </summary>
		/// <returns></returns>
		public object Deserialize(int start)
		{
			this.index = start;
			return this.Deserialize((Type)null);
		}

		/// <summary>
		/// Convert from JSON string to Object graph of specific Type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public object Deserialize(Type type)
		{
			// should this run through a preliminary test here?
			return this.Read(type, false);
		}

		/// <summary>
		/// Convert from JSON string to Object graph of specific Type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public T Deserialize<T>()
		{
			// should this run through a preliminary test here?
			return (T)this.Read(typeof(T), false);
		}

		/// <summary>
		/// Convert from JSON string to Object graph of specific Type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public object Deserialize(int start, Type type)
		{
			this.index = start;

			// should this run through a preliminary test here?
			return this.Read(type, false);
		}

		/// <summary>
		/// Convert from JSON string to Object graph of specific Type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public T Deserialize<T>(int start)
		{
			this.index = start;

			// should this run through a preliminary test here?
			return (T)this.Read(typeof(T), false);
		}

		private object Read(Type expectedType, bool typeIsHint)
		{
			if (expectedType == typeof(Object))
			{
				expectedType = null;
			}

			JsonToken token = this.Tokenize();

			switch (token)
			{
				case JsonToken.ObjectStart:
				{
					return this.ReadObject(typeIsHint ? null : expectedType);
				}
				case JsonToken.ArrayStart:
				{
					return this.ReadArray(typeIsHint ? null : expectedType);
				}
				case JsonToken.String:
				{
					return this.ReadString(typeIsHint ? null : expectedType);
				}
				case JsonToken.Number:
				{
					return this.ReadNumber(typeIsHint ? null : expectedType);
				}
				case JsonToken.False:
				{
					this.index += JsonReader.LiteralFalse.Length;
					return false;
				}
				case JsonToken.True:
				{
					this.index += JsonReader.LiteralTrue.Length;
					return true;
				}
				case JsonToken.Null:
				{
					this.index += JsonReader.LiteralNull.Length;
					return null;
				}
				case JsonToken.NaN:
				{
					this.index += JsonReader.LiteralNotANumber.Length;
					return Double.NaN;
				}
				case JsonToken.PositiveInfinity:
				{
					this.index += JsonReader.LiteralPositiveInfinity.Length;
					return Double.PositiveInfinity;
				}
				case JsonToken.NegativeInfinity:
				{
					this.index += JsonReader.LiteralNegativeInfinity.Length;
					return Double.NegativeInfinity;
				}
				case JsonToken.Undefined:
				{
					this.index += JsonReader.LiteralUndefined.Length;
					return null;
				}
				case JsonToken.End:
				default:
				{
					return null;
				}
			}
		}

		private object ReadObject(Type objectType)
		{
			if (this.Source[this.index] != JsonReader.OperatorObjectStart)
			{
				throw new JsonDeserializationException(JsonReader.ErrorExpectedObject, this.index);
			}

			// If this is a Date, then we should be reading an EJSON Date
			if (objectType != null
				&& objectType == typeof(DateTime)) {
				// Read an EJSON result instead
				var ejsonDateResult = this.ReadObject(typeof(Meteor.EJSON.EJSONDate)) as Meteor.EJSON.EJSONDate;
				var returnDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(ejsonDateResult.date * 10000L);
				return returnDate;
			}

			// If this is a byte[] destination and we're reading an object, we're handling an EJSON UInt8 array
			if (objectType != null
				&& objectType == typeof(byte[])) {
				var ejsonBinaryResult = this.ReadObject(typeof(Meteor.EJSON.EJSONUInt8Array)) as Meteor.EJSON.EJSONUInt8Array;
				var returnBytes = System.Convert.FromBase64String(ejsonBinaryResult.binary);
				return returnBytes;
			}

			Type genericDictionaryType = null;
			Dictionary<string, MemberInfo> memberMap = null;
			Object result;

			if (objectType != null)
			{
				result = this.Settings.Coercion.InstantiateObject(objectType, out memberMap);

				if (memberMap == null)
				{
					// this allows specific IDictionary<string, T> to deserialize T
					Type genericDictionary = objectType.GetInterface(JsonReader.TypeGenericIDictionary);
					if (genericDictionary != null)
					{
						Type[] genericArgs = genericDictionary.GetGenericArguments();
						if (genericArgs.Length == 2)
						{
							if (genericArgs[0] != typeof(String))
							{
								throw new JsonDeserializationException(
									String.Format(JsonReader.ErrorGenericIDictionaryKeys, objectType),
									this.index);
							}

							if (genericArgs[1] != typeof(Object))
							{
								genericDictionaryType = genericArgs[1];
							}
						}
					}
				}
			}
			else
			{
				result = new Dictionary<String, Object>();
			}

			JsonToken token;
			do
			{
				Type memberType;
				MemberInfo memberInfo;

				// consume opening brace or delim
				this.index++;
				if (this.index >= this.SourceLength)
				{
					throw new JsonDeserializationException(JsonReader.ErrorUnterminatedObject, this.index);
				}

				// get next token
				token = this.Tokenize(this.Settings.AllowUnquotedObjectKeys);
				if (token == JsonToken.ObjectEnd)
				{
					break;
				}

				if (token != JsonToken.String && token != JsonToken.UnquotedName)
				{
					throw new JsonDeserializationException(JsonReader.ErrorExpectedPropertyName, this.index);
				}

				// parse object member value
				string memberName = (token == JsonToken.String) ?
					(String)this.ReadString(null) :
					this.ReadUnquotedKey();

				if (genericDictionaryType == null && memberMap != null)
				{
					// determine the type of the property/field
					memberType = TypeCoercionUtility.GetMemberInfo(memberMap, memberName, out memberInfo);
				}
				else
				{
					memberType = genericDictionaryType;
					memberInfo = null;
				}

				// get next token
				token = this.Tokenize();
				if (token != JsonToken.NameDelim)
				{
					throw new JsonDeserializationException(JsonReader.ErrorExpectedPropertyNameDelim, this.index);
				}

				// consume delim
				this.index++;
				if (this.index >= this.SourceLength)
				{
					throw new JsonDeserializationException(JsonReader.ErrorUnterminatedObject, this.index);
				}
			
				object value = this.Read(memberType, false);

				if (result is IDictionary)
				{
					if (objectType == null && this.Settings.IsTypeHintName(memberName))
					{
						result = this.Settings.Coercion.ProcessTypeHint((IDictionary)result, value as string, out objectType, out memberMap);
					}
					else
					{
						((IDictionary)result)[memberName] = value;
					}
				}
				else if (objectType.GetInterface(JsonReader.TypeGenericIDictionary) != null)
				{
					throw new JsonDeserializationException(
						String.Format(JsonReader.ErrorGenericIDictionary, objectType),
						this.index);
				}
				else
				{
					this.Settings.Coercion.SetMemberValue(result, memberType, memberInfo, value);
				}

				// get next token
				token = this.Tokenize();
			} while (token == JsonToken.ValueDelim);

			if (token != JsonToken.ObjectEnd)
			{
				throw new JsonDeserializationException(JsonReader.ErrorUnterminatedObject, this.index);
			}

			// consume closing brace
			this.index++;

			return result;
		}

		private IEnumerable ReadArray(Type arrayType)
		{
			if (this.Source[this.index] != JsonReader.OperatorArrayStart)
			{
				throw new JsonDeserializationException(JsonReader.ErrorExpectedArray, this.index);
			}

			bool isArrayItemTypeSet = (arrayType != null);
			bool isArrayTypeAHint = !isArrayItemTypeSet;
			Type arrayItemType = null;

			if (isArrayItemTypeSet)
			{
				if (arrayType.HasElementType)
				{
					arrayItemType = arrayType.GetElementType();
				}
				else if (arrayType.IsGenericType)
				{
					Type[] generics = arrayType.GetGenericArguments();
					if (generics.Length == 1)
					{
						// could use the first or last, but this more correct
						arrayItemType = generics[0];
					}
				}
			}

			// using ArrayList since has .ToArray(Type) method
			// cannot create generic list at runtime
			ArrayList jsArray = new ArrayList();

			JsonToken token;
			do
			{
				// consume opening bracket or delim
				this.index++;
				if (this.index >= this.SourceLength)
				{
					throw new JsonDeserializationException(JsonReader.ErrorUnterminatedArray, this.index);
				}

				// get next token
				token = this.Tokenize();
				if (token == JsonToken.ArrayEnd)
				{
					break;
				}

				// parse array item
				object value = this.Read(arrayItemType, isArrayTypeAHint);
				jsArray.Add(value);

				// establish if array is of common type
				if (value == null)
				{
					if (arrayItemType != null && arrayItemType.IsValueType)
					{
						// use plain object to hold null
						arrayItemType = null;
					}
					isArrayItemTypeSet = true;
				}
				else if (arrayItemType != null && !arrayItemType.IsAssignableFrom(value.GetType()))
				{
					if (value.GetType().IsAssignableFrom(arrayItemType))
					{
						// attempt to use the more general type
						arrayItemType = value.GetType();
					}
					else
					{
						// use plain object to hold value
						arrayItemType = null;
						isArrayItemTypeSet = true;
					}
				}
				else if (!isArrayItemTypeSet)
				{
					// try out a hint type
					// if hasn't been set before
					arrayItemType = value.GetType();
					isArrayItemTypeSet = true;
				}

				// get next token
				token = this.Tokenize();
			} while (token == JsonToken.ValueDelim);

			if (token != JsonToken.ArrayEnd)
			{
				throw new JsonDeserializationException(JsonReader.ErrorUnterminatedArray, this.index);
			}

			// consume closing bracket
			this.index++;

			// TODO: optimize to reduce number of conversions on lists

			if (arrayItemType != null && arrayItemType != typeof(object))
			{
				// if all items are of same type then convert to array of that type
				return jsArray.ToArray(arrayItemType);
			}

			// convert to an object array for consistency
			return jsArray.ToArray();
		}

		/// <summary>
		/// Reads an unquoted JSON object key
		/// </summary>
		/// <returns></returns>
		private string ReadUnquotedKey()
		{
			int start = this.index;
			do
			{
				// continue scanning until reach a valid token
				this.index++;
			} while (this.Tokenize(true) == JsonToken.UnquotedName);

			return this.Source.Substring(start, this.index - start);
		}

		/// <summary>
		/// Reads a JSON string
		/// </summary>
		/// <param name="expectedType"></param>
		/// <returns>string or value which is represented as a string in JSON</returns>
		private object ReadString(Type expectedType)
		{
			if (this.Source[this.index] != JsonReader.OperatorStringDelim &&
				this.Source[this.index] != JsonReader.OperatorStringDelimAlt)
			{
				throw new JsonDeserializationException(JsonReader.ErrorExpectedString, this.index);
			}

			char startStringDelim = this.Source[this.index];

			// consume opening quote
			this.index++;
			if (this.index >= this.SourceLength)
			{
				throw new JsonDeserializationException(JsonReader.ErrorUnterminatedString, this.index);
			}

			int start = this.index;
			StringBuilder builder = new StringBuilder();

			while (this.Source[this.index] != startStringDelim)
			{
				if (this.Source[this.index] == JsonReader.OperatorCharEscape)
				{
					// copy chunk before decoding
					builder.Append(this.Source, start, this.index - start);

					// consume escape char
					this.index++;
					if (this.index >= this.SourceLength)
					{
						throw new JsonDeserializationException(JsonReader.ErrorUnterminatedString, this.index);
					}

					// decode
					switch (this.Source[this.index])
					{
						case '0':
						{
							// don't allow NULL char '\0'
							// causes CStrings to terminate
							break;
						}
						case 'b':
						{
							// backspace
							builder.Append('\b');
							break;
						}
						case 'f':
						{
							// formfeed
							builder.Append('\f');
							break;
						}
						case 'n':
						{
							// newline
							builder.Append('\n');
							break;
						}
						case 'r':
						{
							// carriage return
							builder.Append('\r');
							break;
						}
						case 't':
						{
							// tab
							builder.Append('\t');
							break;
						}
						case 'u':
						{
							// Unicode escape sequence
							// e.g. Copyright: "\u00A9"

							// unicode ordinal
							int utf16;
							if (this.index+4 < this.SourceLength &&
								Int32.TryParse(
									this.Source.Substring(this.index+1, 4),
									NumberStyles.AllowHexSpecifier,
									NumberFormatInfo.InvariantInfo,
									out utf16))
							{
								builder.Append(Char.ConvertFromUtf32(utf16));
								this.index += 4;
							}
							else
							{
								// using FireFox style recovery, if not a valid hex
								// escape sequence then treat as single escaped 'u'
								// followed by rest of string
								builder.Append(this.Source[this.index]);
							}
							break;
						}
						default:
						{
							builder.Append(this.Source[this.index]);
							break;
						}
					}

					this.index++;
					if (this.index >= this.SourceLength)
					{
						throw new JsonDeserializationException(JsonReader.ErrorUnterminatedString, this.index);
					}

					start = this.index;
				}
				else
				{
					// next char
					this.index++;
					if (this.index >= this.SourceLength)
					{
						throw new JsonDeserializationException(JsonReader.ErrorUnterminatedString, this.index);
					}
				}
			}

			// copy rest of string
			builder.Append(this.Source, start, this.index-start);

			// consume closing quote
			this.index++;

			if (expectedType != null && expectedType != typeof(String))
			{
				return this.Settings.Coercion.CoerceType(expectedType, builder.ToString());
			}

			return builder.ToString();
		}

		private object ReadNumber(Type expectedType)
		{
			bool hasDecimal = false;
			bool hasExponent = false;
			int start = this.index;
			int precision = 0;
			int exponent = 0;

			// If this is a timespan do the microtime conversion
			if (expectedType != null
				&& expectedType == typeof(TimeSpan)) {
				object number = this.ReadNumber(typeof(long));
				return new TimeSpan((long)((long)((int)number) * 10000L));
			}

			// optional minus part
			if (this.Source[this.index] == JsonReader.OperatorNegate)
			{
				// consume sign
				this.index++;
				if (this.index >= this.SourceLength || !Char.IsDigit(this.Source[this.index]))
					throw new JsonDeserializationException(JsonReader.ErrorIllegalNumber, this.index);
			}

			// integer part
			while ((this.index < this.SourceLength) && Char.IsDigit(this.Source[this.index]))
			{
				// consume digit
				this.index++;
			}

			// optional decimal part
			if ((this.index < this.SourceLength) && (this.Source[this.index] == '.'))
			{
				hasDecimal = true;

				// consume decimal
				this.index++;
				if (this.index >= this.SourceLength || !Char.IsDigit(this.Source[this.index]))
				{
					throw new JsonDeserializationException(JsonReader.ErrorIllegalNumber, this.index);
				}

				// fraction part
				while (this.index < this.SourceLength && Char.IsDigit(this.Source[this.index]))
				{
					// consume digit
					this.index++;
				}
			}

			// note the number of significant digits
			precision = this.index-start - (hasDecimal ? 1 : 0);

			// optional exponent part
			if (this.index < this.SourceLength && (this.Source[this.index] == 'e' || this.Source[this.index] == 'E'))
			{
				hasExponent = true;

				// consume 'e'
				this.index++;
				if (this.index >= this.SourceLength)
				{
					throw new JsonDeserializationException(JsonReader.ErrorIllegalNumber, this.index);
				}

				int expStart = this.index;

				// optional minus/plus part
				if (this.Source[this.index] == JsonReader.OperatorNegate || this.Source[this.index] == JsonReader.OperatorUnaryPlus)
				{
					// consume sign
					this.index++;
					if (this.index >= this.SourceLength || !Char.IsDigit(this.Source[this.index]))
					{
						throw new JsonDeserializationException(JsonReader.ErrorIllegalNumber, this.index);
					}
				}
				else
				{
					if (!Char.IsDigit(this.Source[this.index]))
					{
						throw new JsonDeserializationException(JsonReader.ErrorIllegalNumber, this.index);
					}
				}

				// exp part
				while (this.index < this.SourceLength && Char.IsDigit(this.Source[this.index]))
				{
					// consume digit
					this.index++;
				}

				Int32.TryParse(this.Source.Substring(expStart, this.index-expStart), NumberStyles.Integer,
					NumberFormatInfo.InvariantInfo, out exponent);
			}

			// at this point, we have the full number string and know its characteristics
			string numberString = this.Source.Substring(start, this.index - start);

			if (!hasDecimal && !hasExponent && precision < 19)
			{
				// is Integer value

				// parse as most flexible
				decimal number = Decimal.Parse(
					numberString,
					NumberStyles.Integer,
					NumberFormatInfo.InvariantInfo);

				if (number >= Int32.MinValue && number <= Int32.MaxValue)
				{
					// use most common
					return (int)number;
				}
				if (number >= Int64.MinValue && number <= Int64.MaxValue)
				{
					// use more flexible
					return (long)number;
				}

				// use most flexible
				return number;
			}
			else
			{
				// is Floating Point value

				if (expectedType == typeof(Decimal))
				{
					// special case since Double does not convert to Decimal
					return Decimal.Parse(
						numberString,
						NumberStyles.Float,
						NumberFormatInfo.InvariantInfo);
				}

				// use native EcmaScript number (IEEE 754)
				double number = Double.Parse(
					numberString,
					NumberStyles.Float,
					NumberFormatInfo.InvariantInfo);

				if (expectedType != null)
				{
					return this.Settings.Coercion.CoerceType(expectedType, number);
				}

				return number;
			}
		}

		#endregion Parsing Methods

		#region Static Methods

		/// <summary>
		/// A fast method for deserializing an object from JSON
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static object Deserialize(string value)
		{
			return JsonReader.Deserialize(value, 0, null);
		}

		/// <summary>
		/// A fast method for deserializing an object from JSON
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T Deserialize<T>(string value)
		{
			return (T)JsonReader.Deserialize(value, 0, typeof(T));
		}

		/// <summary>
		/// A fast method for deserializing an object from JSON
		/// </summary>
		/// <param name="value"></param>
		/// <param name="start"></param>
		/// <returns></returns>
		public static object Deserialize(string value, int start)
		{
			return JsonReader.Deserialize(value, start, null);
		}

		/// <summary>
		/// A fast method for deserializing an object from JSON
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="start"></param>
		/// <returns></returns>
		public static T Deserialize<T>(string value, int start)
		{
			return (T)JsonReader.Deserialize(value, start, typeof(T));
		}

		/// <summary>
		/// A fast method for deserializing an object from JSON
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static object Deserialize(string value, Type type)
		{
			return JsonReader.Deserialize(value, 0, type);
		}

		/// <summary>
		/// A fast method for deserializing an object from JSON
		/// </summary>
		/// <param name="value">source text</param>
		/// <param name="start">starting position</param>
		/// <param name="type">expected type</param>
		/// <returns></returns>
		public static object Deserialize(string value, int start, Type type)
		{
			return (new JsonReader(value)).Deserialize(start, type);
		}

		#endregion Static Methods

		#region Tokenizing Methods

		private JsonToken Tokenize()
		{
			// unquoted object keys are only allowed in object properties
			return this.Tokenize(false);
		}

		private JsonToken Tokenize(bool allowUnquotedString)
		{
			if (this.index >= this.SourceLength)
			{
				return JsonToken.End;
			}

			// skip whitespace
			while (Char.IsWhiteSpace(this.Source[this.index]))
			{
				this.index++;
				if (this.index >= this.SourceLength)
				{
					return JsonToken.End;
				}
			}

			#region Skip Comments

			// skip block and line comments
			if (this.Source[this.index] == JsonReader.CommentStart[0])
			{
				if (this.index+1 >= this.SourceLength)
				{
					throw new JsonDeserializationException(JsonReader.ErrorUnrecognizedToken, this.index);
				}

				// skip over first char of comment start
				this.index++;

				bool isBlockComment = false;
				if (this.Source[this.index] == JsonReader.CommentStart[1])
				{
					isBlockComment = true;
				}
				else if (this.Source[this.index] != JsonReader.CommentLine[1])
				{
					throw new JsonDeserializationException(JsonReader.ErrorUnrecognizedToken, this.index);
				}
				// skip over second char of comment start
				this.index++;

				if (isBlockComment)
				{
					// store index for unterminated case
					int commentStart = this.index-2;

					if (this.index+1 >= this.SourceLength)
					{
						throw new JsonDeserializationException(JsonReader.ErrorUnterminatedComment, commentStart);
					}

					// skip over everything until reach block comment ending
					while (this.Source[this.index] != JsonReader.CommentEnd[0] ||
						this.Source[this.index+1] != JsonReader.CommentEnd[1])
					{
						this.index++;
						if (this.index+1 >= this.SourceLength)
						{
							throw new JsonDeserializationException(JsonReader.ErrorUnterminatedComment, commentStart);
						}
					}

					// skip block comment end token
					this.index += 2;
					if (this.index >= this.SourceLength)
					{
						return JsonToken.End;
					}
				}
				else
				{
					// skip over everything until reach line ending
					while (JsonReader.LineEndings.IndexOf(this.Source[this.index]) < 0)
					{
						this.index++;
						if (this.index >= this.SourceLength)
						{
							return JsonToken.End;
						}
					}
				}

				// skip whitespace again
				while (Char.IsWhiteSpace(this.Source[this.index]))
				{
					this.index++;
					if (this.index >= this.SourceLength)
					{
						return JsonToken.End;
					}
				}
			}

			#endregion Skip Comments

			// consume positive signing (as is extraneous)
			if (this.Source[this.index] == JsonReader.OperatorUnaryPlus)
			{
				this.index++;
				if (this.index >= this.SourceLength)
				{
					return JsonToken.End;
				}
			}

			switch (this.Source[this.index])
			{
				case JsonReader.OperatorArrayStart:
				{
					return JsonToken.ArrayStart;
				}
				case JsonReader.OperatorArrayEnd:
				{
					return JsonToken.ArrayEnd;
				}
				case JsonReader.OperatorObjectStart:
				{
					return JsonToken.ObjectStart;
				}
				case JsonReader.OperatorObjectEnd:
				{
					return JsonToken.ObjectEnd;
				}
				case JsonReader.OperatorStringDelim:
				case JsonReader.OperatorStringDelimAlt:
				{
					return JsonToken.String;
				}
				case JsonReader.OperatorValueDelim:
				{
					return JsonToken.ValueDelim;
				}
				case JsonReader.OperatorNameDelim:
				{
					return JsonToken.NameDelim;
				}
				default:
				{
					break;
				}
			}

			// number
			if (Char.IsDigit(this.Source[this.index]) ||
				((this.Source[this.index] == JsonReader.OperatorNegate) && (this.index+1 < this.SourceLength) && Char.IsDigit(this.Source[this.index+1])))
			{
				return JsonToken.Number;
			}

			// "false" literal
			if (this.MatchLiteral(JsonReader.LiteralFalse))
			{
				return JsonToken.False;
			}

			// "true" literal
			if (this.MatchLiteral(JsonReader.LiteralTrue))
			{
				return JsonToken.True;
			}

			// "null" literal
			if (this.MatchLiteral(JsonReader.LiteralNull))
			{
				return JsonToken.Null;
			}

			// "NaN" literal
			if (this.MatchLiteral(JsonReader.LiteralNotANumber))
			{
				return JsonToken.NaN;
			}

			// "Infinity" literal
			if (this.MatchLiteral(JsonReader.LiteralPositiveInfinity))
			{
				return JsonToken.PositiveInfinity;
			}

			// "-Infinity" literal
			if (this.MatchLiteral(JsonReader.LiteralNegativeInfinity))
			{
				return JsonToken.NegativeInfinity;
			}

			// "undefined" literal
			if (this.MatchLiteral(JsonReader.LiteralUndefined))
			{
				return JsonToken.Undefined;
			}

			if (allowUnquotedString)
			{
				return JsonToken.UnquotedName;
			}

			throw new JsonDeserializationException(JsonReader.ErrorUnrecognizedToken, this.index);
		}

		/// <summary>
		/// Determines if the next token is the given literal
		/// </summary>
		/// <param name="literal"></param>
		/// <returns></returns>
		private bool MatchLiteral(string literal)
		{
			int literalLength = literal.Length;
			for (int i=0, j=this.index; i<literalLength && j<this.SourceLength; i++, j++)
			{
				if (literal[i] != this.Source[j])
				{
					return false;
				}
			}

			return true;
		}

		#endregion Tokenizing Methods

		#region Type Methods

		/// <summary>
		/// Converts a value into the specified type using type inference.
		/// </summary>
		/// <typeparam name="T">target type</typeparam>
		/// <param name="value">value to convert</param>
		/// <param name="typeToMatch">example object to get the type from</param>
		/// <returns></returns>
		public static T CoerceType<T>(object value, T typeToMatch)
		{
			return (T)new TypeCoercionUtility().CoerceType(typeof(T), value);
		}

		/// <summary>
		/// Converts a value into the specified type.
		/// </summary>
		/// <typeparam name="T">target type</typeparam>
		/// <param name="value">value to convert</param>
		/// <returns></returns>
		public static T CoerceType<T>(object value)
		{
			return (T)new TypeCoercionUtility().CoerceType(typeof(T), value);
		}

		/// <summary>
		/// Converts a value into the specified type.
		/// </summary>
		/// <param name="targetType">target type</param>
		/// <param name="value">value to convert</param>
		/// <returns></returns>
		public static object CoerceType(Type targetType, object value)
		{
			return new TypeCoercionUtility().CoerceType(targetType, value);
		}

		#endregion Type Methods
	}
}
