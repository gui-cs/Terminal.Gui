using System.Text.Json;
using UnitTests;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeTests
{
    public static readonly JsonSerializerOptions _jsonOptions = new ()
    {
        Converters = { new AttributeJsonConverter (), new ColorJsonConverter () }
    };

    [Fact]
    [AutoInitShutdown (configLocation: ConfigLocations.LibraryResources)]
    public void TestApply ()
    {
        Reset ();

        var theme = new ThemeScope ();
        Assert.NotEmpty (theme);

        ThemeManager.Themes.Add ("testTheme", theme);

        Assert.Equal (LineStyle.Single, FrameView.DefaultBorderStyle);
        theme ["FrameView.DefaultBorderStyle"].PropertyValue = LineStyle.Double; // default is Single

        ThemeManager.Theme = "testTheme";
        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();

        Assert.Equal (LineStyle.Double, FrameView.DefaultBorderStyle);

        Reset ();
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigLocations.LibraryResources)]
    public void TestApply_UpdatesColors ()
    {
        // Arrange
        Reset ();

        Assert.False (SchemeManager.Schemes.ContainsKey ("test"));

        var theme = new ThemeScope ();
        Assert.NotEmpty (theme);

        ThemeManager.Themes.Add ("testTheme", theme);

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

        // Assert
        Scheme updatedScheme = SchemeManager.Schemes ["test"];
        Assert.Equal (new Color (Color.Red), updatedScheme.Normal.Foreground);
        Assert.Equal (new Color (Color.Green), updatedScheme.Normal.Background);

        // remove test Scheme from Colors to avoid failures on others unit tests with Scheme
        SchemeManager.Schemes.Remove ("test");
        Assert.Equal (5, SchemeManager.Schemes.Count);
        Reset ();
    }

    [Fact]
    public void TestSerialize_RoundTrip ()
    {
        // This is needed to test only this alone
        Reset ();

        var theme = new ThemeScope ();
        theme ["Dialog.DefaultButtonAlignment"].PropertyValue = Alignment.End;

        string json = JsonSerializer.Serialize (theme, _jsonOptions);

        var deserialized = JsonSerializer.Deserialize<ThemeScope> (json, _jsonOptions);

        Assert.Equal (
                      Alignment.End,
                      (Alignment)deserialized ["Dialog.DefaultButtonAlignment"].PropertyValue
                     );
        Reset ();
    }

    [Fact]
    public void TestUpdatFrom_Add ()
    {
        // arrange
        Reset ();

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

        newTheme ["Schemes"].PropertyValue = SchemeManager.GetDefaultSchemes ();
        Assert.Equal (5, SchemeManager.Schemes.Count);

        // add a new Scheme to the newTheme
        ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue) ["Test"] = scheme;

        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue;
        Assert.Equal (SchemeManager.Schemes.Count, schemes.Count);

        // Act
        theme.Update (newTheme);

        // Assert
        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue;
        Assert.Equal (schemes ["Test"].Normal, scheme.Normal);
        Assert.Equal (schemes ["Test"].Focus, scheme.Focus);
        Reset ();
    }

    [Fact]
    public void TestUpdatFrom_Change ()
    {
        // arrange
        Reset ();

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
        theme ["Schemes"].PropertyValue = SchemeManager.GetDefaultSchemes ();
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
        newTheme ["Schemes"].PropertyValue = SchemeManager.GetDefaultSchemes ();
        ((Dictionary<string, Scheme>)newTheme ["Schemes"].PropertyValue) ["Test"] = newScheme;

        // Act
        theme.Update (newTheme);

        // Assert
        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue;

        // Normal should have changed
        Assert.Equal (new Color (Color.Blue), schemes ["Test"].Normal.Foreground);
        Assert.Equal (new Color (Color.BrightBlue), schemes ["Test"].Normal.Background);
        Assert.Equal (new Color (Color.Cyan), schemes ["Test"].Focus.Foreground);
        Assert.Equal (new Color (Color.BrightCyan), schemes ["Test"].Focus.Background);
        Reset ();
    }
}
