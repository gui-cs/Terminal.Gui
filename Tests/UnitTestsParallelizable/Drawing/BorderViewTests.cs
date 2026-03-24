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

    private static View CreateTabView (IDriver driver,
                                       int width,
                                       int height,
                                       Side side,
                                       int tabOffset,
                                       int? tabLength,
                                       bool hasFocus,
                                       string? title,
                                       bool titleFlag,
                                       Thickness? thickness = null)
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

        if (title is { })
        {
            view.Title = title;
        }

        view.Border.Thickness = thickness
                                ?? side switch
                                   {
                                       Side.Top => new Thickness (1, 3, 1, 1),
                                       Side.Bottom => new Thickness (1, 1, 1, 3),
                                       Side.Left => new Thickness (3, 1, 1, 1),
                                       Side.Right => new Thickness (1, 1, 3, 1),
                                       _ => throw new ArgumentOutOfRangeException (nameof (side))
                                   };

        var settings = BorderSettings.Tab;

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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   0,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        Assert.Equal (5, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   2,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   5,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   2,
                                   null,
                                   true,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   5,
                                   null,
                                   true,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   0,
                                   null,
                                   false,
                                   null,
                                   false);

        Assert.Equal (2, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   2,
                                   null,
                                   false,
                                   "X",
                                   true);

        Assert.Equal (3, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   -1,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ───╮
                       Tab│
                       ╭──┴────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset_WithTitle_Thick_Border ()
    {
        IDriver driver = CreateTestDriver ();

        View view = CreateTabView (driver,
                                   17,
                                   12,
                                   Side.Top,
                                   -1,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        view.Border.Thickness = new Thickness (5, 5, 5, 5);

        DrawAndAssert (view,
                       driver,
                       """
                       ───╮
                       Tab│
                       ╭──┴────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset2_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   -2,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        // Header at X=-2. Left edge and 'T' clipped. Visible: cap ──╮, title ab│.
        DrawAndAssert (view,
                       driver,
                       """
                       ──╮
                       ab│
                       ╭─┴─────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset4_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   -4,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        // Header at X=-4. Only right edge visible at col 0. No title visible.
        DrawAndAssert (view,
                       driver,
                       """
                       ╮
                       │
                       ├───────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset5_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   -5,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        // Header completely off-screen. Content border drawn normally.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   0,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        Assert.Equal (5, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   2,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   -1,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   0,
                                   null,
                                   false,
                                   null,
                                   false);

        Assert.Equal (2, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        Assert.Equal (5, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   false,
                                   null,
                                   false);

        Assert.Equal (2, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Left,
                                   2,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Left,
                                   6,
                                   null,
                                   true,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        Assert.Equal (5, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   false,
                                   null,
                                   false);

        Assert.Equal (2, view.Border.TabLength!.Value);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Right,
                                   2,
                                   null,
                                   false,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Right,
                                   6,
                                   null,
                                   true,
                                   "Tab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
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

    // ════════════════════════════════════════════════════════════════════
    //  Thickness Variants — Depth capping and non-standard depths
    //  Depth should be min (thickness_on_tab_side, 3).
    //  Thickness >= 3 always uses depth 3.
    //  Thickness 1 and 2 produce shallower tabs per spec.
    // ════════════════════════════════════════════════════════════════════

    // ──── Thickness = 4 (depth capped to 3) ────
    // Expected output identical to thickness=3 (extra blank row trimmed).

    [Fact]
    public void Top_Focused_Depth4_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 7);

        View view = CreateTabView (driver,
                                   9,
                                   7,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 4, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │Tab│
                       │   ╰───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Bottom_Focused_Depth4_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 7);

        View view = CreateTabView (driver,
                                   9,
                                   7,
                                   Side.Bottom,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 1, 1, 4));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │   ╭───╯
                       │Tab│
                       ╰───╯
                       """);
    }

    [Fact]
    public void Left_Focused_Depth4_WithTitle ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = CreateTabView (driver,
                                   12,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (4, 1, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭─ ───────╮
                        │T        │
                        │a        │
                        │b        │
                        ╰─╮       │
                          │       │
                          │       │
                          │       │
                          ╰───────╯
                       """);
    }

    [Fact]
    public void Right_Focused_Depth4_WithTitle ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = CreateTabView (driver,
                                   12,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 1, 4, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭─────── ─╮
                       │        T│
                       │        a│
                       │        b│
                       │       ╭─╯
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    // ──── Thickness = 2 (depth 2) ────
    // 2-row tab: cap line + title on closing edge. No bottom line.

    [Fact]
    public void Top_Focused_Depth2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 5);

        View view = CreateTabView (driver,
                                   9,
                                   5,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 2, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │Tab╰───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Bottom_Focused_Depth2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 5);

        View view = CreateTabView (driver,
                                   9,
                                   5,
                                   Side.Bottom,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 1, 1, 2));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │Tab╭───╯
                       ╰───╯
                       """);
    }

    [Fact]
    public void Left_Focused_Depth2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (10, 9);

        View view = CreateTabView (driver,
                                   10,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (2, 1, 1, 1));

        // Depth=2: cap at col 0, closing edge at col 1. Title on closing edge.
        // (1,0) excluded by AddTabSideContentBorder → space at col 1 row 0.
        // (1,4) has ╮ from header bottom edge + content border vertical auto-join.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭ ───────╮
                       │T       │
                       │a       │
                       │b       │
                       ╰╮       │
                        │       │
                        │       │
                        │       │
                        ╰───────╯
                       """);
    }

    [Fact]
    public void Right_Focused_Depth2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (10, 9);

        View view = CreateTabView (driver,
                                   10,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 1, 2, 1));

        // Depth=2: cap at col 9, closing edge at col 8. Title on closing edge.
        // (8,0) excluded → space at col 8 row 0. (8,4) has ╭ from auto-join.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭─────── ╮
                       │       T│
                       │       a│
                       │       b│
                       │       ╭╯
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    // ──── Thickness = 1 (depth 1) ────
    // 1-row tab: title inline on the content border line.

    [Fact]
    public void Top_Focused_Depth1_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 4);

        View view = CreateTabView (driver,
                                   9,
                                   4,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       │Tab╰───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Bottom_Focused_Depth1_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 4);

        View view = CreateTabView (driver,
                                   9,
                                   4,
                                   Side.Bottom,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │Tab╭───╯
                       """);
    }

    [Fact]
    public void Left_Focused_Depth1_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 9);

        View view = CreateTabView (driver,
                                   9,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        // Depth=1: no cap line, no tab edges. Title at rows 1-3 (between top/bottom edges).
        // (0,0) is excluded by AddTabSideContentBorder (tab starts at content border edge).
        // After Trim(), leading space removed from row 0.
        DrawAndAssert (view,
                       driver,
                       """
                       ───────╮
                       T       │
                       a       │
                       b       │
                       │       │
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Right_Focused_Depth1_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 9);

        View view = CreateTabView (driver,
                                   9,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   true,
                                   "Tab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        // Depth=1: no cap line, no tab edges. Title at rows 1-3.
        // (8,0) excluded by AddTabSideContentBorder → trailing space stripped.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────
                       │       T
                       │       a
                       │       b
                       │       │
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }
}
