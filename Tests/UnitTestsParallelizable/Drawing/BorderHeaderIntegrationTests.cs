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
}
