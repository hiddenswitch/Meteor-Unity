using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

namespace Extensions {
    public static partial class ObjectExtensions
    {
        /// <summary>
        /// Serialize the object to JSON.
        /// </summary>
        /// <param name="source">Source.</param>
        public static string Serialize(this object source)
        {
            return JsonFx.Json.JsonWriter.Serialize(source);
        }

        /// <summary>
        /// Clone the instance by serializing and deserializing it.
        /// </summary>
        /// <param name="source">Source.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T Clone<T>(this T source)
            where T : new()
        {
            return source.Serialize().Deserialize<T>();
        }
        
		public static IDictionary ToDictionary(this object value) {
			var dict = new Dictionary<string, object> ();
			var type = value.GetType ();

			if (typeof(IDictionary).IsAssignableFrom(type)) {
				return value as IDictionary;
			}

			bool anonymousType = false;

			// serialize public properties
			PropertyInfo[] properties = type.GetProperties();
			foreach (PropertyInfo property in properties)
			{
				if (!property.CanRead)
				{
					continue;
				}

				if (!property.CanWrite && !anonymousType)
				{
					continue;
				}

//				if (JsonFx.Json.JsonWriter.IsIgnored(type, property, value))
//				{
//					continue;
//				}

				object propertyValue = property.GetValue(value, null);
//				if (this.IsDefaultValue(property, propertyValue))
//				{
//					continue;
//				}

//				if (appendDelim)
//				{
//					this.WriteObjectPropertyDelim();
//				}
//				else
//				{
//					appendDelim = true;
//				}

				string propertyName = property.Name;

				dict[propertyName] = propertyValue;
			}

			// serialize public fields
			FieldInfo[] fields = type.GetFields();
			foreach (FieldInfo field in fields)
			{
				if (!field.IsPublic || field.IsStatic)
				{
					continue;
				}

//				if (this.IsIgnored(type, field, value))
//				{
//					continue;
//				}
//
				object fieldValue = field.GetValue(value);
//				if (this.IsDefaultValue(field, fieldValue))
//				{
//					continue;
//				}
//
//				if (appendDelim)
//				{
//					this.WriteObjectPropertyDelim();
//					this.WriteLine();
//				}
//				else
//				{
//					appendDelim = true;
//				}

				// use Attributes here to control naming
				string fieldName = field.Name;

				dict[fieldName] = fieldValue;
			}

			return dict;
		}

        public static T Coerce<T>(this object source)
		{
			return (T)(new JsonFx.Json.TypeCoercionUtility()).CoerceType(typeof(T), source);
		}
    }
}

