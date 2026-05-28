// Copilot - Claude Opus 4.7

using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     End-to-end tests for the A2.1 two-pass MEC theme overlay applied through
///     <see cref="TuiConfigurationBuilder.ApplyToStaticFacades"/>.
/// </summary>
/// <remarks>
///     <para>
///         The contract under test: <c>BindThemeScope&lt;T&gt;</c> binds the root section first, then overlays
///         <c>Themes:{active}:{section}</c>. Properties present only in the root must survive; properties present
///         in the overlay must win. This mirrors legacy CM <c>Scope.Apply</c> property-level merge semantics.
///     </para>
/// </remarks>
public class ThemeOverlayMergeTests
{
    /// <summary>
    ///     When the theme overlay only mentions one property of a ThemeScope POCO, the other properties keep their
    ///     root-section values (not the compile-time defaults, not <see langword="null"/>).
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_ThemeOverlay_PreservesRootDefaultsForUnmentionedProperties ()
    {
        DialogSettings originalDialog = DialogSettings.Current;
        ThemeSettings originalTheme = ThemeSettings.Defaults;

        try
        {
            TuiConfigurationBuilder tuiBuilder = new ();

            tuiBuilder.RuntimeConfig = """
                                       {
                                         "Theme": { "Theme": "Custom" },
                                         "Dialog": {
                                           "DefaultShadow": "Opaque",
                                           "DefaultBorderStyle": "Double",
                                           "DefaultButtonAlignment": "Start"
                                         },
                                         "Themes": {
                                           "Custom": {
                                             "Dialog": {
                                               "DefaultBorderStyle": "Single"
                                             }
                                           }
                                         }
                                       }
                                       """;

            tuiBuilder.ApplyToStaticFacades ();

            Assert.Equal (LineStyle.Single, DialogSettings.Current.DefaultBorderStyle);
            Assert.Equal (ShadowStyles.Opaque, DialogSettings.Current.DefaultShadow);
            Assert.Equal (Alignment.Start, DialogSettings.Current.DefaultButtonAlignment);
        }
        finally
        {
            DialogSettings.Current = originalDialog;
            ThemeSettings.Defaults = originalTheme;
        }
    }

    /// <summary>
    ///     When no theme overlay exists for a POCO, the root section's values are applied verbatim.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_NoOverlay_UsesRootValuesAsIs ()
    {
        ButtonSettings originalButton = ButtonSettings.Current;
        ThemeSettings originalTheme = ThemeSettings.Defaults;

        try
        {
            TuiConfigurationBuilder tuiBuilder = new ();

            tuiBuilder.RuntimeConfig = """
                                       {
                                         "Theme": { "Theme": "Custom" },
                                         "Button": {
                                           "DefaultShadow": "None"
                                         },
                                         "Themes": { "Custom": { } }
                                       }
                                       """;

            tuiBuilder.ApplyToStaticFacades ();

            Assert.Equal (ShadowStyles.None, ButtonSettings.Current.DefaultShadow);
        }
        finally
        {
            ButtonSettings.Current = originalButton;
            ThemeSettings.Defaults = originalTheme;
        }
    }

    /// <summary>
    ///     The atomic-swap pattern produces a new <see cref="ButtonSettings"/> reference on each apply, never
    ///     mutates the existing instance in place. A reader that captured the prior reference still sees the prior
    ///     values.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_AtomicSwap_DoesNotMutatePriorReference ()
    {
        ButtonSettings originalButton = ButtonSettings.Current;
        ThemeSettings originalTheme = ThemeSettings.Defaults;

        try
        {
            TuiConfigurationBuilder tuiBuilder = new ();
            tuiBuilder.RuntimeConfig = """{ "Button": { "DefaultShadow": "Transparent" } }""";
            tuiBuilder.ApplyToStaticFacades ();

            ButtonSettings captured = ButtonSettings.Current;
            Assert.Equal (ShadowStyles.Transparent, captured.DefaultShadow);

            tuiBuilder.RuntimeConfig = """{ "Button": { "DefaultShadow": "None" } }""";
            tuiBuilder.ApplyToStaticFacades ();

            Assert.Equal (ShadowStyles.Transparent, captured.DefaultShadow);
            Assert.Equal (ShadowStyles.None, ButtonSettings.Current.DefaultShadow);
            Assert.NotSame (captured, ButtonSettings.Current);
        }
        finally
        {
            ButtonSettings.Current = originalButton;
            ThemeSettings.Defaults = originalTheme;
        }
    }
}
