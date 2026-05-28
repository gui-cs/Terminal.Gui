#pragma warning disable CS0618 // Obsolete - MecThemeManager forwards from legacy ThemeManager during transition

namespace Terminal.Gui.Configuration;

/// <summary>
///     MEC-backed implementation of <see cref="IThemeManager"/>.
///     During the transition period (PR #5411), this delegates writes to the legacy static <see cref="ThemeManager"/>
///     because the runtime theme/scheme dictionary is still owned by <see cref="ConfigurationManager.Settings"/>.
///     The Phase A2 work in #5416 will let this type own the theme/scheme data directly.
/// </summary>
public class MecThemeManager : IThemeManager
{
    private readonly TuiConfigurationBuilder _builder;

    /// <summary>Initializes a new instance of <see cref="MecThemeManager"/>.</summary>
    public MecThemeManager (TuiConfigurationBuilder builder)
    {
        _builder = builder;

        // Forward legacy ThemeManager.ThemeChanged into the IThemeManager.ThemeChanged event so
        // consumers of the new API see every theme change, regardless of which API triggered it.
        ThemeManager.ThemeChanged += OnLegacyThemeChanged;
    }

    private void OnLegacyThemeChanged (object? sender, App.EventArgs<string> e) => ThemeChanged?.Invoke (this, e);

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
    public event EventHandler<App.EventArgs<string>>? ThemeChanged;

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

        // During transition, also update the existing ThemeManager. Its setter raises
        // ThemeManager.ThemeChanged, which is forwarded via OnLegacyThemeChanged above.
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
