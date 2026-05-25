namespace Terminal.Gui.Configuration;

/// <summary>
///     Abstracts theme management. Allows switching themes and querying available themes.
/// </summary>
public interface IThemeManager
{
    /// <summary>Gets the name of the currently active theme.</summary>
    string CurrentThemeName { get; }

    /// <summary>Gets the names of all available themes.</summary>
    IReadOnlyList<string> ThemeNames { get; }

    /// <summary>
    ///     Switches the active theme. This updates all ThemeScope Settings POCOs
    ///     and triggers redraw on affected views.
    /// </summary>
    /// <param name="themeName">The name of the theme to activate.</param>
    /// <returns><see langword="true"/> if the theme was found and activated; otherwise <see langword="false"/>.</returns>
    bool SwitchTheme (string themeName);
}
