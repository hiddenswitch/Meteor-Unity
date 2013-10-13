using System;
using System.Collections.Generic;
using System.Reflection;

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
        
        public static T Coerce<T>(this object source)
		{
			return (T)(new JsonFx.Json.TypeCoercionUtility()).CoerceType(typeof(T), source);
		}
    }
}

