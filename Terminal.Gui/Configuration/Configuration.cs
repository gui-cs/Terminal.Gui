using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.Configuration {

	/// <summary>
	/// Classes that read/write configuration file sections (<see cref="ThemeManager"/> are derived from this class. 
	/// </summary>
	public abstract class Config {

		/// <summary>
		/// Applys the settings held by this <see cref="Config{T}"/> object to the running <see cref="Application"/>. 
		/// </summary>
		/// <remarks>
		/// This method must only set a target setting if the configuration held here was actually set (because it was
		/// read from JSON).
		/// </remarks>
		public abstract void Apply ();

		/// <summary>
		/// System.Text.Json does not support copying a deserialized object to an existing instance.
		/// To work around this, ModelBase implements a 'deep, memberwise clone' method. 
		/// `Named CopyPropertiesFrom` to make it clear what it does. 
		/// TOOD: When System.Text.Json implements `PopulateObject` revisit
		/// https://github.com/dotnet/corefx/issues/37627
		/// </summary>
		/// <param name="source"></param>
		public virtual void CopyPropertiesFrom (Config source)
		{
			ConfigProperty.DeepMemberwiseCopy (source, this);
		}
	}

	/// <summary>
	/// The root object of Terminal.Gui configuration settings / JSON schema.
	/// </summary>
	/// <example><code>
	///  {
	///    "$schema" : "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
	///    "Application.UseSystemConsole" : true,
	///    "Theme" : "Default",
	///    "Themes": {
	///    },
	///  },
	/// </code></example>
	/// <remarks>
	/// The nested class <see cref="ConfigRootConverter"/> Does all the heavy lifting for serialization 
	/// of the <see cref="ConfigRoot"/> object. Uses reflection to determine
	/// how to serialize properties based on their type (and [JsonConverter] attributes). Stores/retrieves
	/// data to <see cref="ConfigurationManager.ConfigProperties"/>.
	/// </remarks>
	[JsonConverter (typeof (ConfigRootConverter))]
	public class ConfigRoot {
		class ConfigRootConverter : JsonConverter<ConfigRoot> {
			private readonly static JsonConverter<ConfigRoot> s_defaultConverter = (JsonConverter<ConfigRoot>)JsonSerializerOptions.Default.GetConverter (typeof (ConfigRoot));

			// See: https://stackoverflow.com/questions/60830084/how-to-pass-an-argument-by-reference-using-reflection
			internal abstract class ReadHelper {
				public abstract object Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
			}

			internal class ReadHelper<T> : ReadHelper {
				private readonly ReadDelegate _readDelegate;

				private delegate T ReadDelegate (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);

				public ReadHelper (object converter)
				{
					_readDelegate = Delegate.CreateDelegate (typeof (ReadDelegate), converter, "Read") as ReadDelegate;
				}

				public override object Read (ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
				    => _readDelegate.Invoke (ref reader, type, options);
			}

			public override ConfigRoot Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.StartObject) {
					throw new JsonException ();
				}

				var configRoot = new ConfigRoot ();
				while (reader.Read ()) {
					if (reader.TokenType == JsonTokenType.EndObject) {
						return configRoot;
					}
					if (reader.TokenType != JsonTokenType.PropertyName) {
						throw new JsonException ();
					}
					var propertyName = reader.GetString ();
					reader.Read ();

					if (ConfigurationManager.ConfigProperties.TryGetValue (propertyName, out var configProp)) {
						if (configProp.PropertyInfo.GetCustomAttribute (typeof (JsonConverterAttribute)) is JsonConverterAttribute jca) {
							var readHelperType = typeof (ReadHelper<>).MakeGenericType (configProp.PropertyInfo.PropertyType);
							var readHelper = Activator.CreateInstance (readHelperType, Activator.CreateInstance (jca.ConverterType)) as ReadHelper;
							ConfigurationManager.ConfigProperties [propertyName].PropertyValue = readHelper.Read (ref reader, configProp.PropertyInfo.PropertyType, options);
						} else {
							ConfigurationManager.ConfigProperties [propertyName].PropertyValue = JsonSerializer.Deserialize (ref reader, configProp.PropertyInfo.PropertyType, options);
						}
					} else {
						if (propertyName == "$schema") {
							configRoot.schema = reader.GetString ();
						} else {
							reader.Skip ();
						}
					}
				}
				throw new JsonException ();
			}

			public override void Write (Utf8JsonWriter writer, ConfigRoot root, JsonSerializerOptions options)
			{
				writer.WriteStartObject ();
				writer.WriteString ("$schema", root.schema);
				foreach (var p in ConfigurationManager.ConfigProperties
					.Where (cp =>
						cp.Value.PropertyInfo.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is
						SerializableConfigurationProperty scp && scp.Scope == SerializableConfigurationProperty.Scopes.Settings)) {
					if (p.Value.PropertyValue != null) {
						writer.WritePropertyName (p.Key);
						if (p.Value.PropertyInfo.GetCustomAttribute (typeof (JsonConverterAttribute)) is JsonConverterAttribute jca) {
							var converter = Activator.CreateInstance (jca.ConverterType);
							var method = jca.ConverterType.GetMethod ("Write");
							method.Invoke (converter, new object [] { writer, p.Value.PropertyValue, options });
						} else {
							JsonSerializer.Serialize (writer, p.Value.PropertyValue, options);
						}
					}
				}
				writer.WriteEndObject ();
			}
		}

		/// <summary>
		/// Points to our JSON schema.
		/// </summary>
		[JsonInclude, JsonPropertyName ("$schema")]
		public string schema = "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json";
	}
}
