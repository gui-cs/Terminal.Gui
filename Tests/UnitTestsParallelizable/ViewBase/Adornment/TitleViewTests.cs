// Claude - Opus 4.6

using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests for <see cref="TitleView"/> orientation, direction, key bindings, and TextFormatter behavior.
/// </summary>
[Trait ("Category", "Adornment")]
public class TitleViewTests (ITestOutputHelper output) : TestDriverBase
{
    #region Constructor Defaults

    [Fact]
    public void Constructor_SetsExpectedDefaults ()
    {
        TitleView tv = new ();

        Assert.True (tv.CanFocus);
        Assert.Equal (TabBehavior.NoStop, tv.TabStop);
        Assert.True (tv.SuperViewRendersLineCanvas);
        Assert.Equal (Orientation.Horizontal, tv.Orientation);

        tv.Dispose ();
    }

    [Fact]
    public void Constructor_DefaultOrientation_IsHorizontal ()
    {
        TitleView tv = new ();

        Assert.Equal (Orientation.Horizontal, tv.Orientation);
        Assert.Equal (TextDirection.LeftRight_TopBottom, tv.TextFormatter.Direction);

        tv.Dispose ();
    }

    #endregion

    #region Orientation and TextFormatter.Direction

    [Fact]
    public void Orientation_Horizontal_SetsTextDirectionLeftRight ()
    {
        TitleView tv = new () { Orientation = Orientation.Horizontal };

        Assert.Equal (TextDirection.LeftRight_TopBottom, tv.TextFormatter.Direction);

        tv.Dispose ();
    }

    [Fact]
    public void Orientation_Vertical_SetsTextDirectionTopBottom ()
    {
        TitleView tv = new () { Orientation = Orientation.Vertical };

        Assert.Equal (TextDirection.TopBottom_LeftRight, tv.TextFormatter.Direction);

        tv.Dispose ();
    }

    [Fact]
    public void ChangingOrientation_UpdatesTextDirection ()
    {
        TitleView tv = new ();

        Assert.Equal (TextDirection.LeftRight_TopBottom, tv.TextFormatter.Direction);

        tv.Orientation = Orientation.Vertical;
        Assert.Equal (TextDirection.TopBottom_LeftRight, tv.TextFormatter.Direction);

        tv.Orientation = Orientation.Horizontal;
        Assert.Equal (TextDirection.LeftRight_TopBottom, tv.TextFormatter.Direction);

        tv.Dispose ();
    }

    #endregion

    #region KeyBindings — Directional Commands

    [Fact]
    public void Horizontal_BindsLeftRight_ToDirectionalCommands ()
    {
        TitleView tv = new () { Orientation = Orientation.Horizontal };

        AssertKeyBoundToCommand (tv, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (tv, Key.CursorRight, Command.Right);

        tv.Dispose ();
    }

    [Fact]
    public void Horizontal_DoesNotBindUpDown ()
    {
        TitleView tv = new () { Orientation = Orientation.Horizontal };

        Assert.False (tv.KeyBindings.TryGet (Key.CursorUp, out _));
        Assert.False (tv.KeyBindings.TryGet (Key.CursorDown, out _));

        tv.Dispose ();
    }

    [Fact]
    public void Vertical_BindsUpDown_ToDirectionalCommands ()
    {
        TitleView tv = new () { Orientation = Orientation.Vertical };

        AssertKeyBoundToCommand (tv, Key.CursorUp, Command.Up);
        AssertKeyBoundToCommand (tv, Key.CursorDown, Command.Down);

        tv.Dispose ();
    }

    [Fact]
    public void Vertical_DoesNotBindLeftRight ()
    {
        TitleView tv = new () { Orientation = Orientation.Vertical };

        Assert.False (tv.KeyBindings.TryGet (Key.CursorLeft, out _));
        Assert.False (tv.KeyBindings.TryGet (Key.CursorRight, out _));

        tv.Dispose ();
    }

    [Fact]
    public void ChangingOrientation_RebindsKeys ()
    {
        TitleView tv = new () { Orientation = Orientation.Horizontal };

        // Initially: Left/Right bound
        AssertKeyBoundToCommand (tv, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (tv, Key.CursorRight, Command.Right);
        Assert.False (tv.KeyBindings.TryGet (Key.CursorUp, out _));

        // Change to Vertical: Up/Down bound, Left/Right removed
        tv.Orientation = Orientation.Vertical;

        AssertKeyBoundToCommand (tv, Key.CursorUp, Command.Up);
        AssertKeyBoundToCommand (tv, Key.CursorDown, Command.Down);
        Assert.False (tv.KeyBindings.TryGet (Key.CursorLeft, out _));
        Assert.False (tv.KeyBindings.TryGet (Key.CursorRight, out _));

        tv.Dispose ();
    }

    #endregion

    #region KeyBindings — Enter Removed

    [Fact]
    public void Enter_IsNotBound ()
    {
        TitleView tv = new ();

        IEnumerable<KeyValuePair<Key, KeyBinding>> bindings = tv.KeyBindings.GetBindings ();
        bool hasEnter = bindings.Any (kvp => kvp.Key.KeyCode == KeyCode.Enter);

        Assert.False (hasEnter, "TitleView should not have Enter bound");

        tv.Dispose ();
    }

    #endregion

    #region Direction Property

    [Fact]
    public void Direction_CanBeSet ()
    {
        TitleView tv = new ();

        tv.Direction = NavigationDirection.Backward;
        Assert.Equal (NavigationDirection.Backward, tv.Direction);

        tv.Direction = NavigationDirection.Forward;
        Assert.Equal (NavigationDirection.Forward, tv.Direction);

        tv.Dispose ();
    }

    #endregion

    #region IOrientation Interface

    [Fact]
    public void ImplementsIOrientation ()
    {
        TitleView tv = new ();

        Assert.IsAssignableFrom<IOrientation> (tv);

        tv.Dispose ();
    }

    #endregion

    #region Helpers

    private static void AssertKeyBoundToCommand (TitleView tv, Key key, Command expectedCommand)
    {
        Assert.True (tv.KeyBindings.TryGet (key, out KeyBinding binding), $"Expected key {key} to be bound");
        Assert.Contains (expectedCommand, binding.Commands);
    }

    #endregion

    #region ITitleView — TabDepth Computed Property

    [Fact]
    public void TabDepth_Top_ReturnsTopThickness ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 5, 1, 1) };

        Assert.Equal (5, tv.TabDepth);

        tv.Dispose ();
    }

    [Fact]
    public void TabDepth_Left_ReturnsLeftThickness ()
    {
        TitleView tv = new () { TabSide = Side.Left, BorderThickness = new Thickness (4, 1, 1, 1) };

        Assert.Equal (4, tv.TabDepth);

        tv.Dispose ();
    }

    [Fact]
    public void TabDepth_Bottom_ReturnsBottomThickness ()
    {
        TitleView tv = new () { TabSide = Side.Bottom, BorderThickness = new Thickness (1, 1, 1, 7) };

        Assert.Equal (7, tv.TabDepth);

        tv.Dispose ();
    }

    [Fact]
    public void TabDepth_Right_ReturnsRightThickness ()
    {
        TitleView tv = new () { TabSide = Side.Right, BorderThickness = new Thickness (1, 1, 6, 1) };

        Assert.Equal (6, tv.TabDepth);

        tv.Dispose ();
    }

    [Fact]
    public void TabSide_Change_UpdatesTabDepth ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (2, 5, 3, 4) };

        Assert.Equal (5, tv.TabDepth);

        tv.TabSide = Side.Bottom;
        Assert.Equal (4, tv.TabDepth);

        tv.TabSide = Side.Left;
        Assert.Equal (2, tv.TabDepth);

        tv.TabSide = Side.Right;
        Assert.Equal (3, tv.TabDepth);

        tv.Dispose ();
    }

    [Fact]
    public void BorderThickness_Change_UpdatesTabDepth ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        Assert.Equal (3, tv.TabDepth);

        tv.BorderThickness = new Thickness (1, 7, 1, 1);
        Assert.Equal (7, tv.TabDepth);

        tv.Dispose ();
    }

    [Fact]
    public void Implements_ITitleView ()
    {
        TitleView tv = new ();

        Assert.IsAssignableFrom<ITitleView> (tv);

        tv.Dispose ();
    }

    #endregion

    #region ITitleView — UpdateLayout

    [Fact]
    public void UpdateLayout_HidesTitleView_WhenBorderBoundsEmpty ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = Rectangle.Empty,
            TabOffset = 0,
            TabLength = 6,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        Assert.False (tv.Visible);

        tv.Dispose ();
    }

    [Fact]
    public void UpdateLayout_DoesNotSetVisible_WhenTabLengthNull ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        // Pre-set Visible to false so we can verify UpdateLayout doesn't change it
        tv.Visible = false;

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = null,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        // TabLength null → early return, Visible unchanged
        Assert.False (tv.Visible);

        tv.Dispose ();
    }

    [Fact]
    public void UpdateLayout_SetsTextFromContext ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = 6,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "MyTab",
            ScreenOrigin = Point.Empty
        });

        Assert.Equal ("MyTab", tv.Text);

        tv.Dispose ();
    }

    [Fact]
    public void UpdateLayout_SetsOrientation_Horizontal_ForTopSide ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = 6,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        Assert.Equal (Orientation.Horizontal, tv.Orientation);

        tv.Dispose ();
    }

    [Fact]
    public void UpdateLayout_SetsOrientation_Vertical_ForLeftSide ()
    {
        TitleView tv = new () { TabSide = Side.Left, BorderThickness = new Thickness (3, 1, 1, 1) };

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (2, 0, 5, 10),
            TabOffset = 0,
            TabLength = 6,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        Assert.Equal (Orientation.Vertical, tv.Orientation);

        tv.Dispose ();
    }

    [Fact]
    public void UpdateLayout_SetsBorderThickness_ForDepth3_Focused ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = 6,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        // Focused depth 3 top: cap=1, contentSide=0 → (1, 1, 1, 0)
        Assert.Equal (new Thickness (1, 1, 1, 0), tv.Border.Thickness);

        tv.Dispose ();
    }

    [Fact]
    public void UpdateLayout_SetsBorderThickness_ForDepth3_Unfocused ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = 6,
            HasFocus = false,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        // Unfocused depth 3 top: cap=1, contentSide=1 → (1, 1, 1, 1)
        Assert.Equal (new Thickness (1, 1, 1, 1), tv.Border.Thickness);

        tv.Dispose ();
    }

    [Fact]
    public void UpdateLayout_SetsBorderStyle_FromContext ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = 6,
            HasFocus = true,
            LineStyle = LineStyle.Double,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        Assert.Equal (LineStyle.Double, tv.BorderStyle);

        tv.Dispose ();
    }

    [Fact]
    public void UpdateLayout_SetsVisible_True_WhenTabIsVisible ()
    {
        TitleView tv = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        tv.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = 6,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        Assert.True (tv.Visible);

        tv.Dispose ();
    }

    #endregion

    #region Static Geometry Helpers

    [Theory]
    [InlineData (Side.Top, 0, 6, 3, 0, -2, 6, 3)]
    [InlineData (Side.Top, 2, 6, 3, 2, -2, 6, 3)]
    [InlineData (Side.Bottom, 0, 6, 3, 0, 4, 6, 3)]
    [InlineData (Side.Left, 0, 6, 3, -2, 0, 3, 6)]
    [InlineData (Side.Right, 0, 6, 3, 9, 0, 3, 6)]
    public void ComputeHeaderRect_ReturnsCorrectRect (Side side, int offset, int length, int depth, int expectedX, int expectedY, int expectedW, int expectedH)
    {
        Rectangle contentBorder = new (0, 0, 10, 5);

        Rectangle result = TitleView.ComputeHeaderRect (contentBorder, side, offset, length, depth);

        Assert.Equal (new Rectangle (expectedX, expectedY, expectedW, expectedH), result);
    }

    [Theory]
    [InlineData (Side.Top, 3, 0, -2, 10, 7)]
    [InlineData (Side.Bottom, 3, 0, 0, 10, 7)]
    [InlineData (Side.Left, 3, -2, 0, 12, 5)]
    [InlineData (Side.Right, 3, 0, 0, 12, 5)]
    public void ComputeViewBounds_ReturnsCorrectRect (Side side, int depth, int expectedX, int expectedY, int expectedW, int expectedH)
    {
        Rectangle contentBorder = new (0, 0, 10, 5);

        Rectangle result = TitleView.ComputeViewBounds (contentBorder, side, depth);

        Assert.Equal (new Rectangle (expectedX, expectedY, expectedW, expectedH), result);
    }

    [Theory]
    [InlineData (Side.Top, 1, true, 1, 0, 1, 0)]
    [InlineData (Side.Top, 2, true, 1, 1, 1, 0)]
    [InlineData (Side.Top, 3, true, 1, 1, 1, 0)]
    [InlineData (Side.Top, 3, false, 1, 1, 1, 1)]
    [InlineData (Side.Bottom, 3, true, 1, 0, 1, 1)]
    [InlineData (Side.Bottom, 3, false, 1, 1, 1, 1)]
    [InlineData (Side.Left, 3, true, 1, 1, 0, 1)]
    [InlineData (Side.Left, 3, false, 1, 1, 1, 1)]
    [InlineData (Side.Right, 3, true, 0, 1, 1, 1)]
    [InlineData (Side.Right, 3, false, 1, 1, 1, 1)]
    public void ComputeTitleViewThickness_ReturnsExpected (Side side,
                                                           int depth,
                                                           bool hasFocus,
                                                           int expectedLeft,
                                                           int expectedTop,
                                                           int expectedRight,
                                                           int expectedBottom)
    {
        Thickness result = TitleView.ComputeTitleViewThickness (side, depth, hasFocus);

        Assert.Equal (new Thickness (expectedLeft, expectedTop, expectedRight, expectedBottom), result);
    }

    #endregion

    #region Visual Tests

    [Fact]
    public void Standalone_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (8, 5);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };

        TitleView titleView = new () { Text = "Tab1", CanFocus = true, BorderStyle = LineStyle.Rounded };

        superView.Add (titleView);

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┐
                                              ┊╭────╮┊
                                              ┊│Tab1│┊
                                              ┊╰────╯┊
                                              └┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        superView.Dispose ();
    }

    [Fact]
    public void UpdateLayout_Top_Focused_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (10, 7);

        View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        TitleView titleView = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        superView.Add (titleView);

        titleView.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = 5,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │Tab│
                                              │   │
                                              """,
                                              output,
                                              driver);

        superView.Dispose ();
    }

    [Fact]
    public void UpdateLayout_Top_Unfocused_DrawsWithContentSideBorder ()
    {
        IDriver driver = CreateTestDriver (10, 7);

        View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        TitleView titleView = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        superView.Add (titleView);

        titleView.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 0,
            TabLength = 5,
            HasFocus = false,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │Tab│
                                              ╰───╯
                                              """,
                                              output,
                                              driver);

        superView.Dispose ();
    }

    [Fact]
    public void UpdateLayout_Top_Depth5_Focused_DrawsExtraPadding ()
    {
        IDriver driver = CreateTestDriver (10, 7);

        View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        TitleView titleView = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 5, 1, 1) };

        superView.Add (titleView);

        titleView.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 4, 10, 3),
            TabOffset = 0,
            TabLength = 5,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │   │
                                              │Tab│
                                              │   │
                                              │   │
                                              """,
                                              output,
                                              driver);

        superView.Dispose ();
    }

    [Fact]
    public void UpdateLayout_Left_Focused_DrawsVertically ()
    {
        IDriver driver = CreateTestDriver (10, 7);

        View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        TitleView titleView = new ()
        {
            TabSide = Side.Left,
            BorderThickness = new Thickness (3, 1, 1, 1),

            // Disable so TitleView renders its own borders in this standalone test
            SuperViewRendersLineCanvas = false
        };

        superView.Add (titleView);

        titleView.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (2, 0, 8, 7),
            TabOffset = 0,
            TabLength = 5,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        superView.Layout ();
        superView.Draw ();

        // Focused left: contentSide (right) thickness = 0 → no right border
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──
                                              │T
                                              │a
                                              │b
                                              ╰──
                                              """,
                                              output,
                                              driver);

        superView.Dispose ();
    }

    [Fact]
    public void UpdateLayout_CenteredText_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (10, 5);

        View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };

        TitleView titleView = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        superView.Add (titleView);

        // TabLength wider than title text — text should center
        titleView.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 3),
            TabOffset = 0,
            TabLength = 7,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "A",
            ScreenOrigin = Point.Empty
        });

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭─────╮
                                              │  A  │
                                              │     │
                                              """,
                                              output,
                                              driver);

        superView.Dispose ();
    }

    [Fact]
    public void UpdateLayout_HiddenWhenClippedOffscreen ()
    {
        TitleView titleView = new () { TabSide = Side.Top, BorderThickness = new Thickness (1, 3, 1, 1) };

        // Tab offset pushes header entirely outside the view bounds
        titleView.UpdateLayout (new TabLayoutContext
        {
            BorderBounds = new Rectangle (0, 2, 10, 5),
            TabOffset = 20,
            TabLength = 5,
            HasFocus = true,
            LineStyle = LineStyle.Rounded,
            Title = "Tab",
            ScreenOrigin = Point.Empty
        });

        Assert.False (titleView.Visible);

        titleView.Dispose ();
    }

    #endregion
}
