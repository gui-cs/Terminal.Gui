#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Encapsulates the logic for reading and writing <see cref="SettingsScope"/> objects to configuration sources
/// </summary>
public class SourcesManager
{
    /// <summary>The list of paths to the configuration files.</summary>
    public Dictionary<ConfigLocations, string> Sources { get; } = new ();

    /// <summary>Updates the <see cref="SettingsScope"/> with the settings in a JSON string.</summary>
    /// <param name="settingsScope">The Settings Scope object ot update.</param>
    /// <param name="stream">Json document to update the settings with.</param>
    /// <param name="source">The source (filename/resource name) the Json document was read from.</param>
    /// <param name="location">The Config Location correspondig to <paramref name="source"/></param>
    /// <returns><see langword="true"/> if the source was updated.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool Update (SettingsScope? settingsScope, Stream stream, string source, ConfigLocations location)
    {
        if (settingsScope is null)
        {
            return false;
        }

        // Update the existing settings with the new settings.
        try
        {
            settingsScope.Update ((SettingsScope)JsonSerializer.Deserialize (stream, typeof (SettingsScope), SerializerContext.Options)!);
            CM.OnUpdated ();
            Logging.Trace ($"Read from \"{source}\"");
            if (!Sources.ContainsValue (source))
            {
                Sources.Add (location, source);
            }

            return true;
        }
        catch (JsonException e)
        {
            if (ThrowOnJsonErrors ?? false)
            {
                throw;
            }

            AddJsonError ($"Error deserializing {source}: {e.Message}");
        }

        return false;
    }

    /// <summary>Updates the <see cref="SettingsScope"/> with the settings in a JSON file.</summary>
    /// <param name="settingsScope">The Settings Scope object ot update.</param>
    /// <param name="filePath">The source (filename/resource name) the Json document was read from.</param>
    /// <param name="location">The Config Location correspondig to <paramref name="filePath"/></param>
    /// <returns><see langword="true"/> if the source was updated.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool Update (SettingsScope? settingsScope, string filePath, ConfigLocations location)
    {
        string realPath = filePath.Replace ("~", Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));

        if (!File.Exists (realPath))
        {
            Logging.Warning ($"\"{realPath}\" does not exist.");
            if (!Sources.ContainsValue (filePath))
            {
                Sources.Add (location, filePath);
            }

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

                bool ret = Update (settingsScope, stream, filePath, location);
                stream.Close ();
                stream.Dispose ();

                return ret;
            }
            catch (IOException ioe)
            {
                Logging.Warning ($"Couldn't open {filePath}. Retrying...: {ioe}");
                Task.Delay (100);
                retryCount++;
            }
        }

        return false;
    }

    /// <summary>Updates the <see cref="SettingsScope"/> with the settings in a JSON string.</summary>
    /// <param name="settingsScope">The Settings Scope object ot update.</param>
    /// <param name="json">Json document to update the settings with.</param>
    /// <param name="source">The source (filename/resource name) the Json document was read from.</param>
    /// <param name="location">The location.</param>
    /// <returns><see langword="true"/> if the source was updated.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool Update (SettingsScope? settingsScope, string? json, string source, ConfigLocations location)
    {
        if (string.IsNullOrEmpty (json))
        {
            return false;
        }
        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        return Update (settingsScope, stream, source, location);
    }

    /// <summary>Updates the <see cref="SettingsScope"/> with the settings from a Json resource.</summary>
    /// <param name="settingsScope">The Settings Scope object ot update.</param>
    /// <param name="assembly">The assembly to get the config resource from.</param>
    /// <param name="resourceName">The name of the Resources in the assembly containing the Json document.</param>
    /// <param name="location">The location.</param>
    /// <returns><see langword="true"/> if the source was updated.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool UpdateFromResource (SettingsScope? settingsScope, Assembly assembly, string resourceName, ConfigLocations location)
    {
        if (string.IsNullOrEmpty (resourceName))
        {
            Logging.Warning($"{resourceName} must not be null or empty.");
            return false;
        }

        using Stream? stream = assembly.GetManifestResourceStream (resourceName);

        if (stream is null)
        {
            Logging.Warning ($"Resource \"{resourceName}\" does not exist in \"{assembly.GetName ().Name}\".");
            return false;
        }

        return Update (settingsScope, stream, $"resource://[{assembly.GetName ().Name}]/{resourceName}", location);
    }


    /// <summary>Creates a JSON document with the configuration specified.</summary>
    /// <returns></returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal string ToJson (SettingsScope? scope)
    {
        //Logging.Trace ("ConfigurationManager.ToJson()");

        return JsonSerializer.Serialize (scope, typeof (SettingsScope), SerializerContext);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal Stream ToStream (SettingsScope? scope)
    {
        string json = JsonSerializer.Serialize (scope, typeof (SettingsScope), SerializerContext);

        // turn it into a stream
        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        return stream;
    }

}

