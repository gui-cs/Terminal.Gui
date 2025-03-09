#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties attributed with
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
    /// <summary>The list of paths to the configuration files.</summary>
    public Dictionary<ConfigLocations, string> Sources { get; } = new ();

    /// <summary>Points to our JSON schema.</summary>
    [JsonInclude]
    [JsonPropertyName ("$schema")]
    public string Schema { get; set; } = "https://gui-cs.github.io/Terminal.GuiV2Docs/schemas/tui-config-schema.json";

    /// <summary>Updates the <see cref="SettingsScope"/> with the settings in a JSON string.</summary>
    /// <param name="stream">Json document to update the settings with.</param>
    /// <param name="source">The source (filename/resource name) the Json document was read from.</param>
    /// <param name="location">Location</param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public SettingsScope? Update (Stream stream, string source, ConfigLocations location)
    {
        // Update the existing settings with the new settings.
        try
        {
            Update ((SettingsScope)JsonSerializer.Deserialize (stream, typeof (SettingsScope), SerializerOptions)!);
            OnUpdated ();
            Logging.Trace ($"Read from \"{source}\"");
            if (!Sources.ContainsValue (source))
            {
                Sources.Add (location, source);
            }

            return this;
        }
        catch (JsonException e)
        {
            if (ThrowOnJsonErrors ?? false)
            {
                throw;
            }

            AddJsonError ($"Error deserializing {source}: {e.Message}");
        }

        return this;
    }

    /// <summary>Updates the <see cref="SettingsScope"/> with the settings in a JSON file.</summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="location">The location</param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public SettingsScope? Update (string filePath, ConfigLocations location)
    {
        string realPath = filePath.Replace ("~", Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));

        if (!File.Exists (realPath))
        {
            Logging.Warning ($"\"{realPath}\" does not exist.");
            if (!Sources.ContainsValue (filePath))
            {
                Sources.Add (location, filePath);
            }

            return this;
        }

        int retryCount = 0;

        // Sometimes when the config file is written by an external agent, the change notification comes
        // before the file is closed. This works around that.
        while (retryCount < 2)
        {
            try
            {
                FileStream? stream = File.OpenRead (realPath);
                SettingsScope? s = Update (stream, filePath, location);
                stream.Close ();
                stream.Dispose ();

                return s;
            }
            catch (IOException ioe)
            {
                Logging.Warning ($"Couldn't open {filePath}. Retrying...: {ioe}");
                Task.Delay (100);
                retryCount++;
            }
        }

        return null;
    }

    /// <summary>Updates the <see cref="SettingsScope"/> with the settings in a JSON string.</summary>
    /// <param name="json">Json document to update the settings with.</param>
    /// <param name="source">The source (filename/resource name) the Json document was read from.</param>
    /// <param name="location">The location.</param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public SettingsScope? Update (string? json, string source, ConfigLocations location)
    {
        if (string.IsNullOrEmpty (json))
        {
            return null;
        }
        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        return Update (stream, source, location);
    }

    /// <summary>Updates the <see cref="SettingsScope"/> with the settings from a Json resource.</summary>
    /// <param name="assembly"></param>
    /// <param name="resourceName"></param>
    /// <param name="location"></param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public SettingsScope? UpdateFromResource (Assembly assembly, string resourceName, ConfigLocations location)
    {
        if (string.IsNullOrEmpty (resourceName))
        {
            Debug.WriteLine (
                             $"ConfigurationManager: Resource \"{resourceName}\" does not exist in \"{assembly.GetName ().Name}\"."
                            );

            return this;
        }

        using Stream? stream = assembly.GetManifestResourceStream (resourceName);

        if (stream is null)
        {
            return null;
        }

        return Update (stream, $"resource://[{assembly.GetName ().Name}]/{resourceName}", location);
    }
}
