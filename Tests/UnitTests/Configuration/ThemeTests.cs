using System.Text.Json;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

/// <summary>
///     Tests Settings["Theme"] and ThemeManager.Theme
/// </summary>
public class ThemeTests
{
    public static readonly JsonSerializerOptions _jsonOptions = new ()
    {
        Converters = { new AttributeJsonConverter (), new ColorJsonConverter () }
    };


}
