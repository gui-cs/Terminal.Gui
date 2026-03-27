using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests that multiple Views with tab-style borders compose correctly when sharing
///     a parent's LineCanvas via <see cref="View.SuperViewRendersLineCanvas"/>.
///     Covers all four sides (Top, Bottom, Left, Right) and tab thicknesses 1, 2, and 3.
/// </summary>

// Copilot
public class TabCompositionTests (ITestOutputHelper output) : TestDriverBase
{
    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    private static View CreateTabView (IDriver driver, Side side, int thickness, string title, int tabOffset)
    {
        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            SuperViewRendersLineCanvas = true,
            BorderStyle = LineStyle.Rounded,
            Title = title,
            Text = $"{title} content",
            Arrangement = ViewArrangement.Overlapped
        };

        view.Border.Thickness = side switch
                                {
                                    Side.Top => new Thickness (1, thickness, 1, 1),
                                    Side.Bottom => new Thickness (1, 1, 1, thickness),
                                    Side.Left => new Thickness (thickness, 1, 1, 1),
                                    Side.Right => new Thickness (1, 1, thickness, 1),
                                    _ => throw new ArgumentOutOfRangeException (nameof (side))
                                };

        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = side;
        view.Border.TabOffset = tabOffset;

        // Fix the dimension perpendicular to the tab edge so that all tab views
        // share the same extent and composition tests produce stable output.
        if (side is Side.Top or Side.Bottom)
        {
            view.Width = 12;
            view.Height = Dim.Auto ();
        }
        else
        {
            view.Width = Dim.Auto ();
            view.Height = 6;
        }

        return view;
    }

    private void DrawAndAssert (View view, IDriver driver, string expected)
    {
        view.Layout ();
        view.Draw ();
        DriverAssert.AssertDriverContentsAre (expected, output, driver);
        view.Dispose ();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Side.Top — composition (kept from original PR tests)
    // ═════════════════════════════════════════════════════════════════════

    [Fact]
    public void Top_Thickness3_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Top, 3, "A", 0);
        View tab2 = CreateTabView (driver, Side.Top, 3, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─╮ ╭─╮
                       │A│ │B│
                       │ ╰─┴─┴────╮
                       │A content │
                       ╰──────────╯
                       """);
    }

    [Fact]
    public void Top_Thickness3_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Top, 3, "A", 0);
        View tab2 = CreateTabView (driver, Side.Top, 3, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─╮ ╭─╮
                       │A│ │B│
                       ├─┴─╯ ╰────╮
                       │B content │
                       ╰──────────╯
                       """);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Side.Top — thickness 1
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Top_Thickness1_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Top, 1, "A", 0);
        View tab2 = CreateTabView (driver, Side.Top, 1, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       │A╭─┬─┬────╮
                       │A content │
                       ╰──────────╯
                       """);
    }

    [Fact]
    public void Top_Thickness1_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Top, 1, "A", 0);
        View tab2 = CreateTabView (driver, Side.Top, 1, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─┬─╮B╭────╮
                       │B content │
                       ╰──────────╯
                       """);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Side.Top — thickness 2
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Top_Thickness2_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Top, 2, "A", 0);
        View tab2 = CreateTabView (driver, Side.Top, 2, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─╮ ╭─╮
                       │A╰─┴─┴────╮
                       │A content │
                       ╰──────────╯
                       """);
    }

    [Fact]
    public void Top_Thickness2_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Top, 2, "A", 0);
        View tab2 = CreateTabView (driver, Side.Top, 2, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─╮ ╭─╮
                       ├─┴─╯B╰────╮
                       │B content │
                       ╰──────────╯
                       """);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Side.Bottom — thickness 1, 2, 3
    // ═════════════════════════════════════════════════════════════════════

    [Fact]
    public void Bottom_Thickness1_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Bottom, 1, "A", 0);
        View tab2 = CreateTabView (driver, Side.Bottom, 1, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────╮
                       │A content │
                       │A╭─┬─┬────╯
                       """);
    }

    [Fact]
    public void Bottom_Thickness1_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Bottom, 1, "A", 0);
        View tab2 = CreateTabView (driver, Side.Bottom, 1, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────╮
                       │B content │
                       ├─┬─╮B╭────╯
                       """);
    }

    [Fact]
    public void Bottom_Thickness2_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Bottom, 2, "A", 0);
        View tab2 = CreateTabView (driver, Side.Bottom, 2, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────╮
                       │A content │
                       │A╭─┬─┬────╯
                       ╰─╯ ╰─╯
                       """);
    }

    [Fact]
    public void Bottom_Thickness2_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Bottom, 2, "A", 0);
        View tab2 = CreateTabView (driver, Side.Bottom, 2, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────╮
                       │B content │
                       ├─┬─╮B╭────╯
                       ╰─╯ ╰─╯
                       """);
    }

    [Fact]
    public void Bottom_Thickness3_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Bottom, 3, "A", 0);
        View tab2 = CreateTabView (driver, Side.Bottom, 3, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────╮
                       │A content │
                       │A╭─┬─┬────╯
                       │ │ │B│
                       ╰─╯ ╰─╯
                       """);
    }

    [Fact]
    public void Bottom_Thickness3_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Bottom, 3, "A", 0);
        View tab2 = CreateTabView (driver, Side.Bottom, 3, "B", 4);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────╮
                       │B content │
                       ├─┬─╮B╭────╯
                       │A│ │ │
                       ╰─╯ ╰─╯
                       """);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Side.Left — thickness 1, 2, 3
    // ═════════════════════════════════════════════════════════════════════

    [Fact]
    public void Left_Thickness1_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Left, 1, "A", 0);
        View tab2 = CreateTabView (driver, Side.Left, 1, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ──────────╮
                       AA content│
                       │         │
                       │         │
                       │         │
                       ╰─────────╯
                       """);
    }

    [Fact]
    public void Left_Thickness1_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Left, 1, "A", 0);
        View tab2 = CreateTabView (driver, Side.Left, 1, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─────────╮
                       │B content│
                       │         │
                       │         │
                       B         │
                       ──────────╯
                       """);
    }

    [Fact]
    public void Left_Thickness2_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Left, 2, "A", 0);
        View tab2 = CreateTabView (driver, Side.Left, 2, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────╮
                       │AA content│
                       ╰╮         │
                       ╭┤         │
                       ││         │
                       ╰┴─────────╯
                       """);
    }

    [Fact]
    public void Left_Thickness2_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Left, 2, "A", 0);
        View tab2 = CreateTabView (driver, Side.Left, 2, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭┬─────────╮
                       ││B content│
                       ╰┤         │
                       ╭╯         │
                       │B         │
                       ╰──────────╯
                       """);
    }

    [Fact]
    public void Left_Thickness3_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Left, 3, "A", 0);
        View tab2 = CreateTabView (driver, Side.Left, 3, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭───────────╮
                       │A A content│
                       ╰─╮         │
                       ╭─┤         │
                       │B│         │
                       ╰─┴─────────╯
                       """);
    }

    [Fact]
    public void Left_Thickness3_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Left, 3, "A", 0);
        View tab2 = CreateTabView (driver, Side.Left, 3, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─┬─────────╮
                       │A│B content│
                       ╰─┤         │
                       ╭─╯         │
                       │B          │
                       ╰───────────╯
                       """);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Side.Right — thickness 1, 2, 3
    // ═════════════════════════════════════════════════════════════════════

    [Fact]
    public void Right_Thickness1_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Right, 1, "A", 0);
        View tab2 = CreateTabView (driver, Side.Right, 1, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────
                       │A contentA
                       │         │
                       │         │
                       │         │
                       ╰─────────╯
                       """);
    }

    [Fact]
    public void Right_Thickness1_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Right, 1, "A", 0);
        View tab2 = CreateTabView (driver, Side.Right, 1, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─────────╮
                       │B content│
                       │         │
                       │         │
                       │         B
                       ╰──────────
                       """);
    }

    [Fact]
    public void Right_Thickness2_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Right, 2, "A", 0);
        View tab2 = CreateTabView (driver, Side.Right, 2, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────╮
                       │A contentA│
                       │         ╭╯
                       │         ├╮
                       │         ││
                       ╰─────────┴╯
                       """);
    }

    [Fact]
    public void Right_Thickness2_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Right, 2, "A", 0);
        View tab2 = CreateTabView (driver, Side.Right, 2, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─────────┬╮
                       │B content││
                       │         ├╯
                       │         ╰╮
                       │         B│
                       ╰──────────╯
                       """);
    }

    [Fact]
    public void Right_Thickness3_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Right, 3, "A", 0);
        View tab2 = CreateTabView (driver, Side.Right, 3, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭───────────╮
                       │A contentA │
                       │         ╭─╯
                       │         ├─╮
                       │         │B│
                       ╰─────────┴─╯
                       """);
    }

    [Fact]
    public void Right_Thickness3_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (25, 12);

        View tab1 = CreateTabView (driver, Side.Right, 3, "A", 0);
        View tab2 = CreateTabView (driver, Side.Right, 3, "B", 3);

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─────────┬─╮
                       │B content│A│
                       │         ├─╯
                       │         ╰─╮
                       │         B │
                       ╰───────────╯
                       """);
    }
}
