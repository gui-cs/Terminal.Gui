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

    #region Visual Tests

    [Fact]
    public void HotKey_Without_DrawsCorrectly ()
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
    public void HotKey_With_DrawsCorrectly ()
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

        TitleView titleView = new () { Text = "_Tab1", CanFocus = true, BorderStyle = LineStyle.Rounded };

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

    #endregion
}
