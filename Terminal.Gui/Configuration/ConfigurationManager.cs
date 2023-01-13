using System;
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
	///	1. App specific settings found in the users's home directory (~/.tui/appname.config.json). -- Highest precidence.
	/// </para>
	/// <para>
	///	2. App specific settings found in the directory the app was launched from (./.tui/appname.config.json).
	/// </para>
	/// <para>
	///	3. App settings in app resources (Resources/config.json). 
	/// </para>
	/// <para>
	///	4. Global settings found in the the user's home directory (~/.tui/config.json).
	/// </para>
	/// <para>
	///	5. Global settings found in the directory the app was launched from (./.tui/config.json).
	/// </para>
	/// <para>
	///	6. Default settings defined in the Terminal.Gui assembly -- Lowest precedence.
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
		/// Use <see cref="Config.Apply()"/>, <see cref="Config.Apply()"/> etc to apply these to the running Terminal.Gui app.
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

			Config.UpdateAll (newConfig);
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
		/// Loads all settings found in the various locations into the <see cref="Config"/> object. 
		/// </summary>
		public static void LoadConfigurationFromAllSources ()
		{
			// Styles defined in code in Terminal.Gui assembly (hard-coded) -- Lowest precidence.
			// (We actually ignore them at runtime and depend on the embedded <c>config.json</c> resource).

			// Load the default styles from the Terminal.Gui assembly 
			var resourceName = $"Terminal.Gui.Resources.{_configFilename}";
			using (Stream stream = typeof (ConfigurationManager).Assembly.GetManifestResourceStream (resourceName))
			using (StreamReader reader = new StreamReader (stream)) {
				string json = reader.ReadToEnd ();
				_config = LoadFromJson (json);
#if DEBUG
				Debug.WriteLine ($"ConfigurationManager: Read configuration from {resourceName}");
#endif
			}

			Debug.Assert (_config != null);

			// Global Styles in local directories (./.tui/{visualStylesFilename})
			string globalLocal = $"./.tui/{_configFilename}";
			if (File.Exists (globalLocal)) {
				// Load the local file
				UpdateConfigurationFromFile (globalLocal);
			}

			// Global Styles in user home dir (~/.tui/{visualStylesFilename})
			string globalHome = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}/.tui/{_configFilename}";
			if (File.Exists (globalHome)) {
				// Load the local file
				UpdateConfigurationFromFile (globalHome);
			}

			// App Styles in app exe resources (Resources/{visualStylesFilename})
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

			// Get app name
			string appName = System.Reflection.Assembly.GetEntryAssembly ().FullName.Split (',') [0].Trim ();

			// App Styles in the current working directory (./.tui/appname.visualstyles.json)
			string appLocal = $"./.tui/{appName}.{_configFilename}";
			if (File.Exists (appLocal)) {
				// Load the local file
				UpdateConfigurationFromFile (appLocal);
			}

			// App specific styles in the users's home directory (~/.tui/appname.visualstyles.json)
			string appHome = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}/.tui/{appName}.{_configFilename}";
			if (File.Exists (appHome)) {
				// Load the global file
				UpdateConfigurationFromFile (appHome);
			}
		}
	}
}
