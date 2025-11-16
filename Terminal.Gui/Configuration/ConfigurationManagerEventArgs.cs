#nullable enable

namespace Terminal.Gui.Configuration;

/// <summary>Event arguments for the <see cref="ConfigurationManager"/> events.</summary>
public class ConfigurationManagerEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="ConfigurationManagerEventArgs"/></summary>
    public ConfigurationManagerEventArgs () { }
}


//public class ConfigurationLoadEventArgs : ResultEventArgs<SettingsScope>
//{
//    public ConfigLocations Location { get; }
//    public string? Path { get; }

//    public ConfigurationLoadEventArgs (ConfigLocations location, string? path)
//    {
//        Location = location;
//        Path = path;
//    }
//}
