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
		/// The nested class <see cref="ConfigScopeConverter{rootT}"/> Does all the heavy lifting for serialization 
		/// of the <see cref="SettingsScope"/> object. Uses reflection to determine
		/// how to serialize properties based on their type (and [JsonConverter] attributes). 
		/// </remarks>
		[JsonConverter (typeof (ConfigScopeConverter<SettingsScope>))]
		public class SettingsScope : Scope {

			/// <summary>
			/// Event arguments for the <see cref="ConfigurationManager"/> events.
			/// </summary>
			public class EventArgs : System.EventArgs {

				/// <summary>
				/// Initializes a new instance of <see cref="EventArgs"/>
				/// </summary>
				public EventArgs ()
				{
				}
			}
			
			/// <summary>
			/// Points to our JSON schema.
			/// </summary>
			[JsonInclude, JsonPropertyName ("$schema")]
			public string Schema { get; set; } = "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json";
		}
	}
}
