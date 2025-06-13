#nullable enable
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

// TODO: Change to internal to prevent app usage
/// <summary>
///     INTERNAL: The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties attributed
///     with
///     <see cref="SettingsScope"/>.
/// </summary>
/// <example>
///     <code>
///  {
///    "$schema" : "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
///    "Application.UseSystemConsole" : true,
///    "Theme" : "Default",
///    "Themes": {
///    },
///  },
/// </code>
/// </example>
/// <remarks></remarks>
[JsonConverter (typeof (ScopeJsonConverter<SettingsScope>))]
public class SettingsScope : Scope<SettingsScope>
{
    /// <summary>
    ///     Initializes a new instance. The dictionary will be populated with uninitialized (
    ///     <see cref="ConfigProperty.HasValue"/>
    /// </summary>
    public SettingsScope ()
    {
        ConfigProperty? configProperty = GetUninitializedProperty ("Theme");

        TryAdd ("Theme", configProperty);

        configProperty = GetUninitializedProperty ("Themes");

        TryAdd ("Themes", configProperty);
    }

    /// <summary>Points to our JSON schema.</summary>
    [JsonInclude]
    [JsonPropertyName ("$schema")]
    public string Schema { get; set; } = "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json";
}
