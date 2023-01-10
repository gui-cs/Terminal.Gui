using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.Resources;
using static System.Net.Mime.MediaTypeNames;

namespace Terminal.Gui.Core {

	/// <summary>
	/// Abstract class for implementing configuration settings within the Terminal.Gui configration schema. 
	/// </summary>
	public abstract class Config<T> where T : Config<T> 
	{
		/// <summary>
		/// Gets the default configuration from the implementation (e.g. from <see cref="ListView"/>); called to 
		/// initlize a <see cref="Config{T}"/> instance.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method is only really useful when using <see cref="ConfigurationManager.SaveDefaultConfig(string)"/>
		/// to generate the JSON doc that is embedded into Terminal.Gui (during development). 
		/// </para>
		/// <para>
		/// If this method is used before Terminal.Gui has been initizlied (either <see cref="Application"/> 
		/// or <see cref="ConsoleDriver.Init(Action)"/>) care must be taken to ensure settings that can't be 
		/// set/read don't throw exceptions. 
		/// </para>
		/// </remarks>
		internal abstract void GetHardCodedDefaults ();

		/// <summary>
		/// Applys the confirationg settings held by this <see cref="Config{T}"/> object to the running <see cref="Application"/>.
		/// </summary>
		/// <remarks>
		/// This method must only set a target setting if the configuration held here was actually set (because it was
		/// read from JSON).
		/// </remarks>
		public abstract void Apply ();

		/// <summary>
		/// Updates the internal state of a <see cref="Config{T}"/>  object. Called when JSON has been loaded. This method
		/// must set the internal state such that <see cref="Apply"/> can know whether a particularly configuration 
		/// setting was actually set.
		/// </summary>
		/// <param name="updates"></param>
		internal abstract void Update (T updates);
	}

	/// <summary>
	/// Defines the VisualStyles for Terminal.Gui. Used to serialize and deserialize 
	/// JSON.
	/// </summary>
	public class VisualStyles : Config<VisualStyles> {
		/// <summary>
		/// The <see cref="ColorScheme"/> definitions. 
		/// </summary>
		[JsonInclude]
		public IDictionary<string, ColorScheme> ColorSchemes { get; set; }

		/// <inheritdoc/>
		internal override void GetHardCodedDefaults ()
		{
			ColorSchemes = Colors.ColorSchemes;
		}

		/// <inheritdoc/>
		public override void Apply ()
		{
			// ColorSchemes
			foreach (var scheme in ColorSchemes) {
				Colors.ColorSchemes [scheme.Key] = scheme.Value;
			}
		}

		/// <inheritdoc/>
		internal override void Update (VisualStyles updates)
		{
			if (ColorSchemes != null) {
				foreach (var scheme in updates.ColorSchemes) {
					ColorSchemes [scheme.Key] = scheme.Value;
				}
			}
		}
	}

	/// <summary>
	/// Defines the Application settings for Terminal.Gui. Used to serialize and deserialize 
	/// JSON.
	/// </summary>
	public class Settings : Config<Settings> {
		/// <summary>
		/// The <see cref="Application.HeightAsBuffer"/> setting.
		/// </summary>
		[JsonInclude]
		public bool? HeightAsBuffer { get; set; }

		/// <summary>
		/// The <see cref="Application.AlternateForwardKey"/> setting.
		/// </summary>
		[JsonInclude]
		public Key? AlternateForwardKey { get; set; }

		/// <summary>
		/// The <see cref="Application.AlternateBackwardKey"/> setting.
		/// </summary>
		[JsonInclude]
		public Key? AlternateBackwardKey { get; set; }

		/// <summary>
		/// The <see cref="Application.QuitKey"/> setting.
		/// </summary>
		[JsonInclude]
		public Key? QuitKey { get; set; }

		/// <summary>
		/// The <see cref="Application.IsMouseDisabled"/> setting.
		/// </summary>
		[JsonInclude]
		public bool? IsMouseDisabled { get; set; }

		/// <summary>
		/// The <see cref="Application.UseSystemConsole"/> setting.
		/// </summary>
		[JsonInclude]
		public bool? UseSystemConsole { get; set; }

		/// <inheritdoc/>
		internal override void GetHardCodedDefaults ()
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
		internal override void Update (Settings updates)
		{
			if (updates.HeightAsBuffer.HasValue) HeightAsBuffer = updates.HeightAsBuffer.Value;
			if (updates.AlternateForwardKey.HasValue) AlternateForwardKey = updates.AlternateForwardKey.Value;
			if (updates.AlternateBackwardKey.HasValue) AlternateBackwardKey = updates.AlternateBackwardKey.Value;
			if (updates.QuitKey.HasValue) QuitKey = updates.QuitKey.Value;
			if (updates.IsMouseDisabled.HasValue) IsMouseDisabled = updates.IsMouseDisabled.Value;
			if (updates.UseSystemConsole.HasValue) UseSystemConsole = updates.UseSystemConsole.Value;
		}
	}

	/// <summary>
	/// The root object of Terminal.Gui configuration settings / JSON schema.
	/// </summary>
	public class Configuration {
		/// <summary>
		/// The settings for the <see cref="Application"/>.
		/// </summary>
		[JsonInclude]
		public Settings Settings { get; set; } = new Settings ();

		/// <summary>
		/// The <see cref="VisualStyles"/> for the <see cref="Application"/>.
		/// </summary>
		[JsonInclude]
		public VisualStyles VisualStyles { get; set; } = new VisualStyles ();
	}

	/// <summary>
	/// Provides settings and configuration management for Terminal.Gui applications. See the <see cref="LoadConfigurationFromAllSources"/>
	/// for how configuraiton settings are applied. 
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
			},

		};

		/// <summary>
		/// The <see cref="Configuration"/> that has  been loaded by the <see cref="ConfigurationManager"/>.
		/// Use <see cref="VisualStyles.Apply()"/>, <see cref="Settings.Apply()"/> etc to apply these to the running Terminal.Gui app.
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
		/// <param name="style"></param>
		/// <returns></returns>
		public static string ToJson (Configuration style)
		{
			return JsonSerializer.Serialize<Configuration> (style, serializerOptions);
		}

		/// <summary>
		/// Updates the <see cref="Configuration"/> with the settings in a JSON string.
		/// </summary>
		/// <param name="json"></param>
		public static void UpdateConfiguration (string json)
		{
			// Deserialize the JSON into a Configuration object
			var newConfig = JsonSerializer.Deserialize<Configuration> (json, serializerOptions);

			// TODO: apply only settings defined to _config
			Config.Settings.Update (newConfig.Settings);
			Config.VisualStyles.Update (newConfig.VisualStyles);
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
		/// to create the `Terminal.Gui.Resources.config.json` resource during development. See the 
		/// TestConfigurationManagerSaveDefaults unit test in `ConfigurationManagerTests.cs`.
		/// </summary>
		public static void SaveDefaultConfig (string path)
		{
			var config = new Configuration ();

			// Get the hard coded settings
			config.Settings.GetHardCodedDefaults ();
			config.VisualStyles.GetHardCodedDefaults ();

			// Serialize to a JSON string
			string json = ConfigurationManager.ToJson (config);

			// Write the JSON string to the file specified by filePath
			File.WriteAllText (path, json);
		}

		/// <summary>
		/// Loads all settings found in the various locations into the <see cref="Config"/> object. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Settings are defined in JSON format, according to the schema: 
		///	https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json
		/// </para>
		/// <para>
		/// Settings that will apply to all applications reside in files named <c>visualstyles.json</c>. Settings 
		/// that will apply to a specific Terminal.Gui application reside in files named <c>appname.config.json</c>,
		/// where <c>appname</c> is the assembly name of the application. 
		/// </para>
		/// Settings will be applied using the following precidence (higher precidence settings
		/// overwrite lower precidence settings):
		/// <para>
		///	1. App specific settings found in the users's home directory (~/.tui/appname.visualstyles.json). -- Highest precidence.
		/// </para>
		/// <para>
		///	2. App specific settings found in the current working directory (./.tui/appname.visualstyles.json).
		/// </para>
		/// <para>
		///	3. App settings in app resources (Resources/{visualStylesFilename}). 
		/// </para>
		/// <para>
		///	4. Global settings found in the the user's home direcotry (~/.tui/{visualStylesFilename}).
		/// </para>
		/// <para>
		///	5. Global settings found in the app startup's direcotry (./.tui/{visualStylesFilename}).
		/// </para>
		/// <para>
		///	6. Default settings defined in the Terminal.Gui assembly -- Lowest precidence.
		/// </para>
		/// </remarks>
		public static void LoadConfigurationFromAllSources ()
		{
			// Styles defined in code in Terminal.Gui assembly (hard-coded) -- Lowest precidence.
			// (We actually ignore them at runtime and depend on the embedded `config.json` resource).

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

			Debug.Assert(_config != null);

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
