using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using static Terminal.Gui.Configuration.ConfigurationManager;


#nullable enable

namespace Terminal.Gui.Configuration {
	/// <summary>
	/// Provides settings and configuration management for Terminal.Gui applications. 
	/// <para>
	/// Users can set Terminal.Gui settings on a global or per-application basis by providing JSON formatted configuration files.
	/// The configuration files can be placed in at <c>.tui</c> folder in the user's home directory (e.g. <c>C:/Users/username/.tui</c>, 
	/// or <c>/usr/username/.tui</c>),
	/// the folder where the Terminal.Gui application was launched from (e.g. <c>./.tui</c>), or as a resource
	/// within the Terminal.Gui application's main assembly. 
	/// </para>
	/// <para>
	/// Settings are defined in JSON format, according to this schema: 
	///	https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json
	/// </para>
	/// <para>
	/// Settings that will apply to all applications (global settings) reside in files named <c>config.json</c>. Settings 
	/// that will apply to a specific Terminal.Gui application reside in files named <c>appname.config.json</c>,
	/// where <c>appname</c> is the assembly name of the application (e.g. <c>UICatalog.config.json</c>).
	/// </para>
	/// Settings are applied using the following precedence (higher precedence settings
	/// overwrite lower precedence settings):
	/// <para>
	///	1. Application configuration found in the users's home directory (<c>~/.tui/appname.config.json</c>) -- Highest precedence 
	/// </para>
	/// <para>
	///	2. Application configuration found in the directory the app was launched from (<c>./.tui/appname.config.json</c>).
	/// </para>
	/// <para>
	///	3. Application configuration found in the applications's resources (<c>Resources/config.json</c>). 
	/// </para>
	/// <para>
	///	4. Global configuration found in the the user's home directory (<c>~/.tui/config.json</c>).
	/// </para>
	/// <para>
	///	5. Global configuration found in the directory the app was launched from (<c>./.tui/config.json</c>).
	/// </para>
	/// <para>
	///     6. Global configuration in <c>Terminal.Gui.dll</c>'s resources (<c>Terminal.Gui.Resources.config.json</c>) -- Lowest Precidence.
	/// </para>
	/// </summary>
	public static class ConfigurationManager {

		private static readonly string _configFilename = "config.json";
		private static SettingsScope? _settingsScope = new SettingsScope ();

		/// <summary>
		/// Gets the <see cref="SettingsScope"/> settings that have been read from the configuration files.
		/// </summary>
		internal static Dictionary<string, ConfigProperty>? Settings { get => _settingsScope!.Properties; } 

		private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
			ReadCommentHandling = JsonCommentHandling.Skip,
			PropertyNameCaseInsensitive = true,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = true,
			Converters = {
				// No need to set converterss - the ConfigRootConverter uses property attributes apply the correct
				// Converter.
			},

		};

		/// <summary>
		/// Defines a configuration settings scope. Classes that inherit from this abstract class can be used to define
		/// scopes for configuration settings. Each scope is a JSON object that contains a set of configuration settings.
		/// </summary>
		public abstract class Scope {
			/// <summary>
			/// Crates a new instance.
			/// </summary>
			public Scope ()
			{
				ConfigurationManager._allConfigProperties ??= getConfigProperties ();
				var props = ConfigurationManager._allConfigProperties.Where (cp =>
					(cp.Value.PropertyInfo?.GetCustomAttribute (typeof (SerializableConfigurationProperty))
					as SerializableConfigurationProperty)?.Scope == this.GetType ());
				Properties = props.ToDictionary (dict => dict.Key,
					dict => new ConfigProperty () { PropertyInfo = dict.Value.PropertyInfo, PropertyValue = null }, StringComparer.InvariantCultureIgnoreCase);
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
		}

		/// <summary>
		/// An attribute that can be applied to a property to indicate that it should included in the configuration file.
		/// </summary>
		/// <example>
		/// 	[SerializableConfigurationProperty(Scope = typeof(Configuration.ThemeManager.ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
		///	public static BorderStyle DefaultBorderStyle {
		///	...
		/// </example>
		[AttributeUsage (AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
		public class SerializableConfigurationProperty : System.Attribute {
			/// <summary>
			/// Specifies the scope of the property. 
			/// </summary>
			public Type Scope { get; set; } = typeof (Scope);

			/// <summary>
			/// If <see langword="true"/>, the property will be serialized to the configuration file using only the property name
			/// as the key. If <see langword="false"/>, the property will be serialized to the configuration file using the
			/// property name pre-pended with the classname (e.g. <c>Application.UseSystemConsole</c>).
			/// </summary>
			public bool OmitClassName { get; set; }
		}

		/// <summary>
		/// Holds a property's value and the <see cref="PropertyInfo"/> that allows <see cref="ConfigurationManager"/> to get and set the property's value.
		/// </summary>
		/// <remarks>
		/// Configuration properties must be <see langword="public"/> and <see langword="static"/> and have the <see cref="SerializableConfigurationProperty"/>
		/// attribute. If the type of the property requires specialized JSON serialization, a <see cref="JsonConverter"/> must be provided using 
		/// the <see cref="JsonConverterAttribute"/> attribute.
		/// </remarks>
		public class ConfigProperty {
			private object? propertyValue;

			/// <summary>
			/// Describes the property.
			/// </summary>
			public PropertyInfo? PropertyInfo { get; set; }

			/// <summary>
			/// Holds the property's value as it was either read from the class's implementation or from a config file. 
			/// If the property has not been set (e.g. because no configuration file specified a value), 
			/// this will be <see langword="null"/>.
			/// </summary>
			/// <remarks>
			/// On <see langword="set"/>, performs a sparse-copy of the new value to the existing value (only copies elements of 
			/// the object that are non-null).
			/// </remarks>
			public object? PropertyValue {
				get => propertyValue;
				set {
					if (PropertyValue == null) {
						propertyValue = value;
					} else {
						propertyValue = DeepMemberwiseCopy (value, PropertyValue);
					}
				}
			}
		}

		/// <summary>
		/// Converts <see cref="Scope"/> instances to/from JSON. Does all the heavy lifting of reading/writing
		/// config data to/from <see cref="ConfigurationManager"/> JSON documents.
		/// </summary>
		/// <typeparam name="rootT"></typeparam>
		public class ConfigScopeConverter<rootT> : JsonConverter<rootT> {
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
			public override rootT Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if (reader.TokenType != JsonTokenType.StartObject) {
					throw new JsonException ();
				}

				var root = (rootT)Activator.CreateInstance (typeof (rootT))!;
				// Get ConfigProperty store for this Scope type
				var scopeProperties = typeToConvert!.GetProperty ("ScopeProperties")?.GetValue (root) as Dictionary<string, ConfigProperty>;
				while (reader.Read ()) {
					if (reader.TokenType == JsonTokenType.EndObject) {
						return root;
					}
					if (reader.TokenType != JsonTokenType.PropertyName) {
						throw new JsonException ();
					}
					var propertyName = reader.GetString ();
					reader.Read ();

					if (propertyName != null && scopeProperties != null && scopeProperties.TryGetValue (propertyName, out var configProp)) {
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
							var readHelper = Activator.CreateInstance ((Type?)typeof (ReadHelper<>).MakeGenericType (typeof (rootT), propertyType!)!, converter) as ReadHelper;
							if (scopeProperties [propertyName].PropertyValue == null) {
								scopeProperties [propertyName].PropertyValue = readHelper?.Read (ref reader, propertyType!, options);
							} else {
								scopeProperties [propertyName].PropertyValue = DeepMemberwiseCopy (readHelper?.Read (ref reader, propertyType!, options), scopeProperties [propertyName].PropertyValue);

							}
						} else {
							if (scopeProperties [propertyName].PropertyValue == null) {
								scopeProperties [propertyName].PropertyValue = JsonSerializer.Deserialize (ref reader, propertyType!, options);
							} else {
								scopeProperties [propertyName].PropertyValue = DeepMemberwiseCopy (JsonSerializer.Deserialize (ref reader, propertyType!, options), scopeProperties [propertyName].PropertyValue);

							}
						}
					} else {
						if (root.GetType ().GetCustomAttribute (typeof (JsonIncludeAttribute)) != null) {
							if (root.GetType ().GetCustomAttribute (typeof (JsonPropertyNameAttribute)) != null) {
								propertyName = root.GetType ().GetCustomAttribute (typeof (JsonPropertyNameAttribute))?.ToString ();
							}
							var prop = root.GetType ().GetProperty (propertyName!)!;
							prop.SetValue (root, JsonSerializer.Deserialize (ref reader, prop.PropertyType, options));
						} else {
							reader.Skip ();
						}
					}
				}
				throw new JsonException ();
			}

			/// <inheritdoc/>
			public override void Write (Utf8JsonWriter writer, rootT root, JsonSerializerOptions options)
			{
				writer.WriteStartObject ();

				var properties = root!.GetType ().GetProperties ().Where (p => p.GetCustomAttribute (typeof (JsonIncludeAttribute)) != null);
				foreach (var p in properties) {
					var propertyName = p.Name;
					var attr = p.GetCustomAttribute (typeof (JsonPropertyNameAttribute)) as JsonPropertyNameAttribute;
					if (attr != null) {
						propertyName = attr.Name;
					}
					writer.WritePropertyName (propertyName);
					JsonSerializer.Serialize (writer, root.GetType ().GetProperty (p.Name)?.GetValue (root), options);

				}

				var configStore = (Dictionary<string, ConfigProperty>)typeof (rootT).GetProperty ("ScopeProperties")?.GetValue (root)!;
				foreach (var p in from p in configStore
						  .Where (cp =>
							cp.Value.PropertyInfo?.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is
							SerializableConfigurationProperty scp && scp?.Scope == typeof (rootT))
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

		/// <summary>
		/// A dictionary of all properties in the Terminal.Gui project that are decorated with the <see cref="SerializableConfigurationProperty"/> attribute.
		/// The keys are the property names pre-pended with the class that implements the property (e.g. <c>Application.UseSystemConsole</c>).
		/// The values are instances of <see cref="ConfigProperty"/> which hold the property's value and the
		/// <see cref="PropertyInfo"/> that allows <see cref="ConfigurationManager"/> to get and set the property's value.
		/// </summary>
		private static Dictionary<string, ConfigProperty> _allConfigProperties = getConfigProperties ();

		private static Dictionary<string, ConfigProperty> getConfigProperties ()
		{
			Dictionary<string, Type> classesWithConfigProps = new Dictionary<string, Type> (StringComparer.InvariantCultureIgnoreCase);
			foreach (Type classWithConfig in typeof (ConfigurationManager).Assembly.ExportedTypes
				.Where (myType => myType.IsClass &&
					myType.IsPublic &&
					(myType.GetProperties ()
					.Count (prop => prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) != null) > 0))) {
				classesWithConfigProps.Add (classWithConfig.Name, classWithConfig);
			}

			Dictionary<string, ConfigProperty> configProperties = new Dictionary<string, ConfigProperty> (StringComparer.InvariantCultureIgnoreCase);
			foreach (var p in from c in classesWithConfigProps
					  let props = c.Value.GetProperties ().Where (prop =>
						prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty)
					  let enumerable = props
					  from p in enumerable
					  select p) {
				if (p.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty scp) {
					if (p.GetGetMethod ()!.IsStatic) {
						configProperties.Add (scp.OmitClassName ? p.Name : $"{p.DeclaringType?.Name}.{p.Name}", new ConfigProperty {
							PropertyInfo = p,
							PropertyValue = p.GetValue (null)
						});
					} else {
						throw new Exception ($"Property {p.Name} in class {p.DeclaringType?.Name} is not static. All SerializableConfigurationProperty properties must be static.");
					}
				}
			}

			return configProperties.OrderBy (x => x.Key).ToDictionary (x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties 
		/// attributed with  <see cref="SettingsScope"/>.
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
		/// The nested class <see cref="ConfigScopeConverter{rootT}"/> Does all the heavy lifting for serialization 
		/// of the <see cref="SettingsScope"/> object. Uses reflection to determine
		/// how to serialize properties based on their type (and [JsonConverter] attributes). 
		/// </remarks>
		[JsonConverter (typeof (ConfigScopeConverter<SettingsScope>))]
		public class SettingsScope : Scope {
			/// <summary>
			/// Points to our JSON schema.
			/// </summary>
			[JsonInclude, JsonPropertyName ("$schema")]
			public string Schema { get; set; } = "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json";
		}

		/// <summary>
		/// Loads the <see cref="SettingsScope"/> from a JSON document, overwriting any existing settings.
		/// </summary>
		/// <param name="json"></param>
		/// <returns>A <see cref="SettingsScope"/> object initialized by the contents of the JSON document.</returns>
		internal static void LoadFromJson (string json)
		{
			_settingsScope = JsonSerializer.Deserialize<SettingsScope> (json, serializerOptions);
		}

		/// <summary>
		/// Creates a JSON document with the configuration specified. 
		/// </summary>
		/// <returns></returns>
		internal static string ToJson ()
		{
			return JsonSerializer.Serialize<SettingsScope> (_settingsScope!, serializerOptions);
		}

		/// <summary>
		/// Updates the <see cref="SettingsScope"/> with the settings in a JSON string.
		/// </summary>
		/// <param name="json"></param>
		internal static void UpdateConfiguration (string json)
		{
			// Deserialize the JSON into a Configuration object
			var settings = JsonSerializer.Deserialize<SettingsScope> (json, serializerOptions);
			_settingsScope = DeepMemberwiseCopy (settings, _settingsScope) as SettingsScope;
		}

		/// <summary>
		/// Updates the <see cref="SettingsScope"/> with the settings in a JSON file.
		/// </summary>
		/// <param name="filePath"></param>
		internal static void UpdateConfigurationFromFile (string filePath)
		{
			// Read the JSON file
			string json = File.ReadAllText (filePath);
			UpdateConfiguration (json);
#if DEBUG
			Debug.WriteLine ($"ConfigurationManager: Read configuration from {filePath}");
#endif
		}

		/// <summary>
		/// Retrieves the hard coded default settings from the Terminal.Gui library implementation. Used in development of
		/// the library to generate the default configuration file. Before calling Application.Init, make sure
		/// <see cref="Locations"/> is set to <see cref="ConfigLocations.None"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method is only really useful when using ConfigurationManagerTests
		/// to generate the JSON doc that is embedded into Terminal.Gui (during development). 
		/// </para>
		/// <para>
		/// WARNING: The <c>Terminal.Gui.Resources.config.json</c> resource has setting defintions (Themes)
		/// that are NOT generated by this function. If you use this function to regenerate <c>Terminal.Gui.Resources.config.json</c>,
		/// make sure you copy the Theme definitions from the existing <c>Terminal.Gui.Resources.config.json</c> file.
		/// </para>		
		/// </remarks>
		internal static void GetHardCodedDefaults ()
		{
			ThemeManager.GetHardCodedDefaults ();
			foreach (var p in _settingsScope!.Properties.Where (cp => cp.Value.PropertyInfo != null)) {
				_settingsScope.Properties[p.Key].PropertyValue = p.Value.PropertyInfo?.GetValue (null);
			}
		}

		/// <summary>
		/// Applies the settings in <see cref="SettingsScope"/> to the running <see cref="Application"/> instance.
		/// </summary>
		/// <remarks>
		/// This only applies <see cref="SettingsScope"/> properites. Use <see cref="ThemeManager.Apply"/> to apply the 
		/// current theme.</remarks>
		public static void Apply ()
		{
			// This just applies global Settings (SettingsScope).
			foreach (var p in _settingsScope!.Properties.Where (t => t.Value != null && t.Value.PropertyValue != null)) {
				p.Value?.PropertyInfo?.SetValue (null, DeepMemberwiseCopy (p.Value?.PropertyValue, p.Value?.PropertyInfo?.GetValue (null)));
			}
			OnApplied ();
		}

		/// <summary>
		/// Called when an updated configuration has been applied to the  
		/// application. Fires the <see cref="Applied"/> event.
		/// </summary>
		public static void OnApplied ()
		{
			Applied?.Invoke (new SettingScopeEventArgs ());
		}

		/// <summary>
		/// Event fired when an updated configuration has been applied to the  
		/// application.
		/// </summary>
		public static event Action<SettingScopeEventArgs>? Applied;

		/// <summary>
		/// Describes the location of the configuration files. The constancts can be
		/// combined (bitwise) to specify multiple locations.
		/// </summary>
		[Flags]
		public enum ConfigLocations {
			/// <summary>
			/// No configuration will be loaded. Used for development and testing.
			/// </summary>
			None = 0,

			/// <summary>
			/// Application configuration found in the users's home directory (<c>~/.tui/appname.config.json</c>) -- Highest precidence 
			/// </summary>
			AppHomeDirectory,

			/// <summary>
			/// Global configuration in the directory the app was launched from (<c>./.tui/config.json</c>).
			/// </summary>
			GlobalAppDirectory,

			/// <summary>
			/// Global configuration in the user's home directory (<c>~/.tui/config.json</c>).
			/// </summary>
			GlobalHomeDirectory,

			/// <summary>
			/// Application configuration in the app's resources (<c>appname.Resources.config.json</c>).
			/// </summary>
			AppResources,

			/// <summary>
			/// Application configuration in the directory the app was launched from.
			/// </summary>
			AppDirectory,

			/// <summary>
			/// Global configuration in <c>Terminal.Gui.dll</c>'s resources (<c>Terminal.Gui.Resources.config.json</c>) -- Lowest Precidence.
			/// </summary>
			LibraryResources,

			/// <summary>
			/// This constant is a combination of all locations
			/// </summary>
			All = AppHomeDirectory | GlobalAppDirectory | GlobalHomeDirectory | AppResources | AppDirectory | LibraryResources

		}

		/// <summary>
		/// Loads all settings found in <c>Terminal.Gui.Resources.config.json</c> into <see cref="ConfigurationManager"/>. 
		/// </summary>
		internal static void LoadGlobalFromLibraryResource ()
		{
			var resourceName = $"Terminal.Gui.Resources.{_configFilename}";
			using Stream? stream = typeof (ConfigurationManager).Assembly.GetManifestResourceStream (resourceName)!;
			using StreamReader reader = new StreamReader (stream);
			string json = reader.ReadToEnd ();
			if (json != null) {
				LoadFromJson (json);
			}
#if DEBUG
			Debug.WriteLine ($"ConfigurationManager: Read configuration from {resourceName}");
#endif
		}

		/// <summary>
		/// Loads global configuration from the directory the app was launched from (<c>./.tui/config.json</c>) into
		/// <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadGlobalFromAppDirectory ()
		{
			string globalLocal = $"./.tui/{_configFilename}";
			if (File.Exists (globalLocal)) {
				UpdateConfigurationFromFile (globalLocal);
			}
		}

		/// <summary>
		/// Loads global configuration in the user's home directory (<c>~/.tui/config.json</c>) into
		/// <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadGlobalFromHomeDirectory ()
		{
			string globalHome = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}/.tui/{_configFilename}";
			if (File.Exists (globalHome)) {
				UpdateConfigurationFromFile (globalHome);
			}
		}

		/// <summary>
		/// Loads application configuration in the app's resources (<c>appname.Resources.config.json</c>) into
		/// <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadAppFromAppResources ()
		{
			var embeddedStylesResourceName = Assembly.GetEntryAssembly ()?.GetManifestResourceNames ().FirstOrDefault (x => x.EndsWith (_configFilename));
			if (embeddedStylesResourceName != null) {
				using Stream? stream = Assembly.GetEntryAssembly ()?.GetManifestResourceStream (embeddedStylesResourceName);
				using StreamReader reader = new StreamReader (stream!);
				string json = reader.ReadToEnd ();
				UpdateConfiguration (json);
#if DEBUG
				Debug.WriteLine ($"ConfigurationManager: Read configuration from {embeddedStylesResourceName}");
#endif
			}
		}

		/// <summary>
		/// Name of the running application. By default this property is set to the application's assembly name.
		/// </summary>
		public static string AppName { get; set; } = Assembly.GetEntryAssembly ()?.FullName?.Split (',') [0]?.Trim ()!;

		/// <summary>
		/// Loads application configuration found in the directory the app was launched from (<c>./.tui/appname.config.json</c>)
		/// into <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadAppFromAppDirectory ()
		{
			string appLocal = $"./.tui/{AppName}.{_configFilename}";
			if (File.Exists (appLocal)) {
				UpdateConfigurationFromFile (appLocal);
			}
		}

		/// <summary>
		/// Loads application configuration found in the users's home directory (<c>~/.tui/appname.config.json</c>)
		/// into <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadAppFromHomeDirectory ()
		{
			string appHome = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}/.tui/{AppName}.{_configFilename}";
			if (File.Exists (appHome)) {
				UpdateConfigurationFromFile (appHome);
			}
		}

		/// <summary>
		/// Gets and sets the locations where <see cref="ConfigurationManager"/> will look for config files.
		/// The value is <see cref="ConfigLocations.All"/>.
		/// </summary>
		public static ConfigLocations Locations { get; set; } = ConfigLocations.All;

		/// <summary>
		/// Loads all settings found in the various locations to the <see cref="ConfigurationManager"/>. 
		/// </summary>
		/// <remarks>
		/// Use <see cref="Apply"/> to cause the loaded settings to be applied to the running application
		/// </remarks>
		public static void Load ()
		{
			if (Locations.HasFlag (ConfigLocations.LibraryResources)) LoadGlobalFromLibraryResource ();
			if (Locations.HasFlag (ConfigLocations.GlobalAppDirectory)) LoadGlobalFromAppDirectory ();
			if (Locations.HasFlag (ConfigLocations.GlobalHomeDirectory)) LoadGlobalFromHomeDirectory ();
			if (Locations.HasFlag (ConfigLocations.AppResources)) LoadAppFromAppResources ();
			if (Locations.HasFlag (ConfigLocations.AppDirectory)) LoadAppFromAppDirectory ();
			if (Locations.HasFlag (ConfigLocations.AppHomeDirectory)) LoadAppFromHomeDirectory ();
		}

		/// <summary>
		/// Returns an empty Json document with just the $schema tag.
		/// </summary>
		/// <returns></returns>
		public static string GetEmptyJson ()
		{
			var emptyScope = new SettingsScope ();
			emptyScope.Properties.Clear ();
			return JsonSerializer.Serialize<SettingsScope> (emptyScope, serializerOptions);
		}

		/// <summary>
		/// Event arguments for the <see cref="ConfigurationManager"/> events.
		/// </summary>
		public class SettingScopeEventArgs {

			/// <summary>
			/// Initializes a new instance of <see cref="SettingScopeEventArgs"/>
			/// </summary>
			public SettingScopeEventArgs ()
			{
			}
		}

		/// <summary>
		/// System.Text.Json does not support copying a deserialized object to an existing instance.
		/// To work around this, we implement a 'deep, memberwise copy' method. 
		/// </summary>
		/// <remarks>
		/// TOOD: When System.Text.Json implements `PopulateObject` revisit
		///	https://github.com/dotnet/corefx/issues/37627
		/// </remarks>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <returns><paramref name="destination"/> updated from <paramref name="source"/></returns>
		internal static object DeepMemberwiseCopy (object? source, object? destination)
		{
			if (destination == null) {
				throw new ArgumentNullException ();
			}

			if (source == null) {
				return null!;
			}

			if (source.GetType ().IsGenericType && source.GetType ().GetGenericTypeDefinition ().IsAssignableFrom (typeof (Dictionary<,>))) {
				foreach (var srcKey in ((IDictionary)source).Keys) {
					if (((IDictionary)destination).Contains (srcKey))
						((IDictionary)destination) [srcKey] = DeepMemberwiseCopy (((IDictionary)source) [srcKey], ((IDictionary)destination) [srcKey]);
					else {
						((IDictionary)destination).Add (srcKey, ((IDictionary)source) [srcKey]);
					}
				}
				return destination;
			}

			// If value type, just use copy constructor.
			if (source.GetType ().IsValueType || source.GetType () == typeof (string)) {
				return source;
			}

			var sourceProps = source?.GetType ().GetProperties ().Where (x => x.CanRead).ToList ();
			var destProps = destination?.GetType ().GetProperties ().Where (x => x.CanWrite).ToList ()!;
			foreach (var (sourceProp, destProp) in
				from sourceProp in sourceProps
				where destProps.Any (x => x.Name == sourceProp.Name)
				let destProp = destProps.First (x => x.Name == sourceProp.Name)
				where destProp.CanWrite
				select (sourceProp, destProp)) {

				var sourceVal = sourceProp.GetValue (source);
				var destVal = destProp.GetValue (destination);
				if (sourceVal != null) {
					if (destVal != null) {
						// Recurse
						destProp.SetValue (destination, DeepMemberwiseCopy (sourceVal, destVal));
					} else {
						destProp.SetValue (destination, sourceVal);
					}
				}
			}
			return destination!;
		}
	}
}
