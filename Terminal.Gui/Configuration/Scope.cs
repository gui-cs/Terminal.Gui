using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Terminal.Gui.Configuration.ConfigurationManager;


#nullable enable

namespace Terminal.Gui.Configuration {
	public static partial class ConfigurationManager {

		/// <summary>
		/// Defines a configuration settings scope. Classes that inherit from this abstract class can be used to define
		/// scopes for configuration settings. Each scope is a JSON object that contains a set of configuration settings.
		/// </summary>
		public class Scope<T> : Dictionary<string, ConfigProperty> { //, IScope<Scope<T>> {
			/// <summary>
			/// Crates a new instance.
			/// </summary>
			public Scope () : base (StringComparer.InvariantCultureIgnoreCase)
			{
				foreach (var p in GetScopeProperties ()) {
					Add (p.Key, new ConfigProperty () { PropertyInfo = p.Value.PropertyInfo, PropertyValue = null });
				}
			}

			private IEnumerable<KeyValuePair<string, ConfigProperty>> GetScopeProperties ()
			{
				return ConfigurationManager._allConfigProperties!.Where (cp =>
					(cp.Value.PropertyInfo?.GetCustomAttribute (typeof (SerializableConfigurationProperty))
					as SerializableConfigurationProperty)?.Scope == GetType ());
			}

			/// <summary>
			/// Updates this instance from the specified source scope.
			/// </summary>
			/// <param name="source"></param>
			/// <returns>The updated scope (this).</returns>
			public object? UpdateFrom (Scope<T> source)
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

			/// <summary>
			/// Retrieves the values of the properties of this scope from their corresponding static properties.
			/// </summary>
			public void RetrieveValues ()
			{
				foreach (var p in this.Where (cp => cp.Value.PropertyInfo != null)) {
					p.Value.RetrieveValue ();
				}
			}

			/// <summary>
			/// Applies the values of the properties of this scope to their corresponding static properties.
			/// </summary>
			/// <returns></returns>
			internal virtual bool Apply ()
			{
				bool set = false;
				foreach (var p in this.Where (t => t.Value != null && t.Value.PropertyValue != null)) {
					if (p.Value.Apply ()) {
						set = true;
					}
				}
				return set;
			}
		}

		/// <summary>
		/// Converts <see cref="Scope{T}"/> instances to/from JSON. Does all the heavy lifting of reading/writing
		/// config data to/from <see cref="ConfigurationManager"/> JSON documents.
		/// </summary>
		/// <typeparam name="scopeT"></typeparam>
		public class ScopeJsonConverter<scopeT> : JsonConverter<scopeT> where scopeT : Scope<scopeT> {
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
			public override scopeT Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.StartObject) {
					throw new JsonException ();
				}

				var scope = (scopeT)Activator.CreateInstance (typeof (scopeT))!;
				while (reader.Read ()) {
					if (reader.TokenType == JsonTokenType.EndObject) {
						return scope!;
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
							var readHelper = Activator.CreateInstance ((Type?)typeof (ReadHelper<>).MakeGenericType (typeof (scopeT), propertyType!)!, converter) as ReadHelper;
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
			public override void Write (Utf8JsonWriter writer, scopeT scope, JsonSerializerOptions options)
			{
				writer.WriteStartObject ();

				var properties = scope!.GetType ().GetProperties ().Where (p => p.GetCustomAttribute (typeof (JsonIncludeAttribute)) != null);
				foreach (var p in properties) {
					writer.WritePropertyName (ConfigProperty.GetJsonPropertyName (p));
					JsonSerializer.Serialize (writer, scope.GetType ().GetProperty (p.Name)?.GetValue (scope), options);
				}

				foreach (var p in from p in scope
						  .Where (cp =>
							cp.Value.PropertyInfo?.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is
							SerializableConfigurationProperty scp && scp?.Scope == typeof (scopeT))
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
