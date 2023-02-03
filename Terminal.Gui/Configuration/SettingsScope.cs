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
		}
	}
}
