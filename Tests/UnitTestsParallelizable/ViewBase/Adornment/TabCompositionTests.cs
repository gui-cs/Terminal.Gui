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

    private static View CreateTabView (IDriver driver, Side side, int tabOffset, bool hasFocus, string title)
    {
        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = hasFocus,
            SuperViewRendersLineCanvas = true,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Rounded,
            Title = title,
            Text = $"{title} content",
            Arrangement = ViewArrangement.Overlapped
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
        DriverAssert.AssertDriverContentsAre (expected, output, driver);
        view.Dispose ();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Top — Two tabs, Tab1 focused
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Top_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (40, 5);

        // Tab1: focused, offset 0. Title "Tab1" → TabLength = 6.
        View tab1 = CreateTabView (driver, Side.Top, 0, false, "Tab1");

        // Tab2: unfocused, offset 6 (right after Tab1). Title "Tab2" → TabLength = 6.
        View tab2 = CreateTabView (driver, Side.Top, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();
        Assert.Equal (tab1, superView.SubViews.ElementAt (1));

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────╮────╮
                       │Tab1│Tab2│
                       │    ╰────┴──╮
                       │Tab1 content│
                       ╰────────────╯
                       """);
    }

    [Fact]
    public void Top_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (40, 5);

        // Tab1: focused, offset 0. Title "Tab1" → TabLength = 6.
        View tab1 = CreateTabView (driver, Side.Top, 0, false, "Tab1");
        tab1.Arrangement = ViewArrangement.Overlapped;

        // Tab2: unfocused, offset 6 (right after Tab1). Title "Tab2" → TabLength = 6.
        View tab2 = CreateTabView (driver, Side.Top, 5, false, "Tab2");
        tab2.Arrangement = ViewArrangement.Overlapped;

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────╭────╮
                       │Tab1│Tab2│
                       ├────╯    ╰──╮
                       │Tab2 content│
                       ╰────────────╯
                       """);
    }

    [Fact]
    public void Top_ThreeTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (40, 5);

        View tab1 = CreateTabView (driver, Side.Top, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Top, 5, true, "Tab2");
        View tab3 = CreateTabView (driver, Side.Top, 10, false, "Tab3");

        tab1.Width = tab2.Width = tab3.Width = 20;
        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2, tab3);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────╭────╮────╮
                       │Tab1│Tab2│Tab3│
                       ├────╯    ╰────┴───╮
                       │Tab2 content      │
                       ╰──────────────────╯
                       """);
    }
}
