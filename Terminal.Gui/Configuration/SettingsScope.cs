#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

// TODO: Change to internal to prevent app usage
/// <summary>
///     INTERNAL: The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties attributed with
///     <see cref="SettingsScope"/>.
/// </summary>
/// <example>
///     <code>
///  {
///    "$schema" : "https://gui-cs.github.io/Terminal.GuiV2Docs/schemas/tui-config-schema.json",
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
    public SettingsScope ()
    {
        ConfigProperty? configProperty = GetUninitializedProperty ("Theme");

        if (configProperty is {})
        {
            TryAdd ("Theme", configProperty);
        }

        configProperty = GetUninitializedProperty ("Themes");

        if (configProperty is { })
        {
            TryAdd ("Themes", configProperty);
        }
    }

    /// <summary>Points to our JSON schema.</summary>
    [JsonInclude]
    [JsonPropertyName ("$schema")]
    public string Schema { get; set; } = "https://gui-cs.github.io/Terminal.GuiV2Docs/schemas/tui-config-schema.json";

}
