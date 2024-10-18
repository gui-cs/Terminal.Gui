using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ButtonTests (ITestOutputHelper output)
{
    // Test that Title and Text are the same
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var view = new Button ();
        view.Title = "Hello";
        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);

        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{CM.Glyphs.LeftBracket} Hello {CM.Glyphs.RightBracket}", view.TextFormatter.Text);
        view.Dispose ();
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new Button ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{CM.Glyphs.LeftBracket} Hello {CM.Glyphs.RightBracket}", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);
        view.Dispose ();
    }

    [Theory]
    [InlineData ("01234", 0, 0, 0, 0)]
    [InlineData ("01234", 1, 0, 1, 0)]
    [InlineData ("01234", 0, 1, 0, 1)]
    [InlineData ("01234", 1, 1, 1, 1)]
    [InlineData ("01234", 10, 1, 10, 1)]
    [InlineData ("01234", 10, 3, 10, 3)]
    [InlineData ("0_1234", 0, 0, 0, 0)]
    [InlineData ("0_1234", 1, 0, 1, 0)]
    [InlineData ("0_1234", 0, 1, 0, 1)]
    [InlineData ("0_1234", 1, 1, 1, 1)]
    [InlineData ("0_1234", 10, 1, 10, 1)]
    [InlineData ("0_12你", 10, 3, 10, 3)]
    [InlineData ("0_12你", 0, 0, 0, 0)]
    [InlineData ("0_12你", 1, 0, 1, 0)]
    [InlineData ("0_12你", 0, 1, 0, 1)]
    [InlineData ("0_12你", 1, 1, 1, 1)]
    [InlineData ("0_12你", 10, 1, 10, 1)]
    public void Button_AbsoluteSize_Text (string text, int width, int height, int expectedWidth, int expectedHeight)
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        var btn1 = new Button
        {
            Text = text,
            Width = width,
            Height = height,
        };

        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.GetContentSize ());
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.TextFormatter.ConstrainToSize);

        btn1.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (1, 0, 1, 0)]
    [InlineData (0, 1, 0, 1)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (10, 1, 10, 1)]
    [InlineData (10, 3, 10, 3)]
    public void Button_AbsoluteSize_DefaultText (int width, int height, int expectedWidth, int expectedHeight)
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        var btn1 = new Button ();
        btn1.Width = width;
        btn1.Height = height;

        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.TextFormatter.ConstrainToSize);

        btn1.Dispose ();
    }

    [Fact]
    public void Button_HotKeyChanged_EventFires ()
    {
        var btn = new Button { Text = "_Yar" };

        object sender = null;
        KeyChangedEventArgs args = null;

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Y, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Y, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.Dispose ();
    }

    [Fact]
    public void Button_HotKeyChanged_EventFires_WithNone ()
    {
        var btn = new Button ();

        object sender = null;
        KeyChangedEventArgs args = null;

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Null, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void Constructors_Defaults ()
    {
        // Override CM
        Button.DefaultShadow = ShadowStyle.None;

        var btn = new Button ();
        Assert.Equal (string.Empty, btn.Text);
        btn.BeginInit ();
        btn.EndInit ();
        btn.SetRelativeLayout (new (100, 100));

        Assert.Equal ($"{CM.Glyphs.LeftBracket}  {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.False (btn.IsDefault);
        Assert.Equal (Alignment.Center, btn.TextAlignment);
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.True (btn.CanFocus);
        Assert.Equal (new (0, 0, 4, 1), btn.Viewport);
        Assert.Equal (new (0, 0, 4, 1), btn.Frame);
        Assert.Equal ($"{CM.Glyphs.LeftBracket}  {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
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
{CM.Glyphs.LeftBracket}  {CM.Glyphs.RightBracket}
";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        btn.Dispose ();

        btn = new () { Text = "_Test", IsDefault = true };
        btn.Layout ();
        Assert.Equal (new (10, 1), btn.TextFormatter.ConstrainToSize);



        btn.BeginInit ();
        btn.EndInit ();
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.Equal (Key.T, btn.HotKey);
        Assert.Equal ("_Test", btn.Text);

        Assert.Equal (
                      $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Test {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}",
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

        btn = new () { X = 1, Y = 2, Text = "_abc", IsDefault = true };
        btn.BeginInit ();
        btn.EndInit ();
        Assert.Equal ("_abc", btn.Text);
        Assert.Equal (Key.A, btn.HotKey);

        Assert.Equal (
                      $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} abc {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}",
                      btn.TextFormatter.Format ()
                     );
        Assert.True (btn.IsDefault);
        Assert.Equal (Alignment.Center, btn.TextAlignment);
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.True (btn.CanFocus);

        Application.Driver?.ClearContents ();
        btn.Draw ();

        expected = @$"
 {CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} abc {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}
";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        Assert.Equal (new (0, 0, 9, 1), btn.Viewport);
        Assert.Equal (new (1, 2, 9, 1), btn.Frame);
        btn.Dispose ();
    }

    [Fact]
    public void HotKeyChange_Works ()
    {
        var clicked = false;
        var btn = new Button { Text = "_Test" };
        btn.Accepting += (s, e) => clicked = true;

        Assert.Equal (KeyCode.T, btn.HotKey);
        Assert.False (btn.NewKeyDownEvent (Key.T)); // Button processes, but does not handle
        Assert.True (clicked);

        clicked = false;
        Assert.False (btn.NewKeyDownEvent (Key.T.WithAlt)); // Button processes, but does not handle
        Assert.True (clicked);

        clicked = false;
        btn.HotKey = KeyCode.E;
        Assert.False (btn.NewKeyDownEvent (Key.E.WithAlt)); // Button processes, but does not handle
        Assert.True (clicked);
    }

    [Theory]
    [InlineData (false, 0)]
    [InlineData (true, 1)]
    public void Space_Fires_Accept (bool focused, int expected)
    {
        View superView = new View ()
        {
            CanFocus = true,
        };

        Button button = new ();

        button.CanFocus = focused;

        int acceptInvoked = 0;
        button.Accepting += (s, e) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.Space);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Theory]
    [InlineData (false, 0)]
    [InlineData (true, 1)]
    public void Enter_Fires_Accept (bool focused, int expected)
    {
        View superView = new View ()
        {
            CanFocus = true,
        };

        Button button = new ();

        button.CanFocus = focused;

        int acceptInvoked = 0;
        button.Accepting += (s, e) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 1)]
    public void HotKey_Fires_Accept (bool focused, int expected)
    {
        View superView = new View ()
        {
            CanFocus = true,
        };

        Button button = new ()
        {
            HotKey = Key.A
        };

        button.CanFocus = focused;

        int acceptInvoked = 0;
        button.Accepting += (s, e) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.A);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
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

        var top = new Toplevel ();
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
        var top = new Toplevel ();
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

        // Toplevel does not handle Enter, so it should get passed on to button
        Assert.False (Application.Top.NewKeyDownEvent (Key.Enter));
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
    public void HotKey_Command_Accepts ()
    {
        var button = new Button ();
        var accepted = false;

        button.Accepting += ButtonOnAccept;
        button.InvokeCommand (Command.HotKey);

        Assert.True (accepted);
        button.Dispose ();

        return;

        void ButtonOnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var button = new Button ();
        var acceptInvoked = false;

        button.Accepting += ButtonAccept;

        bool? ret = button.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        button.Dispose ();

        return;

        void ButtonAccept (object sender, CommandEventArgs e)
        {
            acceptInvoked = true;
            e.Cancel = true;
        }
    }

    [Fact]
    public void Setting_Empty_Text_Sets_HoKey_To_KeyNull ()
    {
        var super = new View ();
        var btn = new Button { Text = "_Test" };
        super.Add (btn);
        super.BeginInit ();
        super.EndInit ();

        Assert.Equal ("_Test", btn.Text);
        Assert.Equal (KeyCode.T, btn.HotKey);

        btn.Text = string.Empty;
        Assert.Equal ("", btn.Text);
        Assert.Equal (KeyCode.Null, btn.HotKey);
        btn.Text = string.Empty;
        Assert.Equal ("", btn.Text);
        Assert.Equal (KeyCode.Null, btn.HotKey);

        btn.Text = "Te_st";
        Assert.Equal ("Te_st", btn.Text);
        Assert.Equal (KeyCode.S, btn.HotKey);
        super.Dispose ();
    }

    [Fact]
    public void TestAssignTextToButton ()
    {
        View b = new Button { Text = "heya" };
        Assert.Equal ("heya", b.Text);
        Assert.Contains ("heya", b.TextFormatter.Text);
        b.Text = "heyb";
        Assert.Equal ("heyb", b.Text);
        Assert.Contains ("heyb", b.TextFormatter.Text);

        // with cast
        Assert.Equal ("heyb", ((Button)b).Text);
        b.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Update_Parameterless_Only_On_Or_After_Initialize ()
    {
        Button.DefaultShadow = ShadowStyle.None;
        var btn = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (btn);
        var top = new Toplevel ();
        top.Add (win);

        Assert.False (btn.IsInitialized);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 5);

        Assert.True (btn.IsInitialized);
        Assert.Equal ("Say Hello 你", btn.Text);
        Assert.Equal ($"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.Equal (new (0, 0, 16, 1), btn.Viewport);
        var btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";

        var expected = @$"
┌────────────────────────────┐
│                            │
│      {btnTxt}      │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);
        top.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, MouseFlags.Button1Released, MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Pressed, MouseFlags.Button2Released, MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Pressed, MouseFlags.Button3Released, MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released, MouseFlags.Button4Clicked)]
    public void WantContinuousButtonPressed_True_ButtonClick_Accepts (MouseFlags pressed, MouseFlags released, MouseFlags clicked)
    {
        var me = new MouseEventArgs ();

        var button = new Button ()
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true
        };

        var selectingCount = 0;

        button.Selecting += (s, e) => selectingCount++;
        var acceptedCount = 0;
        button.Accepting += (s, e) =>
                           {
                               acceptedCount++;
                               e.Cancel = true;
                           };

        me = new MouseEventArgs ();
        me.Flags = pressed;
        button.NewMouseEvent (me);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);

        me = new MouseEventArgs ();
        me.Flags = released;
        button.NewMouseEvent (me);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);

        me = new MouseEventArgs ();
        me.Flags = clicked;
        button.NewMouseEvent (me);
        Assert.Equal (1, selectingCount);
        Assert.Equal (1, acceptedCount);

        button.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, MouseFlags.Button1Released)]
    [InlineData (MouseFlags.Button2Pressed, MouseFlags.Button2Released)]
    [InlineData (MouseFlags.Button3Pressed, MouseFlags.Button3Released)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released)]
    public void WantContinuousButtonPressed_True_ButtonPressRelease_Does_Not_Raise_Selected_Or_Accepted (MouseFlags pressed, MouseFlags released)
    {
        var me = new MouseEventArgs ();

        var button = new Button ()
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true
        };

        var acceptedCount = 0;

        button.Accepting += (s, e) =>
                           {
                               acceptedCount++;
                               e.Cancel = true;
                           };

        var selectingCount = 0;

        button.Selecting += (s, e) =>
                           {
                               selectingCount++;
                               e.Cancel = true;
                           };

        me.Flags = pressed;
        button.NewMouseEvent (me);
        Assert.Equal (0, acceptedCount);
        Assert.Equal (0, selectingCount);

        me.Flags = released;
        button.NewMouseEvent (me);
        Assert.Equal (0, acceptedCount);
        Assert.Equal (0, selectingCount);

        button.Dispose ();
    }

}