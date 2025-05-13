#nullable enable
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class SchemeManagerTests
{
    [Fact]
    public void GetCurrentSchemes_Not_Enabled_Gets_Schemes ()
    {
        CM.Disable ();

        Dictionary<string, Scheme?>? schemes = SchemeManager.GetCurrentSchemes ();
        Assert.NotNull (schemes);
        Assert.NotNull (schemes ["Base"]);
        Assert.True (schemes!.ContainsKey ("Base"));
        Assert.True (schemes.ContainsKey ("base"));
    }

    [Fact]
    public void GetCurrentSchemes_Enabled_Gets_Current ()
    {
        CM.Enable();

        Dictionary<string, Scheme?>? schemes = SchemeManager.GetCurrentSchemes ();
        Assert.NotNull (schemes);
        Assert.NotNull (schemes ["Base"]);
        Assert.True (schemes!.ContainsKey ("Base"));
        Assert.True (schemes.ContainsKey ("base"));

        CM.Disable();
    }

    [Fact]
    public void GetHardCodedSchemes_Gets_HardCoded_Theme_Schemes ()
    {
        Dictionary<string, Scheme?>? hardCoded = SchemeManager.GetHardCodedSchemes ();

        Assert.Equal (View.GetHardCodedSchemes (), hardCoded);

    }

    [Fact]
    public void Not_Case_Sensitive_Disabled ()
    {
        Assert.False (IsEnabled);
        Dictionary<string, Scheme?>? current = SchemeManager.GetCurrentSchemes ();
        Assert.NotNull (current);

        Assert.True (current!.ContainsKey ("Base"));
        Assert.True (current.ContainsKey ("base"));
    }

    [Fact]
    public void Not_Case_Sensitive_Enabled ()
    {
        Assert.False (IsEnabled);
        Enable();

        Assert.True (SchemeManager.GetCurrentSchemes ()!.ContainsKey ("Base"));
        Assert.True (SchemeManager.GetCurrentSchemes ()!.ContainsKey ("base"));

        ResetToHardCodedDefaults ();
        Dictionary<string, Scheme?>? current = SchemeManager.GetCurrentSchemes ();
        Assert.NotNull (current);

        Assert.True (current!.ContainsKey ("Base"));
        Assert.True (current.ContainsKey ("base"));

        ResetToHardCodedDefaults ();
        Disable();
    }


    [Fact]
    public void Load_Adds ()
    {
        // arrange
        Enable ();
        ResetToHardCodedDefaults ();


        var theme = new ThemeScope ();
        Assert.NotEmpty (theme);

        Assert.Equal (5, SchemeManager.Schemes.Count);

        theme ["Schemes"].PropertyValue = SchemeManager.Schemes;

        Dictionary<string, Scheme> schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue;
        Assert.Equal (SchemeManager.Schemes.Count, schemes.Count);

        var newTheme = new ThemeScope ();

        var scheme = new Scheme
        {
            // note: Scheme's can't be partial; default for each attribute
            // is always White/Black
            Normal = new Attribute (Color.Red, Color.Green),
            Focus = new Attribute (Color.Cyan, Color.BrightCyan),
            HotNormal = new Attribute (Color.Yellow, Color.BrightYellow),
            HotFocus = new Attribute (Color.Green, Color.BrightGreen),
            Disabled = new Attribute (Color.Gray, Color.DarkGray)
        };

        newTheme ["Schemes"].PropertyValue = SchemeManager.GetCurrentSchemes ();
        Assert.Equal (5, SchemeManager.Schemes.Count);

        // add a new Scheme to the newTheme
        ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue) ["Test"] = scheme;

        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue;
        Assert.Equal (SchemeManager.Schemes.Count, schemes.Count);

        // Act
        theme.UpdateFrom (newTheme);

        // Assert
        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue;
        Assert.Equal (schemes ["Test"].Normal, scheme.Normal);
        Assert.Equal (schemes ["Test"].Focus, scheme.Focus);
        ResetToHardCodedDefaults ();
        Disable ();
    }

    [Fact]
    public void Load_Changes ()
    {
        // arrange
        Enable ();
        ResetToHardCodedDefaults ();


        var theme = new ThemeScope ();
        Assert.NotEmpty (theme);

        var scheme = new Scheme
        {
            // note: Scheme's can't be partial; default for each attribute
            // is always White/Black
            Normal = new Attribute (Color.Red, Color.Green),
            Focus = new Attribute (Color.Cyan, Color.BrightCyan),
            HotNormal = new Attribute (Color.Yellow, Color.BrightYellow),
            HotFocus = new Attribute (Color.Green, Color.BrightGreen),
            Disabled = new Attribute (Color.Gray, Color.DarkGray)
        };
        theme ["Schemes"].PropertyValue = SchemeManager.GetCurrentSchemes ();
        ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue) ["Test"] = scheme;

        Dictionary<string, Scheme> schemes =
            (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue;
        Assert.Equal (scheme.Normal, schemes ["Test"].Normal);
        Assert.Equal (scheme.Focus, schemes ["Test"].Focus);

        // Change just Normal
        var newTheme = new ThemeScope ();

        var newScheme = new Scheme
        {
            Normal = new Attribute (Color.Blue, Color.BrightBlue),
            Focus = scheme.Focus,
            HotNormal = scheme.HotNormal,
            HotFocus = scheme.HotFocus,
            Disabled = scheme.Disabled
        };
        newTheme ["Schemes"].PropertyValue = SchemeManager.GetCurrentSchemes ();
        ((Dictionary<string, Scheme>)newTheme ["Schemes"].PropertyValue) ["Test"] = newScheme;

        // Act
        theme.UpdateFrom (newTheme);

        // Assert
        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue;

        // Normal should have changed
        Assert.Equal (new Color (Color.Blue), schemes ["Test"].Normal.Foreground);
        Assert.Equal (new Color (Color.BrightBlue), schemes ["Test"].Normal.Background);
        Assert.Equal (new Color (Color.Cyan), schemes ["Test"].Focus.Foreground);
        Assert.Equal (new Color (Color.BrightCyan), schemes ["Test"].Focus.Background);
        ResetToHardCodedDefaults ();
        Disable ();
    }


    [Fact (Skip = "WIP")]
    public void Apply_UpdatesSchemes ()
    {
        Enable ();
        ResetToCurrentValues ();

        Assert.False (SchemeManager.Schemes!.ContainsKey ("test"));
        Assert.Equal (5, SchemeManager.Schemes.Count); // base, toplevel, menu, error, dialog

        var theme = new ThemeScope ();
        Assert.NotEmpty (theme);

        ThemeManager.Themes!.Add ("testTheme", theme);

        var scheme = new Scheme { Normal = new Attribute (Color.Red, Color.Green) };

        theme ["Schemes"].PropertyValue = new Dictionary<string, Scheme> (StringComparer.InvariantCultureIgnoreCase) { { "test", scheme } };

        Assert.Equal (
                      new Color (Color.Red),
                      ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue) ["test"].Normal.Foreground
                     );

        Assert.Equal (
                      new Color (Color.Green),
                      ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue) ["test"].Normal.Background
                     );

        // Act
        ThemeManager.Theme = "testTheme";
        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();
        Assert.Equal (5, SchemeManager.Schemes.Count); // base, toplevel, menu, error, dialog

        // Assert
        Scheme updatedScheme = SchemeManager.Schemes ["test"];
        Assert.Equal (new Color (Color.Red), updatedScheme.Normal.Foreground);
        Assert.Equal (new Color (Color.Green), updatedScheme.Normal.Background);

        // remove test Scheme from Colors to avoid failures on others unit tests with Scheme
        SchemeManager.Schemes.Remove ("test");
        Assert.Equal (5, SchemeManager.Schemes.Count);

        ResetToHardCodedDefaults ();
        Disable ();
    }
}
