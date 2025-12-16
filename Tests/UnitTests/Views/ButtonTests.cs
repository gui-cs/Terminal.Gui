using TerminalGuiFluentTesting;
using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests;

public class ButtonTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeApplication]
    public void Constructors_Defaults ()
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        var btn = new Button ()
        {
            App = ApplicationImpl.Instance
        };
        Assert.Equal (string.Empty, btn.Text);
        btn.BeginInit ();
        btn.EndInit ();
        btn.SetRelativeLayout (new (100, 100));

        Assert.Equal ($"{Glyphs.LeftBracket}  {Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.False (btn.IsDefault);
        Assert.Equal (Alignment.Center, btn.TextAlignment);
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.True (btn.CanFocus);
        Assert.Equal (new (0, 0, 4, 1), btn.Viewport);
        Assert.Equal (new (0, 0, 4, 1), btn.Frame);
        Assert.Equal ($"{Glyphs.LeftBracket}  {Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.False (btn.IsDefault);
        Assert.Equal (Alignment.Center, btn.TextAlignment);
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.True (btn.CanFocus);
        Assert.Equal (new (0, 0, 4, 1), btn.Viewport);
        Assert.Equal (new (0, 0, 4, 1), btn.Frame);

        Assert.Equal (string.Empty, btn.Title);
        Assert.Equal (KeyCode.Null, btn.HotKey);

        btn.Draw ();

        var expected = @$"
{Glyphs.LeftBracket}  {Glyphs.RightBracket}
";
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        btn.Dispose ();

        btn = new ()
        {
            App = ApplicationImpl.Instance,
            Text = "_Test", IsDefault = true
        };
        btn.Layout ();
        Assert.Equal (new (10, 1), btn.TextFormatter.ConstrainToSize);

        btn.BeginInit ();
        btn.EndInit ();
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.Equal (Key.T, btn.HotKey);
        Assert.Equal ("_Test", btn.Text);

        Assert.Equal (
                      $"{Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} Test {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}",
                      btn.TextFormatter.Format ()
                     );
        Assert.True (btn.IsDefault);
        Assert.Equal (Alignment.Center, btn.TextAlignment);
        Assert.True (btn.CanFocus);

        btn.SetRelativeLayout (new (100, 100));

        // 0123456789012345678901234567890123456789
        // [* Test *]
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.Equal (10, btn.TextFormatter.Format ().Length);
        Assert.Equal (new (10, 1), btn.TextFormatter.ConstrainToSize);
        Assert.Equal (new (10, 1), btn.GetContentSize ());
        Assert.Equal (new (0, 0, 10, 1), btn.Viewport);
        Assert.Equal (new (0, 0, 10, 1), btn.Frame);
        Assert.Equal (KeyCode.T, btn.HotKey);

        btn.Dispose ();

        btn = new ()
        {
            App = ApplicationImpl.Instance,
            X = 1, Y = 2, Text = "_abc", IsDefault = true
        };
        btn.BeginInit ();
        btn.EndInit ();
        Assert.Equal ("_abc", btn.Text);
        Assert.Equal (Key.A, btn.HotKey);

        Assert.Equal (
                      $"{Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} abc {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}",
                      btn.TextFormatter.Format ()
                     );
        Assert.True (btn.IsDefault);
        Assert.Equal (Alignment.Center, btn.TextAlignment);
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.True (btn.CanFocus);

        ApplicationImpl.Instance.Driver?.ClearContents ();
        btn.Draw ();

        expected = @$"
 {Glyphs.LeftBracket}{Glyphs.LeftDefaultIndicator} abc {Glyphs.RightDefaultIndicator}{Glyphs.RightBracket}
";
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output, ApplicationImpl.Instance.Driver);

        Assert.Equal (new (0, 0, 9, 1), btn.Viewport);
        Assert.Equal (new (1, 2, 9, 1), btn.Frame);
        btn.Dispose ();
    }

    /// <summary>
    ///     This test demonstrates how to change the activation key for Button as described in the README.md keyboard
    ///     handling section
    /// </summary>
    [Fact]
    [AutoInitShutdown]
    public void KeyBindingExample ()
    {
        var pressed = 0;
        var btn = new Button { Text = "Press Me" };

        btn.Accepting += (s, e) => pressed++;

        // The Button class supports the Default and Accept command
        Assert.Contains (Command.HotKey, btn.GetSupportedCommands ());
        Assert.Contains (Command.Accept, btn.GetSupportedCommands ());

        var top = new Runnable ();
        top.Add (btn);
        Application.Begin (top);

        Assert.True (btn.HasFocus);

        // default keybinding is Space which results in Command.Accept (when focused)
        Application.RaiseKeyDownEvent (new ((KeyCode)' '));
        Assert.Equal (1, pressed);

        // remove the default keybinding (Space)
        btn.KeyBindings.Clear (Command.HotKey);
        btn.KeyBindings.Clear (Command.Accept);

        // After clearing the default keystroke the Space button no longer does anything for the Button
        Application.RaiseKeyDownEvent (new ((KeyCode)' '));
        Assert.Equal (1, pressed);

        // Set a new binding of b for the click (Accept) event
        btn.KeyBindings.Add (Key.B, Command.HotKey); // b will now trigger the Accept command (when focused or not)

        // now pressing B should call the button click event
        Application.RaiseKeyDownEvent (Key.B);
        Assert.Equal (2, pressed);

        // now pressing Shift-B should NOT call the button click event
        Application.RaiseKeyDownEvent (Key.B.WithShift);
        Assert.Equal (2, pressed);

        // now pressing Alt-B should NOT call the button click event
        Application.RaiseKeyDownEvent (Key.B.WithAlt);
        Assert.Equal (2, pressed);

        // now pressing Shift-Alt-B should NOT call the button click event
        Application.RaiseKeyDownEvent (Key.B.WithAlt.WithShift);
        Assert.Equal (2, pressed);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Command ()
    {
        var clicked = false;
        var btn = new Button { Text = "_Test" };
        btn.Accepting += (s, e) => clicked = true;
        var top = new Runnable ();
        top.Add (btn);
        Application.Begin (top);

        // Hot key. Both alone and with alt
        Assert.Equal (KeyCode.T, btn.HotKey);
        Assert.False (btn.NewKeyDownEvent (Key.T)); // Button processes, but does not handle
        Assert.True (clicked);
        clicked = false;

        Assert.False (btn.NewKeyDownEvent (Key.T.WithAlt));
        Assert.True (clicked);
        clicked = false;

        Assert.False (btn.NewKeyDownEvent (btn.HotKey));
        Assert.True (clicked);
        clicked = false;
        Assert.False (btn.NewKeyDownEvent (btn.HotKey));
        Assert.True (clicked);
        clicked = false;

        // IsDefault = false
        // Space and Enter should work
        Assert.False (btn.IsDefault);
        Assert.False (btn.NewKeyDownEvent (Key.Enter));
        Assert.True (clicked);
        clicked = false;

        // IsDefault = true
        // Space and Enter should work
        btn.IsDefault = true;
        Assert.False (btn.NewKeyDownEvent (Key.Enter));
        Assert.True (clicked);
        clicked = false;

        // Runnable does not handle Enter, so it should get passed on to button
        Assert.False (Application.TopRunnableView.NewKeyDownEvent (Key.Enter));
        Assert.True (clicked);
        clicked = false;

        // Direct
        Assert.False (btn.NewKeyDownEvent (Key.Enter));
        Assert.True (clicked);
        clicked = false;

        Assert.False (btn.NewKeyDownEvent (Key.Space));
        Assert.True (clicked);
        clicked = false;

        Assert.False (btn.NewKeyDownEvent (new ((KeyCode)'T')));
        Assert.True (clicked);
        clicked = false;

        // Change hotkey:
        btn.Text = "Te_st";
        Assert.False (btn.NewKeyDownEvent (btn.HotKey));
        Assert.True (clicked);
        clicked = false;

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Update_Parameterless_Only_On_Or_After_Initialize ()
    {
        Button.DefaultShadow = ShadowStyle.None;
        var btn = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (btn);
        var top = new Runnable ();
        top.Add (win);

        Assert.False (btn.IsInitialized);

        Application.Begin (top);
        Application.Driver?.SetScreenSize (30, 5);
        Application.LayoutAndDraw ();
        Assert.True (btn.IsInitialized);
        Assert.Equal ("Say Hello 你", btn.Text);
        Assert.Equal ($"{Glyphs.LeftBracket} {btn.Text} {Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.Equal (new (0, 0, 16, 1), btn.Viewport);
        var btnTxt = $"{Glyphs.LeftBracket} {btn.Text} {Glyphs.RightBracket}";

        var expected = @$"
┌────────────────────────────┐
│                            │
│      {btnTxt}      │
│                            │
└────────────────────────────┘
";

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);
        top.Dispose ();
    }

    [Theory (Skip = "Broken in #4474")]
    [InlineData (MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased, MouseFlags.LeftButtonClicked)]
    [InlineData (MouseFlags.MiddleButtonPressed, MouseFlags.MiddleButtonReleased, MouseFlags.MiddleButtonClicked)]
    [InlineData (MouseFlags.RightButtonPressed, MouseFlags.RightButtonReleased, MouseFlags.RightButtonClicked)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released, MouseFlags.Button4Clicked)]
    public void MouseHoldRepeat_True_ButtonClick_Accepts (MouseFlags pressed, MouseFlags released, MouseFlags clicked)
    {
        var me = new Mouse ();

        var button = new Button
        {
            Width = 1,
            Height = 1,
            MouseHoldRepeat = true
        };

        var activatingCount = 0;

        button.Activating += (s, e) => activatingCount++;
        var acceptedCount = 0;

        button.Accepting += (s, e) =>
                            {
                                acceptedCount++;
                                e.Handled = true;
                            };

        me = new ();
        me.Flags = pressed;
        button.NewMouseEvent (me);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptedCount);

        me = new ();
        me.Flags = released;
        button.NewMouseEvent (me);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptedCount);

        me = new ();
        me.Flags = clicked;
        button.NewMouseEvent (me);
        Assert.Equal (1, activatingCount);
        Assert.Equal (1, acceptedCount);

        button.Dispose ();
    }

    [Theory (Skip = "Broken in #4474")]
    [InlineData (MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased)]
    [InlineData (MouseFlags.MiddleButtonPressed, MouseFlags.MiddleButtonReleased)]
    [InlineData (MouseFlags.RightButtonPressed, MouseFlags.RightButtonReleased)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released)]
    public void MouseHoldRepeat_True_ButtonPressRelease_Does_Not_Raise_Selected_Or_Accepted (MouseFlags pressed, MouseFlags released)
    {
        var me = new Mouse ();

        var button = new Button
        {
            Width = 1,
            Height = 1,
            MouseHoldRepeat = true
        };

        var acceptedCount = 0;

        button.Accepting += (s, e) =>
                            {
                                acceptedCount++;
                                e.Handled = true;
                            };

        var activatingCount = 0;

        button.Activating += (s, e) =>
                            {
                                activatingCount++;
                                e.Handled = true;
                            };

        me.Flags = pressed;
        button.NewMouseEvent (me);
        Assert.Equal (0, acceptedCount);
        Assert.Equal (0, activatingCount);

        me.Flags = released;
        button.NewMouseEvent (me);
        Assert.Equal (0, acceptedCount);
        Assert.Equal (0, activatingCount);

        button.Dispose ();
    }
}
