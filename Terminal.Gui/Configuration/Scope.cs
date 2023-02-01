using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;


#nullable enable

namespace Terminal.Gui.Configuration {
	public static partial class ConfigurationManager {
		/// <summary>
		/// Defines a configuration settings scope. Classes that inherit from this abstract class can be used to define
		/// scopes for configuration settings. Each scope is a JSON object that contains a set of configuration settings.
		/// </summary>
		public abstract class Scope : IDictionary<string, ConfigProperty> {
			/// <summary>
			/// Crates a new instance.
			/// </summary>
			public Scope ()
			{
				var props = ConfigurationManager._allConfigProperties!.Where (cp =>
					(cp.Value.PropertyInfo?.GetCustomAttribute (typeof (SerializableConfigurationProperty))
					as SerializableConfigurationProperty)?.Scope == this.GetType ());
				Properties = props.ToDictionary (dict => dict.Key,
					dict => new ConfigProperty () { PropertyInfo = dict.Value.PropertyInfo, PropertyValue = null }, StringComparer.InvariantCultureIgnoreCase);
			}

			internal object? UpdateFrom (Scope source)
			{
				foreach (var prop in source) {
					if (ContainsKey (prop.Key))
						this [prop.Key].PropertyValue = this [prop.Key].UpdateValueFrom (prop.Value.PropertyValue!);
					else {
						this [prop.Key].PropertyValue = prop.Value.PropertyValue;
					}
				}
				return this;
			}

			internal void RetrieveValues ()
			{
				foreach (var p in this.Where (cp => cp.Value.PropertyInfo != null)) {
					p.Value.RetrieveValue ();
				}

			}

			/// <summary>
			/// Applies all congiguration properties of this scope that have a non-null <see cref="ConfigProperty.PropertyValue"/>.
			/// </summary>
			/// <returns><see langword="true"/> if any property was non-null and was set.</returns>
			public bool Apply()
			{
				bool set = false;
				foreach (var p in this.Where (t => t.Value != null && t.Value.PropertyValue != null)) {
					set = p.Value.Apply ();
				}
				return set;
			}

			/// <summary>
			/// Gets the dictionary of <see cref="ConfigProperty"/> objects for this scope.
			/// </summary>
			/// <remarks>
			/// This dictionary is populated in the constructor of the <see cref="Scope"/> class with the properties
			/// attributed with the <see cref="SerializableConfigurationProperty"/> attribute 
			/// and whose <see cref="SerializableConfigurationProperty.Scope"/> 
			/// is the same as the type of this scope.
			/// </remarks>
			[JsonIgnore]
			public Dictionary<string, ConfigProperty> Properties { get; set; }

			// We are derived from IDictionary so that the ColorSchemes JSON element 
			// has a list of ColorScheme objects. 
			#region IDictionary			
			/// <inheritdoc/>
			public ICollection<string> Keys => ((IDictionary<string, ConfigProperty>)Properties).Keys;
			/// <inheritdoc/>
			public ICollection<ConfigProperty> Values => ((IDictionary<string, ConfigProperty>)Properties).Values;
			/// <inheritdoc/>
			public int Count => ((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Count;
			/// <inheritdoc/>
			public bool IsReadOnly => ((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).IsReadOnly;

			/// <inheritdoc/>
			[JsonIgnore]
			public ConfigProperty this [string index] {
				get {
					return Properties [index];
				}
				set {
					Properties [index] = value;
				}
			}
			
			/// <inheritdoc/>
			public void Add (string key, ConfigProperty value)
			{
				((IDictionary<string, ConfigProperty>)Properties).Add (key, value);
			}
			/// <inheritdoc/>
			public bool ContainsKey (string key)
			{
				return ((IDictionary<string, ConfigProperty>)Properties).ContainsKey (key);
			}
			/// <inheritdoc/>
			public bool Remove (string key)
			{
				return ((IDictionary<string, ConfigProperty>)Properties).Remove (key);
			}
			/// <inheritdoc/>
			public bool TryGetValue (string key, out ConfigProperty value)
			{
				return ((IDictionary<string, ConfigProperty>)Properties).TryGetValue (key, out value!);
			}
			/// <inheritdoc/>
			public void Add (KeyValuePair<string, ConfigProperty> item)
			{
				((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Add (item);
			}
			/// <inheritdoc/>
			public void Clear ()
			{
				((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Clear ();
			}
			/// <inheritdoc/>
			public bool Contains (KeyValuePair<string, ConfigProperty> item)
			{
				return ((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Contains (item);
			}
			/// <inheritdoc/>
			public void CopyTo (KeyValuePair<string, ConfigProperty> [] array, int arrayIndex)
			{
				((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).CopyTo (array, arrayIndex);
			}
			/// <inheritdoc/>
			public bool Remove (KeyValuePair<string, ConfigProperty> item)
			{
				return ((ICollection<KeyValuePair<string, ConfigProperty>>)Properties).Remove (item);
			}
			/// <inheritdoc/>
			public IEnumerator<KeyValuePair<string, ConfigProperty>> GetEnumerator ()
			{
				return ((IEnumerable<KeyValuePair<string, ConfigProperty>>)Properties).GetEnumerator ();
			}

			IEnumerator IEnumerable.GetEnumerator ()
			{
				return ((IEnumerable)Properties).GetEnumerator ();
			}

			#endregion
		}


		/// <summary>
		/// Converts <see cref="Scope"/> instances to/from JSON. Does all the heavy lifting of reading/writing
		/// config data to/from <see cref="ConfigurationManager"/> JSON documents.
		/// </summary>
		/// <typeparam name="Tscope"></typeparam>
		public class ScopeJsonConverter<Tscope> : JsonConverter<Tscope> {
			// See: https://stackoverflow.com/questions/60830084/how-to-pass-an-argument-by-reference-using-reflection
			internal abstract class ReadHelper {
				public abstract object? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
			}

			internal class ReadHelper<converterT> : ReadHelper {
				private readonly ReadDelegate _readDelegate;
				private delegate converterT ReadDelegate (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
				public ReadHelper (object converter)
					=> _readDelegate = (ReadDelegate)Delegate.CreateDelegate (typeof (ReadDelegate), converter, "Read");
				public override object? Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
					=> _readDelegate.Invoke (ref reader, type, options);
			}

			/// <inheritdoc/>
			public override Tscope Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.StartObject) {
					throw new JsonException ();
				}

				var scope = Activator.CreateInstance (typeof (Tscope)) as Scope;
				// Get ConfigProperty store for this Scope type

				//var scopeProperties = typeToConvert!.GetProperty ("Properties")?.GetValue (scope) as Dictionary<string, ConfigProperty>;
				while (reader.Read ()) {
					if (reader.TokenType == JsonTokenType.EndObject) {
						return (Tscope)(object)scope!;
					}
					if (reader.TokenType != JsonTokenType.PropertyName) {
						throw new JsonException ();
					}
					var propertyName = reader.GetString ();
					reader.Read ();

					if (propertyName != null && scope!.TryGetValue (propertyName, out var configProp)) {
						// This property name was found in the Scope's ScopeProperties dictionary
						// Figure out if it needs a JsonConverter and if so, create one
						var propertyType = configProp?.PropertyInfo?.PropertyType!;
						if (configProp?.PropertyInfo?.GetCustomAttribute (typeof (JsonConverterAttribute)) is JsonConverterAttribute jca) {
							var converter = Activator.CreateInstance (jca.ConverterType!)!;
							if (converter.GetType ().BaseType == typeof (JsonConverterFactory)) {
								var factory = (JsonConverterFactory)converter;
								if (propertyType != null && factory.CanConvert (propertyType)) {
									converter = factory.CreateConverter (propertyType, options);
								}
							}
							var readHelper = Activator.CreateInstance ((Type?)typeof (ReadHelper<>).MakeGenericType (typeof (Tscope), propertyType!)!, converter) as ReadHelper;
							scope! [propertyName].PropertyValue = readHelper?.Read (ref reader, propertyType!, options);
						} else {
							scope! [propertyName].PropertyValue = JsonSerializer.Deserialize (ref reader, propertyType!, options);
						}
					} else {
						if (scope!.GetType ().GetCustomAttribute (typeof (JsonIncludeAttribute)) != null) {
							if (scope.GetType ().GetCustomAttribute (typeof (JsonPropertyNameAttribute)) != null) {
								propertyName = scope.GetType ().GetCustomAttribute (typeof (JsonPropertyNameAttribute))?.ToString ();
							}
							var prop = scope.GetType ().GetProperty (propertyName!)!;
							prop.SetValue (scope, JsonSerializer.Deserialize (ref reader, prop.PropertyType, options));
						} else {
							reader.Skip ();
						}
					}
				}
				throw new JsonException ();
			}

			/// <inheritdoc/>
			public override void Write (Utf8JsonWriter writer, Tscope root, JsonSerializerOptions options)
			{
				writer.WriteStartObject ();

				var properties = root!.GetType ().GetProperties ().Where (p => p.GetCustomAttribute (typeof (JsonIncludeAttribute)) != null);
				foreach (var p in properties) {
					writer.WritePropertyName (ConfigProperty.GetJsonPropertyName (p));
					JsonSerializer.Serialize (writer, root.GetType ().GetProperty (p.Name)?.GetValue (root), options);
				}

				var configStore = (Dictionary<string, ConfigProperty>)typeof (Tscope).GetProperty ("Properties")?.GetValue (root)!;
				foreach (var p in from p in configStore
						  .Where (cp =>
							cp.Value.PropertyInfo?.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is
							SerializableConfigurationProperty scp && scp?.Scope == typeof (Tscope))
						  where p.Value.PropertyValue != null
						  select p) {

					writer.WritePropertyName (p.Key);
					var propertyType = p.Value.PropertyInfo?.PropertyType;

					if (propertyType != null && p.Value.PropertyInfo?.GetCustomAttribute (typeof (JsonConverterAttribute)) is JsonConverterAttribute jca) {
						var converter = Activator.CreateInstance (jca.ConverterType!)!;
						if (converter.GetType ().BaseType == typeof (JsonConverterFactory)) {
							var factory = (JsonConverterFactory)converter;
							if (factory.CanConvert (propertyType)) {
								converter = factory.CreateConverter (propertyType, options)!;
							}
						}
						if (p.Value.PropertyValue != null) {
							converter.GetType ().GetMethod ("Write")?.Invoke (converter, new object [] { writer, p.Value.PropertyValue, options });
						}
					} else {
						JsonSerializer.Serialize (writer, p.Value.PropertyValue, options);
					}
				}

				writer.WriteEndObject ();
			}
		}

	}
}
