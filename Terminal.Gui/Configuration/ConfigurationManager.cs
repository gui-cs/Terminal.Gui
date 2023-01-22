using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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
	///     and the default for <see cref="ConfigurationManager.Save()"/>.
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
		private static ConfigRoot _config = new ConfigRoot ();

		private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
			ReadCommentHandling = JsonCommentHandling.Skip,
			PropertyNameCaseInsensitive = true,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = true,
			Converters = {
				//new AttributeJsonConverter (),
				//new ColorJsonConverter (),
				//new ColorSchemeJsonConverter (),
				//new KeyJsonConverter (),
				//new DictionaryConverter<Theme> ()
			},

		};

		/// <summary>
		/// An attribute that can be applied to a property to indicate that it should included in the configuration file.
		/// </summary>
		[AttributeUsage (AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
		public class SerializableConfigurationProperty : System.Attribute {
			/// <summary>
			/// Defines the scopes that a property can be serialized to.
			/// </summary>
			public enum Scopes {
				/// <summary>
				/// The property belongs to the global settings scope and does not apply to a <see cref="Theme"/>.
				/// </summary>
				/// <remarks>
				/// Properties in the <see cref="Settings"/> scope are applied to all applications.
				/// They are serialized to the <c>config.json</c> file at the root of the JSON DOM (defined by <see cref="ConfigRoot"/>.
				/// </remarks>
				Settings,

				/// <summary>
				/// The property should be applied as part of a <see cref="Theme"/>.
				/// </summary>
				/// <remarks>
				/// Theme properties are serialized within the <c>{ "ThemeDefinitions" : [] }</c> property of the JSON DOM.
				/// </remarks>
				Theme
			};

			/// <summary>
			/// Specifies the scope of the property.
			/// </summary>
			public Scopes Scope { get; set; }

			/// <summary>
			/// If <see langword="true"/>, the property will be serialized to the configuration file using only the property name
			/// as the key. If <see langword="false"/>, the property will be serialized to the configuration file using the
			/// property name pre-pended with the classname (e.g. <c>Application.UseSystemConsole</c>).
			/// </summary>
			public bool OmitClassName { get; set; }
		}

#nullable enable

		/// <summary>
		/// Holds a property's value and the <see cref="PropertyInfo"/> that allows <see cref="ConfigurationManager"/> to get and set the property's value.
		/// </summary>
		/// <remarks>
		/// Configuration properties must be <see langword="public"/>. a <see langword="static"/>, and have the <see cref="SerializableConfigurationProperty"/>
		/// attribute. If the type of the property requires specialized JSON serialization, a custom <see cref="JsonConverter"/> must be provided and the 
		/// property must be decorated with the <see cref="JsonConverterAttribute"/> attribute.
		/// </remarks>
		public class ConfigProperty {
			/// <summary>
			/// Describes the property.
			/// </summary>
			public PropertyInfo? PropertyInfo { get; set; }

			/// <summary>
			/// Holds the property's value as it was either read from the class's implementation or from a config file. 
			/// If the proeprty has not been set (e.g. because no configuration file specified a value), this will be <see langword="null"/>.
			/// </summary>
			public object? PropertyValue { get; set; }

		}

		/// <summary>
		/// A dictionary of all properties in the Terminal.Gui project that are decorated with the <see cref="SerializableConfigurationProperty"/> attribute.
		/// The keys are the property names pre-pended with the class that implements the property (e.g. <c>Application.UseSystemConsole</c>).
		/// The values are instances of <see cref="ConfigProperty"/> which hold the property's value and the
		/// <see cref="PropertyInfo"/> that allows <see cref="ConfigurationManager"/> to get and set the property's value.
		/// </summary>
		public static Dictionary<string, ConfigProperty> ConfigProperties = getConfigProperties ();

		private static Dictionary<string, ConfigProperty> getConfigProperties ()
		{
			Dictionary<string, Type> classesWithConfigProps = new Dictionary<string, Type> ();
			foreach (Type classWithConfig in typeof (Theme).Assembly.ExportedTypes
				.Where (myType => myType.IsClass && myType.IsPublic && myType.GetProperties ()
					.Where (prop => prop.GetCustomAttributes (typeof (SerializableConfigurationProperty), false)
					.Count () > 0)
				.Count () > 0)) {
				classesWithConfigProps.Add (classWithConfig.Name, classWithConfig);
			}

			Dictionary<string, ConfigProperty> configProperties = new Dictionary<string, ConfigProperty> ();
			foreach (var p in from c in classesWithConfigProps
					  let props = c.Value.GetProperties ().Where (prop => {
						  return prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty;
					  })
					  let enumerable = props
					  from p in enumerable
					  select p) {
				if (p.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty scp) {
					if (p.GetGetMethod ().IsStatic) {
						configProperties.Add (scp.OmitClassName ? p.Name : $"{p.DeclaringType?.Name}.{p.Name}", new ConfigProperty {
							PropertyInfo = p,
							PropertyValue = p.GetValue (null)
						});
					} else {
						throw new Exception ($"Property {p.Name} in class {p.DeclaringType?.Name} is not static. All SerializableConfigurationProperty properties must be static.");
					}	

					var configProperty = new ConfigProperty () {
						PropertyInfo = p,
						PropertyValue = p.GetValue (null)
					};
					if (scp.OmitClassName) {
						configProperties [p.Name] = configProperty;
					} else {
						configProperties [p.DeclaringType?.Name + "." + p.Name] = configProperty;
					}
				}
			}

			return configProperties.OrderBy (x => x.Key).ToDictionary (x => x.Key, x => x.Value);
		}


		/// <summary>
		/// The <see cref="ConfigRoot"/> that has  been loaded by the <see cref="ConfigurationManager"/>.
		/// Use <see cref="Config{T}.Apply()"/> to apply these to the running Terminal.Gui app.
		/// </summary>
		public static ConfigRoot Config { get { return _config; } }

		/// <summary>
		/// Loads the <see cref="ConfigRoot"/> from a JSON document. 
		/// </summary>
		/// <param name="json"></param>
		/// <returns>A <see cref="ConfigRoot"/> object initialized by the contents of the JSON document.</returns>
		public static ConfigRoot LoadFromJson (string json)
		{
			return JsonSerializer.Deserialize<ConfigRoot> (json, serializerOptions);
		}

		/// <summary>
		/// Creates a JSON document with the configuration specified. 
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public static string ToJson (ConfigRoot config)
		{
			return JsonSerializer.Serialize<ConfigRoot> (config, serializerOptions);
		}

		/// <summary>
		/// Updates the <see cref="ConfigRoot"/> with the settings in a JSON string.
		/// </summary>
		/// <param name="json"></param>
		public static void UpdateConfiguration (string json)
		{
			// Deserialize the JSON into a Configuration object
			var newConfig = JsonSerializer.Deserialize<ConfigRoot> (json, serializerOptions);

			Config.CopyUpdatedProperitesFrom (newConfig);
		}

		/// <summary>
		/// Updates the <see cref="ConfigRoot"/> with the settings in a JSON file.
		/// </summary>
		/// <param name="filePath"></param>
		public static void UpdateConfigurationFromFile (string filePath)
		{
			// Read the JSON file
			string json = File.ReadAllText (filePath);
			UpdateConfiguration (json);
#if DEBUG
			Debug.WriteLine ($"ConfigurationManager: Read configuration from {filePath}");
#endif
		}

		/// <summary>
		/// Writes the configuration settings hard-coded into the libary to a JSON file. Used
		/// to create the <c>Terminal.Gui.Resources.config.json</c> resource during development. See the 
		/// TestConfigurationManagerSaveDefaults unit test in <c>ConfigurationManagerTests.cs</c>.
		/// </summary>
		/// <remarks>
		/// WARNING: The <c>Terminal.Gui.Resources.config.json</c> resource has setting defintions (Themes)
		/// that are NOT generated by this function. If you use this function to regenerate <c>Terminal.Gui.Resources.config.json</c>,
		/// make sure you copy the Theme definitions from the existing <c>Terminal.Gui.Resources.config.json</c> file.
		/// </remarks>
		public static void SaveHardCodedDefaults (string path)
		{
			var config = new ConfigRoot ();

			// Get the hard coded settings
			config.GetAllHardCodedDefaults ();

			// Serialize to a JSON string
			string json = ConfigurationManager.ToJson (config);

			// Write the JSON string to the file specified by filePath
			File.WriteAllText (path, json);
		}

		/// <summary>
		/// Describes the location of the configuration files. The constancts can be
		/// combined (bitwise) to specify multiple locations.
		/// </summary>
		[Flags]
		public enum ConfigLocation {
			/// <summary>
			/// Application configuration found in the users's home directory (<c>~/.tui/appname.config.json</c>) -- Highest precidence 
			/// and the default for <see cref="ConfigurationManager.Save()"/>.
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
		/// Loads all settings found in <c>Terminal.Gui.Resources.config.json</c> into <see cref="Config{T}"/>. 
		/// </summary>
		public static void LoadGlobalFromLibraryResource ()
		{
			var resourceName = $"Terminal.Gui.Resources.{_configFilename}";
			using (Stream stream = typeof (ConfigurationManager).Assembly.GetManifestResourceStream (resourceName))
			using (StreamReader reader = new StreamReader (stream)) {
				string json = reader.ReadToEnd ();
				if (json != null) {
					_config = LoadFromJson (json);
				}
#if DEBUG
				Debug.WriteLine ($"ConfigurationManager: Read configuration from {resourceName}");
#endif
			}
		}

		/// <summary>
		/// Loads global configuration from the directory the app was launched from (<c>./.tui/config.json</c>) into
		/// <see cref="Config"/>.
		/// </summary>
		public static void LoadGlobalFromAppDirectory ()
		{
			string globalLocal = $"./.tui/{_configFilename}";
			if (File.Exists (globalLocal)) {
				UpdateConfigurationFromFile (globalLocal);
			}
		}

		/// <summary>
		/// Loads global configuration in the user's home directory (<c>~/.tui/config.json</c>) into
		/// <see cref="Config"/>.
		/// </summary>
		public static void LoadGlobalFromHomeDirectory ()
		{
			string globalHome = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}/.tui/{_configFilename}";
			if (File.Exists (globalHome)) {
				UpdateConfigurationFromFile (globalHome);
			}
		}

		/// <summary>
		/// Loads application configuration in the app's resources (<c>appname.Resources.config.json</c>) into
		/// <see cref="Config"/>.
		/// </summary>
		public static void LoadAppFromAppResources ()
		{
			var embeddedStylesResourceName = System.Reflection.Assembly.GetEntryAssembly ().GetManifestResourceNames ().FirstOrDefault (x => x.EndsWith (_configFilename));
			if (embeddedStylesResourceName != null) {
				using (Stream stream = System.Reflection.Assembly.GetEntryAssembly ().GetManifestResourceStream (embeddedStylesResourceName))
				using (StreamReader reader = new StreamReader (stream)) {
					string json = reader.ReadToEnd ();
					UpdateConfiguration (json);
#if DEBUG
					Debug.WriteLine ($"ConfigurationManager: Read configuration from {embeddedStylesResourceName}");
#endif
				}
			}
		}

		/// <summary>
		/// Name of the running application. By default this property is set to the application's assembly name.
		/// </summary>
		public static string AppName { get; set; }
		private static string _appName = System.Reflection.Assembly.GetEntryAssembly ().FullName.Split (',') [0].Trim ();

		/// <summary>
		/// Loads application configuration found in the directory the app was launched from (<c>./.tui/appname.config.json</c>)
		/// into <see cref="Config"/>.
		/// </summary>
		public static void LoadAppFromAppDirectory ()
		{
			string appLocal = $"./.tui/{AppName}.{_configFilename}";
			if (File.Exists (appLocal)) {
				UpdateConfigurationFromFile (appLocal);
			}
		}

		/// <summary>
		/// Loads application configuration found in the users's home directory (<c>~/.tui/appname.config.json</c>)
		/// into <see cref="Config"/>.
		/// </summary>
		public static void LoadAppFromHomeDirectory ()
		{
			if (File.Exists (AppName)) {
				UpdateConfigurationFromFile (AppName);
			}
		}

		/// <summary>
		/// Loads all settings found in the various locations into the <see cref="Config{T}"/> object. 
		/// </summary>
		public static void Load (ConfigLocation locations = ConfigLocation.All)
		{
			if (locations.HasFlag (ConfigLocation.LibraryResources)) LoadGlobalFromLibraryResource ();
			if (locations.HasFlag (ConfigLocation.GlobalAppDirectory)) LoadGlobalFromAppDirectory ();
			if (locations.HasFlag (ConfigLocation.GlobalHomeDirectory)) LoadGlobalFromHomeDirectory ();
			if (locations.HasFlag (ConfigLocation.AppResources)) LoadAppFromAppResources ();
			if (locations.HasFlag (ConfigLocation.AppDirectory)) LoadAppFromAppDirectory ();
			if (locations.HasFlag (ConfigLocation.AppHomeDirectory)) LoadAppFromHomeDirectory ();
		}
	}
}
