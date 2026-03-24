using UnitTests;

namespace DrawingTests;

/// <summary>
///     Visual tests for <see cref="BorderView"/> tab header rendering via <see cref="Border"/> properties.
///     These replicate <see cref="TabHeaderRendererTests"/> scenarios at the higher BorderView level,
///     covering all four <see cref="Side"/> values and important variations of offset and Title.
/// </summary>

// Copilot
public class BorderViewTests (ITestOutputHelper output) : TestDriverBase
{
    // ────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────

    private static View CreateTabView (
        IDriver driver,
        int width,
        int height,
        Side side,
        int tabOffset,
        int? tabLength,
        bool hasFocus,
        string? title,
        bool titleFlag)
    {
        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = hasFocus,
            Width = width,
            Height = height,
            BorderStyle = LineStyle.Rounded
        };

        if (title is not null)
        {
            view.Title = title;
        }

        view.Border.Thickness = side switch
        {
            Side.Top => new Thickness (1, 3, 1, 1),
            Side.Bottom => new Thickness (1, 1, 1, 3),
            Side.Left => new Thickness (3, 1, 1, 1),
            Side.Right => new Thickness (1, 1, 3, 1),
            _ => throw new ArgumentOutOfRangeException (nameof (side))
        };

        BorderSettings settings = BorderSettings.Tab;

        if (titleFlag)
        {
            settings |= BorderSettings.Title;
        }

        view.Border.Settings = settings;
        view.Border.TabSide = side;
        view.Border.TabOffset = tabOffset;

        if (tabLength.HasValue)
        {
            view.Border.TabLength = tabLength.Value;
        }

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
    //  Side.Top — View 9×6, Thickness(1,3,1,1), borderBounds=(0,2,9,4)
    //  Content border: 9 wide, 4 tall. Interior: 7 cols × 2 rows.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Top_Unfocused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: 0, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        Assert.Equal (5, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                     ╭───╮
                                     │Tab│
                                     ├───┴───╮
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Top_Unfocused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: 2, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                       ╭───╮
                                       │Tab│
                                     ╭─┴───┴─╮
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Top_Unfocused_OverflowRight_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: 5, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                          ╭───
                                          │Tab
                                     ╭────┴──╮
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Top_Focused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: 0, tabLength: null,
                                   hasFocus: true, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                     ╭───╮
                                     │Tab│
                                     │   ╰───╮
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Top_Focused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: 2, tabLength: null,
                                   hasFocus: true, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                       ╭───╮
                                       │Tab│
                                     ╭─╯   ╰─╮
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Top_Focused_OverflowRight_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: 5, tabLength: null,
                                   hasFocus: true, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                          ╭───
                                          │Tab
                                     ╭────╯  │
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Top_Unfocused_Offset0_NoTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: 0, tabLength: null,
                                   hasFocus: false, title: null, titleFlag: false);

        Assert.Equal (2, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                     ╭╮
                                     ││
                                     ├┴──────╮
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Top_Unfocused_SingleCharTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: 2, tabLength: null,
                                   hasFocus: false, title: "X", titleFlag: true);

        Assert.Equal (3, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                       ╭─╮
                                       │X│
                                     ╭─┴─┴───╮
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Top, tabOffset: -1, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                     ───╮
                                     Tab│
                                     ╭──┴────╮
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Bottom — View 9×6, Thickness(1,1,1,3), borderBounds=(0,0,9,4)
    //  Content border: 9 wide, 4 tall. Tab protrudes below.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Bottom_Unfocused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Bottom, tabOffset: 0, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        Assert.Equal (5, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                     ╭───────╮
                                     │       │
                                     │       │
                                     ├───┬───╯
                                     │Tab│
                                     ╰───╯
                                     """);
    }

    [Fact]
    public void Bottom_Focused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Bottom, tabOffset: 0, tabLength: null,
                                   hasFocus: true, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                     ╭───────╮
                                     │       │
                                     │       │
                                     │   ╭───╯
                                     │Tab│
                                     ╰───╯
                                     """);
    }

    [Fact]
    public void Bottom_Unfocused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Bottom, tabOffset: 2, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                     ╭───────╮
                                     │       │
                                     │       │
                                     ╰─┬───┬─╯
                                       │Tab│
                                       ╰───╯
                                     """);
    }

    [Fact]
    public void Bottom_Unfocused_NegativeOffset_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Bottom, tabOffset: -1, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                     ╭───────╮
                                     │       │
                                     │       │
                                     ╰──┬────╯
                                     Tab│
                                     ───╯
                                     """);
    }

    [Fact]
    public void Bottom_Unfocused_Offset0_NoTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);
        View view = CreateTabView (driver, 9, 6, Side.Bottom, tabOffset: 0, tabLength: null,
                                   hasFocus: false, title: null, titleFlag: false);

        Assert.Equal (2, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                     ╭───────╮
                                     │       │
                                     │       │
                                     ├┬──────╯
                                     ││
                                     ╰╯
                                     """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Left — View 11×9, Thickness(3,1,1,1), borderBounds=(2,0,9,9)
    //  Content border: 9 wide (cols 2–10), 9 tall. Tab protrudes left.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Left_Unfocused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);
        View view = CreateTabView (driver, 11, 9, Side.Left, tabOffset: 0, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        Assert.Equal (5, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                     ╭─┬───────╮
                                     │T│       │
                                     │a│       │
                                     │b│       │
                                     ╰─┤       │
                                       │       │
                                       │       │
                                       │       │
                                       ╰───────╯
                                     """);
    }

    [Fact]
    public void Left_Unfocused_Offset0_NoTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);
        View view = CreateTabView (driver, 11, 9, Side.Left, tabOffset: 0, tabLength: null,
                                   hasFocus: false, title: null, titleFlag: false);

        Assert.Equal (2, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                     ╭─┬───────╮
                                     ╰─┤       │
                                       │       │
                                       │       │
                                       │       │
                                       │       │
                                       │       │
                                       │       │
                                       ╰───────╯
                                     """);
    }

    [Fact]
    public void Left_Unfocused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);
        View view = CreateTabView (driver, 11, 9, Side.Left, tabOffset: 2, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                       ╭───────╮
                                       │       │
                                     ╭─┤       │
                                     │T│       │
                                     │a│       │
                                     │b│       │
                                     ╰─┤       │
                                       │       │
                                       ╰───────╯
                                     """);
    }

    [Fact]
    public void Left_Focused_Overflow_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);
        View view = CreateTabView (driver, 11, 9, Side.Left, tabOffset: 6, tabLength: null,
                                   hasFocus: true, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                       ╭───────╮
                                       │       │
                                       │       │
                                       │       │
                                       │       │
                                       │       │
                                     ╭─╯       │
                                     │T        │
                                     │a ───────╯
                                     """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Right — View 11×9, Thickness(1,1,3,1), borderBounds=(0,0,9,9)
    //  Content border: 9 wide (cols 0–8), 9 tall. Tab protrudes right.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Right_Unfocused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);
        View view = CreateTabView (driver, 11, 9, Side.Right, tabOffset: 0, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        Assert.Equal (5, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                     ╭───────┬─╮
                                     │       │T│
                                     │       │a│
                                     │       │b│
                                     │       ├─╯
                                     │       │
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Right_Unfocused_Offset0_NoTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);
        View view = CreateTabView (driver, 11, 9, Side.Right, tabOffset: 0, tabLength: null,
                                   hasFocus: false, title: null, titleFlag: false);

        Assert.Equal (2, view.Border.TabLength!.Value);

        DrawAndAssert (view, driver, """
                                     ╭───────┬─╮
                                     │       ├─╯
                                     │       │
                                     │       │
                                     │       │
                                     │       │
                                     │       │
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Right_Unfocused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);
        View view = CreateTabView (driver, 11, 9, Side.Right, tabOffset: 2, tabLength: null,
                                   hasFocus: false, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                     ╭───────╮
                                     │       │
                                     │       ├─╮
                                     │       │T│
                                     │       │a│
                                     │       │b│
                                     │       ├─╯
                                     │       │
                                     ╰───────╯
                                     """);
    }

    [Fact]
    public void Right_Focused_Overflow_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);
        View view = CreateTabView (driver, 11, 9, Side.Right, tabOffset: 6, tabLength: null,
                                   hasFocus: true, title: "Tab", titleFlag: true);

        DrawAndAssert (view, driver, """
                                     ╭───────╮
                                     │       │
                                     │       │
                                     │       │
                                     │       │
                                     │       │
                                     │       ╰─╮
                                     │        T│
                                     ╰─────── a│
                                     """);
    }
}
