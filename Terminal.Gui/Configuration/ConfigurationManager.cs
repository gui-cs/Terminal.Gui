using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Terminal.Gui.Configuration {

	/// <summary>
	/// Classes that read/write configuration file sections (<see cref="Settings"/> and <see cref="ColorSchemes"/> are derived from this class. 
	/// </summary>
	[JsonDerivedType (typeof (Settings))]
	[JsonDerivedType (typeof (Themes))]
	public class Config {
		/// <summary>
		/// Gets the hard coded default settings from the implementation (e.g. from <see cref="ListView"/>); called to 
		/// initlize a <see cref="Config"/> object instance.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method is only really useful when using <see cref="ConfigurationManager.SaveHardCodedDefaults(string)"/>
		/// to generate the JSON doc that is embedded into Terminal.Gui (during development). 
		/// </para>
		/// <para>
		/// If this method is used before Terminal.Gui has been initizlied (either <see cref="Application"/> 
		/// or <see cref="ConsoleDriver.Init(Action)"/>) care must be taken to ensure settings that can't be 
		/// set/read don't throw exceptions. 
		/// </para>
		/// </remarks>
		public virtual void GetHardCodedDefaults () { }

		/// <summary>
		/// Applys the confirationg settings held by this <see cref="Config"/> object to the running <see cref="Application"/>.
		/// </summary>
		/// <remarks>
		/// This method must only set a target setting if the configuration held here was actually set (because it was
		/// read from JSON).
		/// </remarks>
		public virtual void Apply () { }

		/// <summary>
		/// Updates the internal state of a <see cref="Config"/>  object. Called when JSON has been loaded. This method
		/// must set the internal state such that <see cref="Apply"/> can know whether a particularly configuration 
		/// setting was actually set.
		/// </summary>
		/// <param name="updates"></param>
		public virtual void Update (Configuration updates) { }
	}

	/// <summary>
	/// A Theme is a set of settings.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A Theme is a collection of settings that are named. 
	/// </para>
	/// </remarks>
	/// <example><code>
	/// 	"Default": {
	/// 		"ColorSchemes": [
	/// 		{
	/// 		"TopLevel": {
	/// 		"Normal": {
	/// 			"Foreground": "BrightGreen",
	/// 			"Background": "Black"
	/// 		},
	/// 		"Focus": {
	/// 		"Foreground": "White",
	/// 			"Background": "Cyan"
	/// 
	/// 		},
	/// 		"HotNormal": {
	/// 			"Foreground": "Brown",
	/// 			"Background": "Black"
	/// 
	/// 		},
	/// 		"HotFocus": {
	/// 			"Foreground": "Blue",
	/// 			"Background": "Cyan"
	/// 		},
	/// 		"Disabled": {
	/// 			"Foreground": "DarkGray",
	/// 			"Background": "Black"
	/// 
	/// 		}
	/// 	}
	/// </code></example> 
	public class Theme : Config {
		/// <summary>
		/// The ColorScheme for the Theme
		/// </summary>
		[JsonConverter (typeof (DictionaryConverter<ColorScheme>))]
		public Dictionary<string, ColorScheme> ColorSchemes { get; set; } = new Dictionary<string, ColorScheme> ();

		/// <inheritdoc/>
		public override void Apply ()
		{
			if (ColorSchemes != null) {
				// ColorSchemes
				foreach (var scheme in ColorSchemes) {
					Colors.ColorSchemes [scheme.Key] = scheme.Value;
				}
			}
		}

		/// <inheritdoc/>
		public override void GetHardCodedDefaults ()
		{
			throw new NotImplementedException ();
		}

		/// <inheritdoc/>
		public override void Update (Configuration updates)
		{
			throw new NotImplementedException ();
		}

		public void CopyFrom (Theme theme)
		{
			if (theme == null) {
				return;
			}
			foreach (var updatedScheme in theme.ColorSchemes) {
				ColorSchemes [updatedScheme.Key] = updatedScheme.Value;
			}
		}
	}

	/// <summary>
	/// Defines the Themes for a Terminal.Gui application.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A Theme is a collection of settings that are named. The default theme is named "Default".
	/// </para>
	/// <para>
	/// The <c>SelectedTheme</c> property is used to detemrine the currently active theme. 
	/// </para>
	/// </remarks>
	/// <example><code>
	/// "Themes": {
	/// 	"SelectedTheme" : "Default",
	/// 	"ThemeDefinitions": [
	/// 	{
	/// 		"Default": {
	/// 			"ColorSchemes": [
	/// 			{
	/// 			"TopLevel": {
	/// 			"Normal": {
	/// 				"Foreground": "BrightGreen",
	/// 				"Background": "Black"
	/// 			},
	/// 			"Focus": {
	/// 			"Foreground": "White",
	/// 				"Background": "Cyan"
	/// 
	/// 			},
	/// 			"HotNormal": {
	/// 				"Foreground": "Brown",
	/// 				"Background": "Black"
	/// 
	/// 			},
	/// 			"HotFocus": {
	/// 				"Foreground": "Blue",
	/// 				"Background": "Cyan"
	/// 			},
	/// 			"Disabled": {
	/// 				"Foreground": "DarkGray",
	/// 				"Background": "Black"
	/// 
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code></example> 
	public class Themes : Config {
		/// <summary>
		/// The currenlty selected theme. 
		/// </summary>
		[JsonInclude]
		public string SelectedTheme = string.Empty;

		/// <summary>
		/// The <see cref="Theme"/> definitions. 
		/// </summary>
		[JsonInclude]
		[JsonConverter (typeof (DictionaryConverter<Theme>))]
		public Dictionary<string, Theme> ThemeDefinitions { get; set; } = new Dictionary<string, Theme> ();

		/// <inheritdoc/>
		public override void GetHardCodedDefaults ()
		{
			SelectedTheme = "Default";
			var defaultTheme = new Theme () { };
			foreach (var scheme in Colors.ColorSchemes) {
				defaultTheme.ColorSchemes.Add (scheme.Key, scheme.Value);
			}
			ThemeDefinitions.Add (SelectedTheme, defaultTheme);
		}

		/// <inheritdoc/>
		public override void Apply ()
		{
			if (ThemeDefinitions != null && ThemeDefinitions.ContainsKey (SelectedTheme)) {
				ThemeDefinitions [SelectedTheme].Apply ();
			}
		}

		/// <inheritdoc/>
		public override void Update (Configuration updates)
		{
			if (ThemeDefinitions != null && updates.Themes != null) {
				foreach (var theme in updates.Themes.ThemeDefinitions) {
					if (ThemeDefinitions.ContainsKey (theme.Key)) {
						ThemeDefinitions [theme.Key].CopyFrom (theme.Value);
					} else {
						ThemeDefinitions.Add (theme.Key, theme.Value);
					}
				}
			}

			if (!string.IsNullOrEmpty (updates.Themes.SelectedTheme)) {
				SelectedTheme = updates.Themes.SelectedTheme;
			}
		}
	}

	/// <summary>
	/// Defines the Application settings for a Terminal.Gui application.
	/// </summary>
	/// <example><code>
	///  "Settings": {
	///    "QuitKey": {
	///      "Key": "Q",
	///      "Modifiers": [
	///        "Ctrl"
	///      ]
	///    },
	///    "AlternateForwardKey": {
	///      "Key": "PageDown",
	///      "Modifiers": [
	///         "Ctrl"
	///      ]
	///    },
	///    "AlternateBackwardKey": {
	///      "Key": "PageUp",
	///      "Modifiers": [
	///      "Ctrl"
	///      ]
	///    },
	///    "UseSystemConsole": false,
	///    "IsMouseDisabled": false,
	///    "HeightAsBuffer": false
	///  }
	/// </code></example>
	public class Settings : Config {
		/// <summary>
		/// The <see cref="Application.QuitKey"/> setting.
		/// </summary>
		public Key? QuitKey { get; set; }

		/// <summary>
		/// The <see cref="Application.AlternateForwardKey"/> setting.
		/// </summary>
		public Key? AlternateForwardKey { get; set; }

		/// <summary>
		/// The <see cref="Application.AlternateBackwardKey"/> setting.
		/// </summary>
		public Key? AlternateBackwardKey { get; set; }

		/// <summary>
		/// The <see cref="Application.UseSystemConsole"/> setting.
		/// </summary>
		public bool? UseSystemConsole { get; set; }

		/// <summary>
		/// The <see cref="Application.IsMouseDisabled"/> setting.
		/// </summary>
		public bool? IsMouseDisabled { get; set; }

		/// <summary>
		/// The <see cref="Application.HeightAsBuffer"/> setting.
		/// </summary>
		public bool? HeightAsBuffer { get; set; }

		/// <inheritdoc/>
		public override void GetHardCodedDefaults ()
		{
			if (Application.Driver != null) {
				HeightAsBuffer = Application.HeightAsBuffer;
			}
			AlternateForwardKey = Application.AlternateForwardKey;
			AlternateBackwardKey = Application.AlternateBackwardKey;
			QuitKey = Application.QuitKey;
			IsMouseDisabled = Application.IsMouseDisabled;
			UseSystemConsole = Application.UseSystemConsole;
		}

		/// <inheritdoc/>
		public override void Apply ()
		{

			if (Application.Driver != null && HeightAsBuffer.HasValue) Application.HeightAsBuffer = HeightAsBuffer.Value;
			if (AlternateForwardKey.HasValue) Application.AlternateForwardKey = AlternateForwardKey.Value;
			if (AlternateBackwardKey.HasValue) Application.AlternateBackwardKey = AlternateBackwardKey.Value;
			if (QuitKey.HasValue) Application.QuitKey = QuitKey.Value;
			if (IsMouseDisabled.HasValue) Application.IsMouseDisabled = IsMouseDisabled.Value;
			if (UseSystemConsole.HasValue) Application.UseSystemConsole = UseSystemConsole.Value;
		}

		/// <inheritdoc/>
		public override void Update (Configuration updates)
		{
			var updatedSettings = updates.Settings;
			if (updatedSettings.HeightAsBuffer.HasValue) HeightAsBuffer = updatedSettings.HeightAsBuffer.Value;
			if (updatedSettings.AlternateForwardKey.HasValue) AlternateForwardKey = updatedSettings.AlternateForwardKey.Value;
			if (updatedSettings.AlternateBackwardKey.HasValue) AlternateBackwardKey = updatedSettings.AlternateBackwardKey.Value;
			if (updatedSettings.QuitKey.HasValue) QuitKey = updatedSettings.QuitKey.Value;
			if (updatedSettings.IsMouseDisabled.HasValue) IsMouseDisabled = updatedSettings.IsMouseDisabled.Value;
			if (updatedSettings.UseSystemConsole.HasValue) UseSystemConsole = updatedSettings.UseSystemConsole.Value;
		}
	}

	/// <summary>
	/// The root object of Terminal.Gui configuration settings / JSON schema.
	/// </summary>
	/// <example><code>
	///  {
	///    "$schema" : "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
	///    "Settings": {
	///    },
	///    "Themes": {
	///    },
	///  },
	/// </code></example>
	public class Configuration {
		/// <summary>
		/// Points to our JSON schema.
		/// </summary>
		[JsonInclude, JsonPropertyName ("$schema")]
		public string schema = "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json";

		/// <summary>
		/// The Settings.
		/// </summary>
		[JsonInclude]
		public Settings Settings = new Settings ();

		/// <summary>
		/// The ColorSchemes.
		/// </summary>
		[JsonInclude]
		public Themes Themes = new Themes ();

		/// <summary>
		/// Applies the settings in each <see cref="Config"/> object to the running <see cref="Application"/>.
		/// </summary>
		public void ApplyAll ()
		{
			Settings.Apply ();
			Themes.Apply ();
		}

		/// <summary>
		/// Updates the internal state of <see cref="Configuration"/> from a newly read
		/// instance.
		/// </summary>
		/// <param name="newConfig"></param>
		/// <exception cref="NotImplementedException"></exception>
		internal void UpdateAll (Configuration newConfig)
		{
			Settings.Update (newConfig);
			Themes.Update (newConfig);
		}

		/// <summary>
		/// Retrives all hard coded default settings; used to generate the default config.json file
		/// during development. 
		/// </summary>
		internal void GetAllHardCodedDefaults ()
		{
			Settings.GetHardCodedDefaults ();
			Themes.GetHardCodedDefaults ();
		}
	}


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
