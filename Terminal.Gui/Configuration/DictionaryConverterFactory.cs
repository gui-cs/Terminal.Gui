using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Terminal.Gui.Configuration {
	public class DictionaryConverterFactory : JsonConverterFactory {
		public override bool CanConvert (Type typeToConvert)
		{
			return typeToConvert.IsClass && typeToConvert.GetDictionaryKeyValueType () != null && typeToConvert.GetConstructor (Type.EmptyTypes) != null;
		}

		public override JsonConverter CreateConverter (Type typeToConvert, JsonSerializerOptions options)
		{
			var keyValueTypes = typeToConvert.GetDictionaryKeyValueType ();
			var converterType = typeof (DictionaryAsArrayConverter<,,>).MakeGenericType (typeToConvert, keyValueTypes.Value.Key, keyValueTypes.Value.Value);
			return (JsonConverter)Activator.CreateInstance (converterType);
		}
	}

	public class DictionaryAsArrayConverter<TKey, TValue> : DictionaryAsArrayConverter<Dictionary<TKey, TValue>, TKey, TValue> {
	}

	public class DictionaryAsArrayConverter<TDictionary, TKey, TValue> : JsonConverter<TDictionary> where TDictionary : class, IDictionary<TKey, TValue>, new() {
		struct KeyValueDTO {
			public TKey Key { get; set; }
			public TValue Value { get; set; }
		}

		public override TDictionary Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var list = JsonSerializer.Deserialize<List<KeyValueDTO>> (ref reader, options);
			if (list == null)
				return null;
			var dictionary = typeToConvert == typeof (Dictionary<TKey, TValue>) ? (TDictionary)(object)new Dictionary<TKey, TValue> (list.Count) : new TDictionary ();
			foreach (var pair in list)
				dictionary.Add (pair.Key, pair.Value);
			return dictionary;
		}

		public override void Write (Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options)
		{
			JsonSerializer.Serialize (writer, value.Select (p => new KeyValueDTO { Key = p.Key, Value = p.Value }), options);
		}
	}

	public static class TypeExtensions {
		public static IEnumerable<Type> GetInterfacesAndSelf (this Type type)
		{
			if (type == null)
				throw new ArgumentNullException ();
			if (type.IsInterface)
				return new [] { type }.Concat (type.GetInterfaces ());
			else
				return type.GetInterfaces ();
		}

		public static KeyValuePair<Type, Type>? GetDictionaryKeyValueType (this Type type)
		{
			KeyValuePair<Type, Type>? types = null;
			foreach (var pair in type.GetDictionaryKeyValueTypes ()) {
				if (types == null)
					types = pair;
				else
					return null;
			}
			return types;
		}

		public static IEnumerable<KeyValuePair<Type, Type>> GetDictionaryKeyValueTypes (this Type type)
		{
			foreach (Type intType in type.GetInterfacesAndSelf ()) {
				if (intType.IsGenericType
				    && intType.GetGenericTypeDefinition () == typeof (IDictionary<,>)) {
					var args = intType.GetGenericArguments ();
					if (args.Length == 2)
						yield return new KeyValuePair<Type, Type> (args [0], args [1]);
				}
			}
		}
	}
}
