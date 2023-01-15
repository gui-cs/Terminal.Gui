using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		private static Configuration _config = new Configuration ();

		private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
			ReadCommentHandling = JsonCommentHandling.Skip,
			PropertyNameCaseInsensitive = true,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = true,
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter (),
				new ColorSchemeJsonConverter (),
				new KeyJsonConverter (),
				//new DictionaryAsArrayConverter <string, ColorScheme> (),
				//new DictionaryAsArrayConverter <string, Theme> ()
			},

		};

		/// <summary>
		/// The <see cref="Configuration"/> that has  been loaded by the <see cref="ConfigurationManager"/>.
		/// Use <see cref="Config{T}.Apply()"/> to apply these to the running Terminal.Gui app.
		/// </summary>
		public static Configuration Config { get { return _config; } }

		/// <summary>
		/// Loads the <see cref="Configuration"/> from a JSON document. 
		/// </summary>
		/// <param name="json"></param>
		/// <returns>A <see cref="Configuration"/> object initialized by the contents of the JSON document.</returns>
		public static Configuration LoadFromJson (string json)
		{
			return JsonSerializer.Deserialize<Configuration> (json, serializerOptions);
		}

		/// <summary>
		/// Creates a JSON document with the configuration specified. 
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public static string ToJson (Configuration config)
		{
			return JsonSerializer.Serialize<Configuration> (config, serializerOptions);
		}

		/// <summary>
		/// Updates the <see cref="Configuration"/> with the settings in a JSON string.
		/// </summary>
		/// <param name="json"></param>
		public static void UpdateConfiguration (string json)
		{
			// Deserialize the JSON into a Configuration object
			var newConfig = JsonSerializer.Deserialize<Configuration> (json, serializerOptions);

			Config.CopyUpdatedProperitesFrom (newConfig);
		}

		/// <summary>
		/// Updates the <see cref="Configuration"/> with the settings in a JSON file.
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
			var config = new Configuration ();

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
				_config = LoadFromJson (json);
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
