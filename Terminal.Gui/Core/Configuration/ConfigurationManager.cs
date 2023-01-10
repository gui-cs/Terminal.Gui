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
	/// Defines the VisualStyles for Terminal.Gui. 
	/// </summary>
	public class VisualStyles {
		/// <summary>
		/// Creates a new instance, initilziing the styles from the
		/// Terminal.Gui source code.
		/// </summary>
		public VisualStyles ()
		{
		}


		/// <summary>
		/// The <see cref="ColorScheme"/> definitions. 
		/// </summary>
		[JsonInclude]
		public IDictionary<string, ColorScheme> ColorSchemes { get; set; }

		/// <summary>
		/// Gets the default styles set in the <see cref="Application"/>, etc... classes.
		/// </summary>
		internal void GetHardCodedDefaults ()
		{
			ColorSchemes = Colors.ColorSchemes;
		}

		/// <summary>
		/// Applys the loaded <see cref="VisualStyles"/> to <see cref="Application"/>.
		/// </summary>
		public void Apply ()
		{
			// ColorSchemes
			foreach (var scheme in ColorSchemes) {
				Colors.ColorSchemes [scheme.Key] = scheme.Value;
			}
		}

		internal void Update (VisualStyles updates)
		{
			if (updates.ColorSchemes != null) {
				foreach (var scheme in updates.ColorSchemes) {
					ColorSchemes [scheme.Key] = scheme.Value;
				}
			}
		}
	}

	/// <summary>
	/// Defines the Application settings for Terminal.Gui.
	/// </summary>
	public class ApplicationSettings {
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ApplicationSettings ()
		{

		}

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

		/// <summary>
		/// Gets the defaults set in the <see cref="Application"/> class.
		/// </summary>
		public void GetHardCodedDefaults ()
		{
			//HeightAsBuffer = Application.HeightAsBuffer;
			AlternateForwardKey = Application.AlternateForwardKey;
			AlternateBackwardKey = Application.AlternateBackwardKey;
			QuitKey = Application.QuitKey;
			IsMouseDisabled = Application.IsMouseDisabled;
			UseSystemConsole = Application.UseSystemConsole;
		}

		/// <summary>
		/// Applies the settings to <see cref="Application"/>
		/// </summary>
		public void Apply ()
		{
			if (Application.Driver != null && HeightAsBuffer.HasValue) Application.HeightAsBuffer = HeightAsBuffer.Value;
			if (AlternateForwardKey.HasValue) Application.AlternateForwardKey = AlternateForwardKey.Value;
			if (AlternateBackwardKey.HasValue) Application.AlternateBackwardKey = AlternateBackwardKey.Value;
			if (QuitKey.HasValue) Application.QuitKey = QuitKey.Value;
			if (IsMouseDisabled.HasValue) Application.IsMouseDisabled = IsMouseDisabled.Value;
			if (UseSystemConsole.HasValue) Application.UseSystemConsole = UseSystemConsole.Value;
		}

		internal void Update (ApplicationSettings updates)
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
	/// The root object of Terminal.Gui configuration settings. 
	/// </summary>
	public class Configuration {
		/// <summary>
		/// The settings for the <see cref="Application"/>.
		/// </summary>
		[JsonInclude]
		public ApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings ();

		/// <summary>
		/// The <see cref="VisualStyles"/> for the <see cref="Application"/>.
		/// </summary>
		[JsonInclude]
		public VisualStyles VisualStyles { get; set; } = new VisualStyles ();
	}

	/// <summary>
	/// 
	/// </summary>
	public static class ConfigurationManager {

		private static readonly string _configFilename = "config.json";
		private static Configuration _config = new Configuration ();

		private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
			ReadCommentHandling = JsonCommentHandling.Skip,
			PropertyNameCaseInsensitive = true,
			IgnoreNullValues = true,
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
		/// Use <see cref="VisualStyles.Apply()"/>, <see cref="ApplicationSettings.Apply()"/> etc to apply these to the running Terminal.Gui app.
		/// </summary>
		public static Configuration Config { get { return _config; } }

		/// <summary>
		/// Loads the default settings from the Terminal.Gui assembly.
		/// <remarks>
		/// The settings are read from a JSON file stored in the Terminal.Gui assembly (in Terminal.Gui.Resouces).
		/// </remarks>
		/// </summary>
		public static void LoadDefaults ()
		{
			// Load the default styles from the Terminal.Gui assembly
			using (Stream stream = typeof (ConfigurationManager).Assembly.GetManifestResourceStream ($"Terminal.Gui.Resources.{_configFilename}"))
			using (StreamReader reader = new StreamReader (stream)) {
				string json = reader.ReadToEnd ();
				_config = LoadFromJson (json);
			}
		}

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
			Config.ApplicationSettings.Update (newConfig.ApplicationSettings);
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
		}

		/// <summary>
		/// Writes the configuration settings hard-coded into the libary to a JSON file. Used
		/// to create the JSON documents during development. 
		/// </summary>
		public static void SaveHardCodedConfig (string path)
		{
			var config = new Configuration ();

			// Serialize the Colors object to a JSON string
			string json = ConfigurationManager.ToJson (config);

			// Write the JSON string to the file specified by filePath
			File.WriteAllText (path, json);
		}

		/// <summary>
		/// Loads all settings found into the <see cref="Configuration"/>. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Styles are defined in JSON format, according to the schema: 
		/// </para>
		/// <para>
		/// Settings that will apply to all applications reside in files named <c>visualstyles.json</c>. Settings 
		/// that will apply to a specific Terminal.Gui application reside in files named <c>appname.visualstyles.json</c>,
		/// where <c>appname</c> is the assembly name of the application. 
		/// </para>
		/// Styles will be appied using the following precidence (higher precidence settings
		/// overwrite lower precidence settings):
		/// <para>
		/// 1. App specific styles found in the users's home directory (~/.tui/appname.visualstyles.json).
		/// </para>
		/// <para>
		/// 2. App specific styles found in the app startup directory (./.tui/appname.visualstyles.json).
		/// </para>
		/// <para>
		/// 3. App settings in app resources (Resources/{visualStylesFilename}). 
		/// </para>
		/// <para>
		/// 4. Global styles found in the the user's home direcotry (~/.tui/{visualStylesFilename}).
		/// </para>
		/// <para>
		/// 5. Global styles found in the app startup's direcotry (./.tui/{visualStylesFilename}).
		/// </para>
		/// <para>
		/// 6. Default styles defined in the Terminal.Gui assembly --lowest priority.
		/// </para>
		/// <para>
		/// The style manager first uses the settings from any copy of the visualstyles.json in the .tui/ directory in the user's home directory. 
		/// Then it looks for any copies of the file located in the app directories, 
		/// adding any settings found in them, but ignoring attributes already discovered in higher-priority locations. 
		/// As a last resort, for any settings not explicitly assigned at either the global or app level, 
		/// it assigns default values from the settings compiled into Terminal.Gui.dll.
		/// </para>
		/// </remarks>
		public static void LoadConfigurationFromAllSources ()
		{
			// Styles in Terminal.Gui assembly (hard-coded) --lowest priority
			Debug.Assert (Config != null);

			// Global Styles in local directories (./.tui/{visualStylesFilename})
			string globalLocal = $"./.tui/{_configFilename}";
			if (File.Exists (globalLocal)) {
				// Load the local file
				UpdateConfigurationFromFile (globalLocal);
			}

			// Global Styles in user home dir (~/.tui/{visualStylesFilename})
			string globalHome = $"~/.tui/{_configFilename}";
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
				}
			}

			// Get app name
			string appName = System.Reflection.Assembly.GetEntryAssembly ().FullName.Split (',') [0].Trim ();

			// App Styles in local directories (./.tui/appname.visualstyles.json)
			string appLocal = $"./.tui/{appName}.{_configFilename}";
			if (File.Exists (appLocal)) {
				// Load the local file
				UpdateConfigurationFromFile (appLocal);
			}

			// App Styles in the users's home directory (~/.tui/appname.visualstyles.json)
			string appHome = $"~/.tui/{appName}.{_configFilename}";
			if (File.Exists (appHome)) {
				// Load the global file
				UpdateConfigurationFromFile (appHome);
			}
		}
	}
}
