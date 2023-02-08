using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;


#nullable enable

namespace Terminal.Gui.Configuration {
	public static partial class ConfigurationManager {
		/// <summary>
		/// The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties 
		/// attributed with  <see cref="SettingsScope"/>.
		/// </summary>
		/// <example><code>
		///  {
		///    "$schema" : "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
		///    "Application.UseSystemConsole" : true,
		///    "Theme" : "Default",
		///    "Themes": {
		///    },
		///  },
		/// </code></example>
		/// <remarks>
		/// </remarks>
		[JsonConverter (typeof (ScopeJsonConverter<SettingsScope>))]
		public class SettingsScope : Scope<SettingsScope> {
			/// <summary>
			/// Points to our JSON schema.
			/// </summary>
			[JsonInclude, JsonPropertyName ("$schema")]
			public string Schema { get; set; } = "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json";


			/// <summary>
			/// Updates the <see cref="SettingsScope"/> with the settings in a JSON file.
			/// </summary>
			/// <param name="filePath"></param>
			public SettingsScope? Update (string filePath)
			{
				if (!File.Exists (filePath)) {
					Debug.WriteLine ($"ConfigurationManager: Configuration file \"{filePath}\" does not exist.");
					return this;
				}

				var stream = File.OpenRead (filePath);
				return Update (stream, filePath);
			}

			/// <summary>
			/// Updates the <see cref="SettingsScope"/> with the settings from a Json resource.
			/// </summary>
			/// <param name="assembly"></param>
			/// <param name="resourceName"></param>
			public SettingsScope? UpdateFromResource (Assembly assembly, string resourceName)
			{
				if (resourceName == null || string.IsNullOrEmpty (resourceName)) {
					Debug.WriteLine ($"ConfigurationManager: Resource \"{resourceName}\" does not exist in \"{assembly.FullName}\".");
					return this;
				}
				
				using Stream? stream = assembly.GetManifestResourceStream (resourceName)!;
				if (stream == null) {
					Debug.WriteLine ($"ConfigurationManager: Failed to read resource \"{resourceName}\" from \"{assembly.FullName}\".");
					return this;
				}

				return Update (stream, resourceName);
			}
			
			/// <summary>
			/// Updates the <see cref="SettingsScope"/> with the settings in a JSON string.
			/// </summary>
			/// <param name="stream">Json document to update the settings with.</param>
			/// <param name="source">The source (filename/resource name) the Json document was read from.</param>
			public SettingsScope? Update (Stream stream, string source)
			{
				// Update the existing settings with the new settings.
				try {
					Update (JsonSerializer.Deserialize<SettingsScope> (stream, serializerOptions)!);
					OnUpdated ();
					Debug.WriteLine ($"ConfigurationManager: Read configuration from \"{source}\"");
					return this;
				} catch (JsonException e) {
					if (ThrowOnJsonErrors ?? false) {
						throw;
					} else {
						AddJsonError ($"Error deserializing {source}: {e.Message}");
					}
				}
				return this;
			}

			/// <summary>
			/// Updates the <see cref="SettingsScope"/> with the settings in a JSON string.
			/// </summary>
			/// <param name="json">Json document to update the settings with.</param>
			/// <param name="source">The source (filename/resource name) the Json document was read from.</param>
			public SettingsScope? Update (string json, string source)
			{
				var stream = new MemoryStream ();
				var writer = new StreamWriter (stream);
				writer.Write (json);
				writer.Flush ();
				stream.Position = 0;

				return Update (stream, source);
			}
		}
	}
}
