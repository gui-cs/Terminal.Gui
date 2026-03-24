using UnitTests;

namespace DrawingTests;

/// <summary>
///     Integration tests for <see cref="TabHeaderRenderer"/> wired through <see cref="Border"/> properties.
///     These verify that setting <see cref="BorderSettings.Tab"/> on a View's Border causes the
///     header protrusion to be rendered correctly.
/// </summary>

// Copilot
public class BorderHeaderIntegrationTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Header_Top_ShowSeparator ()
    {
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = false,
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab;
        view.Border.TabSide = Side.Top;
        view.Border.TabOffset = 0;
        view.Border.TabLength = 5;

        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │   │
                                              ├───┴─────╮
                                              │         │
                                              │         │
                                              ╰─────────╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void Header_Top_NoSeparator ()
    {
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = true,
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab;
        view.Border.TabSide = Side.Top;
        view.Border.TabOffset = 0;
        view.Border.TabLength = 5;

        Assert.True (view.HasFocus);
        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │   │
                                              │   ╰─────╮
                                              │         │
                                              │         │
                                              ╰─────────╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void Header_Top_WithOffset ()
    {
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = false,
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab;
        view.Border.TabSide = Side.Top;
        view.Border.TabOffset = 3;
        view.Border.TabLength = 5;

        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                                 ╭───╮
                                                 │   │
                                              ╭──┴───┴──╮
                                              │         │
                                              │         │
                                              ╰─────────╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void Header_Bottom_ShowSeparator ()
    {
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = false,
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 1, 1, 3);
        view.Border.Settings = BorderSettings.Tab;
        view.Border.TabSide = Side.Bottom;
        view.Border.TabOffset = 0;
        view.Border.TabLength = 5;

        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭─────────╮
                                              │         │
                                              │         │
                                              ├───┬─────╯
                                              │   │
                                              ╰───╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void Header_Bottom_NoSeparator ()
    {
        // Copilot
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = true,
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 1, 1, 3);
        view.Border.Settings = BorderSettings.Tab;
        view.Border.TabSide = Side.Bottom;
        view.Border.TabOffset = 0;
        view.Border.TabLength = 5;

        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭─────────╮
                                              │         │
                                              │         │
                                              │   ╭─────╯
                                              │   │
                                              ╰───╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void Header_Top_WithTitle ()
    {
        // Copilot
        // When both Tab and Title are set, Title is rendered within the tab header.
        // The Title flag also extends the right border line one row into the header area.
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = false,
            Title = "AB",
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = Side.Top;
        view.Border.TabOffset = 0;

        // TabLength auto-computed: "AB" = 2 columns + 2 border = 4
        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──╮
                                              │AB│      
                                              ├──┴──────╮
                                              │         │
                                              │         │
                                              ╰─────────╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void Header_Top_WithTitle_Focused ()
    {
        // Copilot
        // When focused with Tab+Title, the separator is suppressed (open gap).
        // The Title flag extends the right border line one row into the header area.
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = true,
            Title = "AB",
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = Side.Top;
        view.Border.TabOffset = 0;

        // TabLength auto-computed: "AB" = 2 columns + 2 border = 4
        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──╮
                                              │AB│      
                                              │  ╰──────╮
                                              │         │
                                              │         │
                                              ╰─────────╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void Header_Top_NoTitle_DefaultTabLength ()
    {
        // Copilot
        // When Tab is set without Title, TabLength defaults to 2 (0 content + 2 border lines)
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = false,
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab;
        view.Border.TabSide = Side.Top;
        view.Border.TabOffset = 0;

        // TabLength auto-computed: no title → 0 columns + 2 border = 2
        int tabLength = view.Border.TabLength!.Value;
        Assert.Equal (2, tabLength);

        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭╮
                                              ││
                                              ├┴────────╮
                                              │         │
                                              │         │
                                              ╰─────────╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void Header_Top_WithTitle_WithOffset ()
    {
        // Copilot
        // When Tab+Title+Offset are used, the Title flag extends the left and right borders
        // one row into the header area.
        IDriver driver = CreateTestDriver (11, 6);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = false,
            Title = "Tab",
            Width = 11,
            Height = 6,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = Side.Top;
        view.Border.TabOffset = 2;

        // TabLength auto-computed: "Tab" = 3 columns + 2 border = 5
        Assert.Equal (5, view.Border.TabLength!.Value);

        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());

        // Cells outside the tab header are transparent (show driver-init spaces).
        // Trailing spaces on header rows reflect this transparency.
        DriverAssert.AssertDriverContentsWithFrameAre ("""
                                                         ╭───╮    
                                                         │Tab│    
                                                       ╭─┴───┴───╮
                                                       │         │
                                                       │         │
                                                       ╰─────────╯
                                                       """,
                                                       output,
                                                       driver);

        view.Dispose ();
    }

    [Fact]
    public void TabLength_AutoComputed_WithTitle ()
    {
        // Copilot
        View view = new () { Title = "Test", Width = 10, Height = 5, BorderStyle = LineStyle.Rounded };

        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;

        // "Test" = 4 columns + 2 border lines = 6
        Assert.Equal (6, view.Border.TabLength!.Value);

        view.Dispose ();
    }

    [Fact]
    public void TabLength_AutoComputed_WithoutTitle ()
    {
        // Copilot
        View view = new () { Width = 10, Height = 5, BorderStyle = LineStyle.Rounded };

        view.Border.Settings = BorderSettings.Tab;

        // No title → 0 columns + 2 border lines = 2
        Assert.Equal (2, view.Border.TabLength!.Value);

        view.Dispose ();
    }

    [Fact]
    public void TabLength_ExplicitOverride ()
    {
        // Copilot
        View view = new () { Title = "Test", Width = 10, Height = 5, BorderStyle = LineStyle.Rounded };

        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabLength = 8;

        // Explicit value overrides auto-computation
        Assert.Equal (8, view.Border.TabLength.Value);

        view.Dispose ();
    }

    [Fact]
    public void TabLength_IgnoresTitle_WhenTitleFlagNotSet ()
    {
        // Copilot
        // When Tab is set but Title is NOT set, TabLength should be 2
        // even if Parent.Title has text.
        View view = new () { Title = "Test", Width = 10, Height = 5, BorderStyle = LineStyle.Rounded };

        view.Border.Settings = BorderSettings.Tab; // No Title flag

        // "Test" has 4 columns, but Title flag is off → 0 + 2 = 2
        Assert.Equal (2, view.Border.TabLength!.Value);

        view.Dispose ();
    }

    [Fact]
    public void Title_And_Tab_Not_Mutually_Exclusive ()
    {
        // Copilot
        // Verify that BorderSettings.Title and BorderSettings.Tab can both be set
        View view = new () { Title = "Hello", Width = 10, Height = 5, BorderStyle = LineStyle.Rounded };

        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;

        Assert.True (view.Border.Settings.HasFlag (BorderSettings.Tab));
        Assert.True (view.Border.Settings.HasFlag (BorderSettings.Title));

        // TabLength auto-computed from title: "Hello" = 5 columns + 2 = 7
        Assert.Equal (7, view.Border.TabLength!.Value);

        view.Dispose ();
    }
}