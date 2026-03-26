using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests that multiple Views with tab-style borders compose correctly when sharing
///     a parent's LineCanvas via <see cref="View.SuperViewRendersLineCanvas"/>.
/// </summary>

// Copilot
public class TabCompositionTests (ITestOutputHelper output) : TestDriverBase
{
    // ────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────

    private static View CreateTabView (IDriver driver,
                                       int width,
                                       int height,
                                       Side side,
                                       int tabOffset,
                                       bool hasFocus,
                                       string title)
    {
        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = hasFocus,
            SuperViewRendersLineCanvas = true,
            Width = width,
            Height = height,
            BorderStyle = LineStyle.Rounded,
            Title = title
        };

        view.Border.Thickness = side switch
        {
            Side.Top => new Thickness (1, 3, 1, 1),
            Side.Bottom => new Thickness (1, 1, 1, 3),
            Side.Left => new Thickness (3, 1, 1, 1),
            Side.Right => new Thickness (1, 1, 3, 1),
            _ => throw new ArgumentOutOfRangeException (nameof (side))
        };

        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = side;
        view.Border.TabOffset = tabOffset;

        return view;
    }

    private void DrawAndAssert (View view, IDriver driver, string expected)
    {
        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());
        DriverAssert.AssertDriverContentsAre (expected, output, driver);
        view.Dispose ();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Top — Two tabs, Tab1 focused
    // ════════════════════════════════════════════════════════════════════

    [Fact (Skip = "Border needs refactoring")]
    public void Top_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (19, 6);

        // Tab1: focused, offset 0. Title "Tab1" → TabLength = 6.
        View tab1 = CreateTabView (driver, 19, 6, Side.Top, 0, true, "Tab1");

        // Tab2: unfocused, offset 6 (right after Tab1). Title "Tab2" → TabLength = 6.
        View tab2 = CreateTabView (driver, 19, 6, Side.Top, 5, false, "Tab2");

        View container = new ()
        {
            Driver = driver,
            Width = 19,
            Height = 6
        };
        container.Add (tab1, tab2);

        DrawAndAssert (container,
                       driver,
                       """
                       ╭────┬────╮
                       │Tab1│Tab2│
                       │    ╰────┴───────╮
                       │                 │
                       │                 │
                       ╰─────────────────╯
                       """);
    }

    [Fact (Skip = "Border needs refactoring")]
    public void Top_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (19, 6);

        // Tab1: unfocused, offset 0.
        View tab1 = CreateTabView (driver, 19, 6, Side.Top, 0, false, "Tab1");

        // Tab2: focused, offset 6.
        View tab2 = CreateTabView (driver, 19, 6, Side.Top, 6, true, "Tab2");

        View container = new ()
        {
            Driver = driver,
            Width = 19,
            Height = 6
        };
        container.Add (tab1, tab2);

        DrawAndAssert (container,
                       driver,
                       """
                       ╭────┬────╮
                       │Tab1│Tab2│
                       ├────╯    ╰───────╮
                       │                 │
                       │                 │
                       ╰─────────────────╯
                       """);
    }

    [Fact (Skip = "Border needs refactoring")]
    public void Top_ThreeTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (19, 6);

        View tab1 = CreateTabView (driver, 19, 6, Side.Top, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, 19, 6, Side.Top, 6, true, "Tab2");
        View tab3 = CreateTabView (driver, 19, 6, Side.Top, 12, false, "Tab3");

        View container = new ()
        {
            Driver = driver,
            Width = 19,
            Height = 6
        };
        container.Add (tab1, tab2, tab3);

        DrawAndAssert (container,
                       driver,
                       """
                       ╭────┬────┬────╮
                       │Tab1│Tab2│Tab3│
                       ├────╯    ╰────┴──╮
                       │                 │
                       │                 │
                       ╰─────────────────╯
                       """);
    }
}
