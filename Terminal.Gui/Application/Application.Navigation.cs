#nullable enable
namespace Terminal.Gui;

public static partial class Application // Navigation stuff
{
    /// <summary>
    ///     Gets the <see cref="ApplicationNavigation"/> instance for the current <see cref="Application"/>.
    /// </summary>
    public static ApplicationNavigation? Navigation { get; internal set; }
}
