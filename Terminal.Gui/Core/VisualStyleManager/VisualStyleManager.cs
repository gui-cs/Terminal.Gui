using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.Resources;

namespace Terminal.Gui.Core {

	/// <summary>
	/// 
	/// </summary>
	public class VisualStyle {
		public VisualStyle ()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// 
		public IDictionary<string, ColorScheme> ColorSchemes { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[JsonPropertyName ("view")]
		public ViewStyle View { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[JsonPropertyName ("listview")]
		public ViewStyle ListView { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public class ViewStyle {
		/// <summary>
		/// 
		/// </summary>
		[JsonPropertyName ("background")]
		public string Background { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[JsonPropertyName ("foreground")]
		public string Foreground { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public static class VisualStyleManager {
		private static readonly string visualStylesFilename = "visualstyles.json";
		private static VisualStyle _defaultStyles = null;

		private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true,
			IgnoreNullValues = true,
			WriteIndented = true,
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter (),
				new ColorSchemeJsonConverter ()
			},
			
		};

		static VisualStyleManager ()
		{

		}

		/// <summary>
		/// 
		/// </summary>
		public static VisualStyle Defaults { get { return _defaultStyles; } }

		/// <summary>
		/// 
		/// </summary>
		public static void LoadDefaults ()
		{
			// Load the default styles from the Terminal.Gui assembly
			using (Stream stream = typeof (VisualStyleManager).Assembly.GetManifestResourceStream ($"Terminal.Gui.Resources.{visualStylesFilename}"))
			using (StreamReader reader = new StreamReader (stream)) {
				string json = reader.ReadToEnd ();
				_defaultStyles = LoadFromJson (json);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static VisualStyle LoadFromJson (string json)
		{
			return JsonSerializer.Deserialize<VisualStyle> (json, serializerOptions);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="style"></param>
		/// <returns></returns>
		public static string ToJson (VisualStyle style)
		{
			return JsonSerializer.Serialize<VisualStyle> (style, serializerOptions);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="style"></param>
		public static void ApplyStyles (VisualStyle style)
		{
			// ColorSchemes
			foreach (var scheme in style.ColorSchemes) {
				Colors.ColorSchemes [scheme.Key] = scheme.Value;
			}

			// Apply the style to the various views
			//View.DefaultBackground = style.View.Background;
			//View.DefaultForeground = style.View.Foreground;
			//View.DefaultBackground = style.ListView.Background;
			//View.DefaultForeground = style.ListView.Foreground;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		public static void ApplyStyles (string json)
		{
			// Deserialize the JSON into a VisualStyle object
			ApplyStyles (JsonSerializer.Deserialize<VisualStyle> (json, serializerOptions));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filePath"></param>
		public static void ApplyStylesFromFile (string filePath)
		{
			// Read the JSON file
			string json = File.ReadAllText (filePath);

			ApplyStyles (json);
		}

		/// <summary>
		/// Writes the default styles to a JSON file.
		/// </summary>
		public static void SaveDefaultStylesToFile (string path)
		{
			var visualStyle = new VisualStyle ();
			visualStyle.ColorSchemes = Colors.ColorSchemes;

			// Serialize the Colors object to a JSON string
			string json = JsonSerializer.Serialize<VisualStyle> (visualStyle, serializerOptions);

			// Write the JSON string to the file specified by filePath
			File.WriteAllText (path, json);
		}

		/// <summary>
		/// Applies all found <see cref="VisualStyle"/>s to <see cref="Application"/>.
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
		/// <exception cref="NotImplementedException"></exception>
		public static void ApplyStyles ()
		{
			// Styles in Terminal.Gui assembly --lowest priority
			Debug.Assert (Defaults != null);

			// Global Styles in local directories (./.tui/{visualStylesFilename})
			string globalLocal = $"./.tui/{visualStylesFilename}";
			if (File.Exists (globalLocal)) {
				// Load the local file
				ApplyStylesFromFile (globalLocal);
			}

			// Global Styles in user home dir (~/.tui/{visualStylesFilename})
			string globalHome = $"~/.tui/{visualStylesFilename}";
			if (File.Exists (globalHome)) {
				// Load the local file
				ApplyStylesFromFile (globalHome);
			}

			// App Styles in app exe resources (Resources/{visualStylesFilename})
			var embeddedStylesResourceName = System.Reflection.Assembly.GetEntryAssembly ().GetManifestResourceNames ().First(x => x.EndsWith(visualStylesFilename));
			if (embeddedStylesResourceName != null) {
				using (Stream stream = System.Reflection.Assembly.GetEntryAssembly ().GetManifestResourceStream (embeddedStylesResourceName))
				using (StreamReader reader = new StreamReader (stream)) {
					string json = reader.ReadToEnd ();
					ApplyStyles (json);
				}
			}

			// Get app name
			string appName = System.Reflection.Assembly.GetEntryAssembly ().FullName.Split(',')[0].Trim();

			// App Styles in local directories (./.tui/appname.visualstyles.json)
			string appLocal = $"./.tui/{appName}.{visualStylesFilename}";
			if (File.Exists (appLocal)) {
				// Load the local file
				ApplyStylesFromFile (appLocal);
			}

			// App Styles in the users's home directory (~/.tui/appname.visualstyles.json)
			string appHome = $"~/.tui/{appName}.{visualStylesFilename}";
			if (File.Exists (appHome)) {
				// Load the global file
				ApplyStylesFromFile (appHome);
			}
		}
	}
}
