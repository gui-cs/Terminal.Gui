namespace Terminal.Gui.Configuration;

/// <summary>
///     MEC-backed implementation of <see cref="IThemeManager"/>.
///     During the transition period, this delegates to the existing static <see cref="ThemeManager"/>
///     for theme data while providing the new interface.
/// </summary>
public class MecThemeManager : IThemeManager
{
    private readonly TuiConfigurationBuilder _builder;

    /// <summary>Initializes a new instance of <see cref="MecThemeManager"/>.</summary>
    public MecThemeManager (TuiConfigurationBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc/>
    public string CurrentThemeName => ThemeSettings.Defaults.Theme;

    /// <inheritdoc/>
    public IReadOnlyList<string> ThemeNames
    {
        get
        {
            // During transition, delegate to existing ThemeManager
            try
            {
                return ThemeManager.GetThemeNames ();
            }
            catch (InvalidOperationException)
            {
                return [ThemeManager.DEFAULT_THEME_NAME];
            }
        }
    }

    /// <inheritdoc/>
    public bool SwitchTheme (string themeName)
    {
        if (string.IsNullOrEmpty (themeName))
        {
            return false;
        }

        // Verify the theme exists before switching
        if (!ThemeNames.Contains (themeName))
        {
            return false;
        }

        // During transition, also update the existing ThemeManager
        try
        {
            ThemeManager.Theme = themeName;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }

        // Update the settings POCO only on success
        ThemeSettings.Defaults.Theme = themeName;

        // Re-apply all ThemeScope POCOs from configuration
        _builder.ApplyToStaticFacades ();

        return true;
    }
}
