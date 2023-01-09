using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.Resources;

namespace Terminal.Gui.Core {
	 
	//  "ColorSchemes": {
	//    "Base": {
	//	    "normal": {
	//	      "foreground": "White",
	//	      "background": "Blue"
	//	    },
	//	    "focus": {
	//		"foreground": "Black",
	//	      "background": "Gray"
	//	    },
	//	    "hotNormal": {
	//		"foreground": "BrightCyan",
	//	      "background": "Blue"
	//	    },
	//	    "hotFocus": {
	//		"foreground": "BrightBlue",
	//	      "background": "Gray"
	//	    },
	//	    "disabled": {
	//		"foreground": "DarkGray",
	//	      "background": "Blue"
	//	    }
	//    }
	//  },
	//}

	/// <summary>
	/// 
	/// </summary>
	public class VisualStyle {
		public VisualStyle()
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
		private static VisualStyle _defaultStyles = null;

		private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true,
			IgnoreNullValues = true,
			WriteIndented = true,
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter (),
				new ColorSchemeJsonConverter ()
			}
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
			using (Stream stream = typeof (VisualStyleManager).Assembly.GetManifestResourceStream ("Terminal.Gui.Resources.visualstyles.json"))
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
			Colors.Base = style.ColorSchemes ["Base"];
			Colors.TopLevel = style.ColorSchemes ["TopLevel"];
			Colors.Error = style.ColorSchemes ["Error"];
			Colors.Dialog = style.ColorSchemes ["Dialog"];
			Colors.Menu = style.ColorSchemes ["Menu"];

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
			ApplyStyles(JsonSerializer.Deserialize<VisualStyle> (json, serializerOptions));
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
		/// 
		/// </summary>
		/// <param name="appName"></param>
		/// <exception cref="FileNotFoundException"></exception>
		public static void LoadAppStyle (string appName)
		{
			// Construct the file paths
			string localFilePath = $"./.tui/{appName}.visualstyle.json";
			string globalFilePath = $"~/.tui/{appName}.visualstyle.json";

			// Check if the local file exists
			if (File.Exists (localFilePath)) {
				// Load the local file
				ApplyStyles (localFilePath);
				return;
			}

			// Check if the global file exists
			if (File.Exists (globalFilePath)) {
				// Load the global file
				ApplyStyles (globalFilePath);
				return;
			}

			// No visual style file was found
			throw new FileNotFoundException ($"Unable to find visual style file for app '{appName}'.");
		}
	}
}
