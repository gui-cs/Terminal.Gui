using BenchmarkDotNet.Attributes;
using Terminal.Gui.Configuration;

namespace Terminal.Gui.Benchmarks.Configuration;

/// <summary>
///     Measures the cost of switching the active theme via
///     <c>ThemeManager.Theme = "X"; ConfigurationManager.Apply ()</c>.
///     Parametric over every built-in theme name shipped in the embedded <c>config.json</c>.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*ThemeSwitch*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Configuration", "Theme")]
public class ThemeSwitchBenchmark
{
    /// <summary>The built-in theme to switch to during each benchmark invocation.</summary>
    [ParamsSource (nameof (ThemeNames))]
    public string ThemeName { get; set; } = ThemeManager.DEFAULT_THEME_NAME;

    /// <summary>Returns the set of built-in theme names available after loading library resources.</summary>
    public static IEnumerable<string> ThemeNames
    {
        get
        {
            ConfigurationManager.Disable (true);
            ConfigurationManager.Enable (ConfigLocations.LibraryResources);

            IEnumerable<string> names = ThemeManager.GetThemeNames ();

            ConfigurationManager.Disable (true);

            return names;
        }
    }

    /// <summary>Loads the embedded configuration so all built-in themes are available.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        ConfigurationManager.Disable (true);
        ConfigurationManager.Enable (ConfigLocations.LibraryResources);
    }

    /// <summary>Resets the theme to Default before each iteration for a stable starting point.</summary>
    [IterationSetup]
    public void IterationSetup ()
    {
        ThemeManager.Theme = ThemeManager.DEFAULT_THEME_NAME;
        ConfigurationManager.Apply ();
    }

    /// <summary>
    ///     Switches the active theme and applies the change.
    ///     This is the user-facing hot path when cycling themes via a <see cref="Views.Shortcut"/>.
    /// </summary>
    [Benchmark]
    public void SwitchTheme ()
    {
        ThemeManager.Theme = ThemeName;
        ConfigurationManager.Apply ();
    }

    /// <summary>Ensures ConfigurationManager is disabled after all iterations.</summary>
    [GlobalCleanup]
    public void Cleanup ()
    {
        ConfigurationManager.Disable (true);
    }
}
