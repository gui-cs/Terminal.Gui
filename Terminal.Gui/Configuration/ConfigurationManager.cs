using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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
	public static partial class ConfigurationManager {

		private static readonly string _configFilename = "config.json";

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
			/// Helper to get either the Json property named (specified by [JsonPropertyName(name)]
			/// or the actual property name.
			/// </summary>
			/// <param name="pi"></param>
			/// <returns></returns>
			public static string GetJsonPropertyName (PropertyInfo pi)
			{
				var jpna = pi.GetCustomAttribute (typeof (JsonPropertyNameAttribute)) as JsonPropertyNameAttribute;
				return jpna?.Name ?? pi.Name;
			}

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
					propertyValue = value;
				}
			}

			internal object? UpdateValueFrom (object source)
			{
				if (source == null) {
					return PropertyValue;
				}

				if (source.GetType () != PropertyInfo!.PropertyType) {
					throw new ArgumentException ($"The source object is not of type {PropertyInfo!.PropertyType}.");
				}
				if (PropertyValue != null && source != null) {
					PropertyValue = DeepMemberwiseCopy (source, PropertyValue);
				} else {
					PropertyValue = source;
				}

				return PropertyValue;
			}

			/// <summary>
			/// Retrieves (using reflection) the value of the static property described in <see cref="PropertyInfo"/>
			/// into <see cref="PropertyValue"/>.
			/// </summary>
			/// <returns></returns>
			public object? RetrieveValue ()
			{
				return PropertyValue = PropertyInfo!.GetValue (null);
			}

			/// <summary>
			/// Applies the <see cref="PropertyValue"/> to the property described by <see cref="PropertyInfo"/>.
			/// </summary>
			/// <returns></returns>
			public bool Apply ()
			{
				if (PropertyValue != null) {
					PropertyInfo?.SetValue (null, DeepMemberwiseCopy (PropertyValue, PropertyInfo?.GetValue (null)));
				}
				return PropertyValue != null;
			}
		}

		/// <summary>
		/// A dictionary of all properties in the Terminal.Gui project that are decorated with the <see cref="SerializableConfigurationProperty"/> attribute.
		/// The keys are the property names pre-pended with the class that implements the property (e.g. <c>Application.UseSystemConsole</c>).
		/// The values are instances of <see cref="ConfigProperty"/> which hold the property's value and the
		/// <see cref="PropertyInfo"/> that allows <see cref="ConfigurationManager"/> to get and set the property's value.
		/// </summary>
		/// <remarks>
		/// Is <see langword="null"/> until <see cref="Initialize"/> is called. 
		/// </remarks>
		private static Dictionary<string, ConfigProperty>? _allConfigProperties;

		/// <summary>
		/// The backing property for <see cref="Settings"/>. 
		/// </summary>
		/// <remarks>
		/// Is <see langword="null"/> until <see cref="Reset"/> is called. Gets set to a new instance by
		/// deserializtion (see <see cref="Load"/>).
		/// </remarks>
		private static SettingsScope? _settings;

		/// <summary>
		/// The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties with the <see cref="SettingsScope"/>
		/// attribute value.
		/// </summary>
		public static SettingsScope? Settings {
			get {
				if (_settings == null) {
					throw new InvalidOperationException ("ConfigurationManager has not been initialized. Call ConfigurationManager.Reset() before accessing the Settings property.");
				}
				return _settings;
			}
			set {
				_settings = value!;

				// Update the other scopes
				ThemeManager.Themes = (Dictionary<string, ThemeScope>)_settings.Properties ["Themes"]!.PropertyValue!;
			}
		}

		/// <summary>
		/// The root object of Terminal.Gui themes manager. Contains only properties with the <see cref="ThemeScope"/>
		/// attribute value.
		/// </summary>
		public static ThemeManager? Themes => ThemeManager.Instance;

		/// <summary>
		/// Aplication-specific configuration settings scope.
		/// </summary>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true), JsonPropertyName ("AppSettings")]
		public static AppScope? AppSettings { get; set; } 

		/// <summary>
		/// Initializes the internal state of ConfiguraitonManager. Nominally called once as part of application
		/// startup to initilaize global state. Also called from some Unit Tests to ensure correctness (e.g. Reset()).
		/// </summary>
		internal static void Initialize ()
		{
			_allConfigProperties = new Dictionary<string, ConfigProperty> ();
			_settings = null;

			Dictionary<string, Type> classesWithConfigProps = new Dictionary<string, Type> (StringComparer.InvariantCultureIgnoreCase);
			// Get Terminal.Gui.dll classes

			var types = from assembly in AppDomain.CurrentDomain.GetAssemblies ()
				    from type in assembly.GetTypes ()
				    where type.GetProperties ().Any (prop => prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) != null)
				    select type;

			foreach (var classWithConfig in types) {
				classesWithConfigProps.Add (classWithConfig.Name, classWithConfig);
			}

			Debug.WriteLine ($"ConfigManager.getConfigProperties found {classesWithConfigProps.Count} clases:");
			classesWithConfigProps.ToList ().ForEach (x => Debug.WriteLine ($"  Class: {x.Key}"));

			foreach (var p in from c in classesWithConfigProps
					  let props = c.Value.GetProperties (BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Where (prop =>
						prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty)
					  let enumerable = props
					  from p in enumerable
					  select p) {
				if (p.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty scp) {
					if (p.GetGetMethod (true)!.IsStatic) {
						// If the class name is ommited, JsonPropertyName is allowed. 
						_allConfigProperties!.Add (scp.OmitClassName ? ConfigProperty.GetJsonPropertyName (p) : $"{p.DeclaringType?.Name}.{p.Name}", new ConfigProperty {
							PropertyInfo = p,
							PropertyValue = null
						});
					} else {
						throw new Exception ($"Property {p.Name} in class {p.DeclaringType?.Name} is not static. All SerializableConfigurationProperty properties must be static.");
					}
				}
			}

			_allConfigProperties = _allConfigProperties!.OrderBy (x => x.Key).ToDictionary (x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);

			Debug.WriteLine ($"ConfigManager.Initialize found {_allConfigProperties.Count} properties:");
			_allConfigProperties.ToList ().ForEach (x => Debug.WriteLine ($"  Property: {x.Key}"));

			AppSettings = new AppScope ();
		}

		/// <summary>
		/// Creates a JSON document with the configuration specified. 
		/// </summary>
		/// <returns></returns>
		internal static string ToJson ()
		{
			Debug.WriteLine ($"ConfigurationManager.ToJson()");
			return JsonSerializer.Serialize<SettingsScope> (Settings!, serializerOptions);
		}

		/// <summary>
		/// Updates the <see cref="SettingsScope"/> with the settings in a JSON string.
		/// </summary>
		/// <param name="json"></param>
		internal static void Update (string json)
		{
			Debug.WriteLine ($"ConfigurationManager.UpdateConfiguration()");
			// Update the existing settings with the new settings.
			var settings = JsonSerializer.Deserialize<SettingsScope> (json, serializerOptions);
			Settings = DeepMemberwiseCopy (settings, Settings) as SettingsScope;
		}

		/// <summary>
		/// Updates the <see cref="SettingsScope"/> with the settings in a JSON file.
		/// </summary>
		/// <param name="filePath"></param>
		internal static void UpdateFromFile (string filePath)
		{
			// Read the JSON file
			string json = File.ReadAllText (filePath);
			Update (json);
			Debug.WriteLine ($"ConfigurationManager: Read configuration from {filePath}");
		}

		/// <summary>
		/// Resets the state of <see cref="ConfigurationManager"/>. Should be called whenever a new app session
		/// (e.g. in <see cref="Application.Init(ConsoleDriver, IMainLoopDriver)"/> starts. Called intenrally by <see cref="Load"/>
		/// if the <c>reset</c> parameter is <see langword="true"/>.
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		public static void Reset ()
		{
			if (_allConfigProperties == null) {
				ConfigurationManager.Initialize ();
			}

			Debug.WriteLine ($"ConfigurationManager.Reset()");
			Settings = new SettingsScope ();
			ThemeManager.Reset ();
			//AppSettings.Reset ();

			// To enable some unit tests, we only load from resources if the flag is set
			if (Locations.HasFlag (ConfigLocations.LibraryResources)) ResetFromLibraryResource ();

			Apply ();
			Themes?.Apply ();
			AppSettings?.Apply ();
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
			Settings = new SettingsScope ();
			ThemeManager.GetHardCodedDefaults ();
			AppSettings?.RetrieveValues ();
			foreach (var p in Settings!.Where (cp => cp.Value.PropertyInfo != null)) {
				Settings! [p.Key].PropertyValue = p.Value.PropertyInfo?.GetValue (null);
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
			if (Settings!.Apply ()) {
				OnApplied ();
			}
		}

		/// <summary>
		/// Called when an updated configuration has been applied to the  
		/// application. Fires the <see cref="Applied"/> event.
		/// </summary>
		public static void OnApplied ()
		{
			Debug.WriteLine ($"ConfigurationManager.OnApplied()");
			Applied?.Invoke (new SettingsScope.EventArgs ());
		}

		/// <summary>
		/// Event fired when an updated configuration has been applied to the  
		/// application.
		/// </summary>
		public static event Action<SettingsScope.EventArgs>? Applied;

		/// <summary>
		/// Loads all settings found in <c>Terminal.Gui.Resources.config.json</c> into <see cref="ConfigurationManager"/>. 
		/// </summary>
		internal static void ResetFromLibraryResource ()
		{
			var resourceName = $"Terminal.Gui.Resources.{_configFilename}";
			using Stream? stream = typeof (ConfigurationManager).Assembly.GetManifestResourceStream (resourceName)!;
			using StreamReader reader = new StreamReader (stream);
			string json = reader.ReadToEnd ();
			if (json != null) {
				Settings = JsonSerializer.Deserialize<SettingsScope> (json, serializerOptions);
			}
			Debug.WriteLine ($"ConfigurationManager: Read configuration from {resourceName}");
		}

		/// <summary>
		/// Loads global configuration from the directory the app was launched from (<c>./.tui/config.json</c>) into
		/// <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadGlobalAppDirectory ()
		{
			string globalLocal = $"./.tui/{_configFilename}";
			if (File.Exists (globalLocal)) {
				UpdateFromFile (globalLocal);
			}
		}

		/// <summary>
		/// Loads global configuration in the user's home directory (<c>~/.tui/config.json</c>) into
		/// <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadGlobalHomeDirectory ()
		{
			string globalHome = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}/.tui/{_configFilename}";
			if (File.Exists (globalHome)) {
				UpdateFromFile (globalHome);
			}
		}

		/// <summary>
		/// Loads application configuration in the app's resources (<c>appname.Resources.config.json</c>) into
		/// <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadAppResources ()
		{
			var embeddedStylesResourceName = Assembly.GetEntryAssembly ()?.GetManifestResourceNames ().FirstOrDefault (x => x.EndsWith (_configFilename));
			if (embeddedStylesResourceName != null) {
				using Stream? stream = Assembly.GetEntryAssembly ()?.GetManifestResourceStream (embeddedStylesResourceName);
				using StreamReader reader = new StreamReader (stream!);
				string json = reader.ReadToEnd ();
				Update (json);
				Debug.WriteLine ($"ConfigurationManager: Read configuration from {embeddedStylesResourceName}");
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
		internal static void LoadAppDirectory ()
		{
			string appLocal = $"./.tui/{AppName}.{_configFilename}";
			if (File.Exists (appLocal)) {
				UpdateFromFile (appLocal);
			}
		}

		/// <summary>
		/// Loads application configuration found in the users's home directory (<c>~/.tui/appname.config.json</c>)
		/// into <see cref="ConfigurationManager"/>.
		/// </summary>
		internal static void LoadAppHomeDirectory ()
		{
			string appHome = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}/.tui/{AppName}.{_configFilename}";
			if (File.Exists (appHome)) {
				UpdateFromFile (appHome);
			}
		}


		/// <summary>
		/// Describes the location of the configuration files. The constancts can be
		/// combined (bitwise) to specify multiple locations.
		/// </summary>
		[Flags]
		public enum ConfigLocations {
			/// <summary>
			/// No configuration will be loaded.
			/// </summary>
			/// <remarks>
			///  Used for development and testing only. For Terminal,Gui to function properly, at least
			///  <see cref="LibraryResources"/> should be set.
			/// </remarks>
			None = 0,

			/// <summary>
			/// Application configuration found in the user's home directory (<c>~/.tui/appname.config.json</c>) -- Highest precidence 
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
		/// Gets and sets the locations where <see cref="ConfigurationManager"/> will look for config files.
		/// The value is <see cref="ConfigLocations.All"/>.
		/// </summary>
		public static ConfigLocations Locations { get; set; } = ConfigLocations.All;

		/// <summary>
		/// Loads all settings found in the various configuraiton storage locations to 
		/// the <see cref="ConfigurationManager"/>. Optionally,
		/// resets all settings attributed with <see cref="SerializableConfigurationProperty"/> to the defaults 
		/// defined in <see cref="LoadAppResources"/>.
		/// </summary>
		/// <remarks>
		/// Use <see cref="Apply"/> to cause the loaded settings to be applied to the running application.
		/// </remarks>
		/// <param name="reset">If <see langword="true"/> the state of <see cref="ConfigurationManager"/> will
		/// be reset to the defaults defined in <see cref="LoadAppResources"/>.</param>
		public static void Load (bool reset = false)
		{
			Debug.WriteLine ($"ConfigurationManager.Load()");

			if (reset) Reset ();

			// LibraryResoruces is always loaded by Reset

			if (Locations.HasFlag (ConfigLocations.GlobalAppDirectory)) LoadGlobalAppDirectory ();
			if (Locations.HasFlag (ConfigLocations.GlobalHomeDirectory)) LoadGlobalHomeDirectory ();
			if (Locations.HasFlag (ConfigLocations.AppResources)) LoadAppResources ();
			if (Locations.HasFlag (ConfigLocations.AppDirectory)) LoadAppDirectory ();
			if (Locations.HasFlag (ConfigLocations.AppHomeDirectory)) LoadAppHomeDirectory ();
		}

		/// <summary>
		/// Returns an empty Json document with just the $schema tag.
		/// </summary>
		/// <returns></returns>
		public static string GetEmptyJson ()
		{
			var emptyScope = new SettingsScope ();
			emptyScope.Clear ();
			return JsonSerializer.Serialize<SettingsScope> (emptyScope, serializerOptions);
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
		internal static object? DeepMemberwiseCopy (object? source, object? destination)
		{
			if (destination == null) {
				throw new ArgumentNullException (nameof(destination));
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

			if (source.GetType ().BaseType == typeof (Scope)) {
				return ((Scope)destination).UpdateFrom ((Scope)source);

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
