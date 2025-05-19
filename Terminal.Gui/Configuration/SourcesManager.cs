#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Terminal.Gui.Configuration;

/// <summary>
///    Manages the <see cref="ConfigurationManager"/> Sources and provides the API for loading them. Source is a location where a configuration can be stored. Sources are defined in <see cref="ConfigLocations"/>.
/// </summary>
public class SourcesManager
{
    /// <summary>
    ///     Provides a map from each of the <see cref="ConfigLocations"/> to file system and resource paths that have been loaded by <see cref="ConfigurationManager"/>.
    /// </summary>
    public Dictionary<ConfigLocations, string> Sources { get; } = new ();

    /// <summary>INTERNAL: Loads <paramref name="stream"/> into the specified <see cref="SettingsScope"/>.</summary>
    /// <param name="settingsScope">The Settings Scope object that <paramref name="stream"/> will be loaded into.</param>
    /// <param name="stream">Json document to update the settings with.</param>
    /// <param name="source">The source (filename/resource name) the Json document was read from.</param>
    /// <param name="location">The Config Location corresponding to <paramref name="source"/></param>
    /// <returns><see langword="true"/> if the settingsScope was updated.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal bool Load (SettingsScope? settingsScope, Stream stream, string source, ConfigLocations location)
    {
        if (settingsScope is null)
        {
            return false;
        }

        // Update the existing settings with the new settings.
        try
        {
#if DEBUG
            string? json = new StreamReader (stream).ReadToEnd ();
            stream.Position = 0;
            Debug.Assert (json != null, "json != null");
#endif
            SettingsScope? scope = JsonSerializer.Deserialize (stream, typeof (SettingsScope), ConfigurationManager.SerializerContext.Options) as SettingsScope;
            settingsScope.UpdateFrom (scope!);
            ConfigurationManager.OnUpdated ();

            AddSource (location, source);

            Logging.Trace ($"Read configuration from \"{source}\" - ConfigLocation: {location}");
            return true;
        }
        catch (JsonException e)
        {
            if (ConfigurationManager.ThrowOnJsonErrors ?? false)
            {
                throw;
            }

            ConfigurationManager.AddJsonError ($"Error reading {source}: {e.Message}");
        }

        return false;
    }

    internal void AddSource (ConfigLocations location, string source)
    {
        if (!Sources.TryAdd (location, source))
        {
            //Logging.Warning ($"{location} has already been added to Sources.");
            Sources [location] = source;
        }
    }


    /// <summary>INTERNAL: Loads the `config.json` file a <paramref name="filePath"/> into the specified <see cref="SettingsScope"/>.</summary>
    /// <param name="settingsScope">The Settings Scope object that <paramref name="filePath"/> will be loaded into.</param>
    /// <param name="filePath">Json document to update the settings with.</param>
    /// <param name="location">The Config Location corresponding to <paramref name="filePath"/></param>
    /// <returns><see langword="true"/> if the settingsScope was updated.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal bool Load (SettingsScope? settingsScope, string filePath, ConfigLocations location)
    {
        string realPath = filePath.Replace ("~", Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));

        if (!File.Exists (realPath))
        {
            //Logging.Warning ($"\"{realPath}\" does not exist.");

            // Always add the source even if it doesn't exist.
            AddSource (location, filePath);
            return true;
        }

        int retryCount = 0;

        // Sometimes when the config file is written by an external agent, the change notification comes
        // before the file is closed. This works around that.
        while (retryCount < 2)
        {
            try
            {
                FileStream? stream = File.OpenRead (realPath);

                bool ret = Load (settingsScope, stream, filePath, location);
                stream.Close ();
                stream.Dispose ();

                return ret;
            }
            catch (IOException ioe)
            {
                Logging.Warning ($"{ioe.Message}. Retrying...");
                Task.Delay (100);
                retryCount++;
            }
        }

        return false;
    }

    /// <summary>INTERNAL: Loads the Json document in <paramref name="json"/> into the specified <see cref="SettingsScope"/>.</summary>
    /// <param name="settingsScope">The Settings Scope object that <paramref name="json"/> will be loaded into.</param>
    /// <param name="json">Json document to update the settings with.</param>
    /// <param name="source">The source (filename/resource name) the Json document was read from.</param>
    /// <param name="location">The Config Location corresponding to <paramref name="json"/></param>
    /// <returns><see langword="true"/> if the settingsScope was updated.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal bool Load (SettingsScope? settingsScope, string? json, string source, ConfigLocations location)
    {
        Debug.Assert (location != ConfigLocations.All);

        if (string.IsNullOrEmpty (json))
        {
            return false;
        }
        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        return Load (settingsScope, stream, source, location);
    }

    /// <summary>INTERNAL: Loads the Json document from the resource named <paramref name="resourceName"/> from <paramref name="assembly"/> into the specified <see cref="SettingsScope"/>.</summary>
    /// <param name="settingsScope">The Settings Scope object that <paramref name="resourceName"/> will be loaded into.</param>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <param name="resourceName">The name of the resource containing the Json document was read from.</param>
    /// <param name="location">The Config Location corresponding to <paramref name="resourceName"/></param>
    /// <returns><see langword="true"/> if the settingsScope was updated.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal bool Load (SettingsScope? settingsScope, Assembly assembly, string resourceName, ConfigLocations location)
    {
        if (string.IsNullOrEmpty (resourceName))
        {
            Logging.Warning ($"{resourceName} must not be null or empty.");
            return false;
        }

        using Stream? stream = assembly.GetManifestResourceStream (resourceName);

        if (stream is null)
        {
            Logging.Warning ($"Resource \"{resourceName}\" does not exist in \"{assembly.GetName ().Name}\".");
            return false;
        }

        return Load (settingsScope, stream, $"resource://[{assembly.GetName ().Name}]/{resourceName}", location);
    }


    /// <summary>
    ///     INTERNAL: Returns a JSON document with the configuration specified.
    /// </summary>
    /// <param name="scope"></param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal string ToJson (SettingsScope? scope)
    {
        //Logging.Debug  ("ConfigurationManager.ToJson()");
        return JsonSerializer.Serialize (scope, typeof (SettingsScope), ConfigurationManager.SerializerContext);
    }

    /// <summary>
    ///     INTERNAL: Returns a stream with the configuration specified.
    /// </summary>
    /// <param name="scope"></param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal Stream ToStream (SettingsScope? scope)
    {
        string json = JsonSerializer.Serialize (scope, typeof (SettingsScope), ConfigurationManager.SerializerContext);

        // turn it into a stream
        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        return stream;
    }
}

