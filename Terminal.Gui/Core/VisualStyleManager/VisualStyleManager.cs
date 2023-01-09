using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Core.VisualStyleManager {

	public class VisualStyle {
		[JsonPropertyName ("view")]
		public ViewStyle View { get; set; }

		[JsonPropertyName ("listview")]
		public ViewStyle ListView { get; set; }
	}

	public class ViewStyle {
		[JsonPropertyName ("background")]
		public string Background { get; set; }

		[JsonPropertyName ("foreground")]
		public string Foreground { get; set; }
	}

	class VisualStyleManager {
		public static void ApplyStyle (string filePath)
		{
			// Read the JSON file
			string json = File.ReadAllText (filePath);

			// Deserialize the JSON into a VisualStyle object
			VisualStyle style = JsonSerializer.Deserialize<VisualStyle> (json);

			// Apply the style to the various views
			View.DefaultBackground = style.View.Background;
			View.DefaultForeground = style.View.Foreground;
			View.DefaultBackground = style.ListView.Background;
			View.DefaultForeground = style.ListView.Foreground;
		}

		public static void LoadAppStyle (string appName)
		{
			// Construct the file paths
			string localFilePath = $"./.tui/{appName}.visualstyle.json";
			string globalFilePath = $"~/.tui/{appName}.visualstyle.json";

			// Check if the local file exists
			if (File.Exists (localFilePath)) {
				// Load the local file
				ApplyStyle (localFilePath);
				return;
			}

			// Check if the global file exists
			if (File.Exists (globalFilePath)) {
				// Load the global file
				ApplyStyle (globalFilePath);
				return;
			}

			// No visual style file was found
			throw new FileNotFoundException ($"Unable to find visual style file for app '{appName}'.");
		}
	}
}
