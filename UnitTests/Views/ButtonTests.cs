using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ButtonTests
{
    private readonly ITestOutputHelper _output;
    public ButtonTests (ITestOutputHelper output) { _output = output; }

    // Test that Title and Text are the same
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var view = new Button ();
        view.Title = "Hello";
        Assert.Equal ("Hello", view.Title);
        Assert.Equal ($"Hello", view.TitleTextFormatter.Text);

        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{CM.Glyphs.LeftBracket} Hello {CM.Glyphs.RightBracket}", view.TextFormatter.Text);
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new Button ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{CM.Glyphs.LeftBracket} Hello {CM.Glyphs.RightBracket}", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ($"Hello", view.TitleTextFormatter.Text);
    }

    // BUGBUG: This test is NOT a unit test and needs to be broken apart into 
    //         more specific tests (e.g. it tests Checkbox as well as Button)
    [Fact]
    [AutoInitShutdown]
    public void AutoSize_False_With_Fixed_Width ()
    {
        var tab = new View ();

        var lblWidth = 8;

        var view = new View
        {
            Y = 1,
            Width = lblWidth,
            Height = 1,
            TextAlignment = TextAlignment.Right,
            Text = "Find:"
        };
        tab.Add (view);

        var txtToFind = new TextField
        {
            X = Pos.Right (view) + 1, Y = Pos.Top (view), Width = 20, Text = "Testing buttons."
        };
        tab.Add (txtToFind);

        var btnFindNext = new Button
        {
            AutoSize = false,
            X = Pos.Right (txtToFind) + 1,
            Y = Pos.Top (view),
            Width = 20,
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            TextAlignment = TextAlignment.Centered,
            IsDefault = true,
            Text = "Find _Next"
        };
        tab.Add (btnFindNext);

        var btnFindPrevious = new Button
        {
            AutoSize = false,
            X = Pos.Right (txtToFind) + 1,
            Y = Pos.Top (btnFindNext) + 1,
            Width = 20,
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            TextAlignment = TextAlignment.Centered,
            Text = "Find _Previous"
        };
        tab.Add (btnFindPrevious);

        var btnCancel = new Button
        {
            AutoSize = false,
            X = Pos.Right (txtToFind) + 1,
            Y = Pos.Top (btnFindPrevious) + 2,
            Width = 20,
            TextAlignment = TextAlignment.Centered,
            Text = "Cancel"
        };
        tab.Add (btnCancel);

        var ckbMatchCase = new CheckBox { X = 0, Y = Pos.Top (txtToFind) + 2, Checked = true, Text = "Match c_ase" };
        tab.Add (ckbMatchCase);

        var ckbMatchWholeWord = new CheckBox
        {
            X = 0, Y = Pos.Top (ckbMatchCase) + 1, Checked = false, Text = "Match _whole word"
        };
        tab.Add (ckbMatchWholeWord);

        var tabView = new TabView { Width = Dim.Fill (), Height = Dim.Fill () };
        tabView.AddTab (new Tab { DisplayText = "Find", View = tab }, true);

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };

        tab.Width = view.Width + txtToFind.Width + btnFindNext.Width + 2;
        tab.Height = btnFindNext.Height + btnFindPrevious.Height + btnCancel.Height + 4;

        win.Add (tabView);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (54, 11);

        Assert.Equal (new Rectangle (0, 0, 54, 11), win.Frame);
        Assert.Equal (new Rectangle (0, 0, 52, 9), tabView.Frame);
        Assert.Equal (new Rectangle (0, 0, 50, 7), tab.Frame);
        Assert.Equal (new Rectangle (0, 1, 8, 1), view.Frame);
        Assert.Equal (new Rectangle (9, 1, 20, 1), txtToFind.Frame);

        Assert.Equal (0, txtToFind.ScrollOffset);
        Assert.Equal (16, txtToFind.CursorPosition);

        Assert.Equal (new (30, 1, 20, 1), btnFindNext.Frame);
        Assert.Equal (new (30, 2, 20, 1), btnFindPrevious.Frame);
        Assert.Equal (new (30, 4, 20, 1), btnCancel.Frame);
//        Assert.Equal (new (0, 3, 12, 1), ckbMatchCase.Frame);
//        Assert.Equal (new (0, 4, 18, 1), ckbMatchWholeWord.Frame);

        var btn1 =
            $"{
                CM.Glyphs.LeftBracket
            }{
                CM.Glyphs.LeftDefaultIndicator
            } Find Next {
                CM.Glyphs.RightDefaultIndicator
            }{
                CM.Glyphs.RightBracket
            }";
        var btn2 = $"{CM.Glyphs.LeftBracket} Find Previous {CM.Glyphs.RightBracket}";
        var btn3 = $"{CM.Glyphs.LeftBracket} Cancel {CM.Glyphs.RightBracket}";

        var expected = @$"
┌────────────────────────────────────────────────────┐
│╭────╮                                              │
││Find│                                              │
││    ╰─────────────────────────────────────────────╮│
││                                                  ││
││   Find: Testing buttons.       {
    btn1
}   ││
││                               {
    btn2
}  ││
││{
    CM.Glyphs.Checked
} Match case                                      ││
││{
    CM.Glyphs.UnChecked
} Match whole word                 {
    btn3
}     ││
│└──────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Stays_True_AnchorEnd ()
    {
        var btn = new Button { Y = Pos.Center (), Text = "Say Hello 你", AutoSize = true };
        var btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";

        btn.X = Pos.AnchorEnd () - Pos.Function (() => btn.TextFormatter.Text.GetColumns ());
        btn.X = Pos.AnchorEnd () - Pos.Function (() => btn.TextFormatter.Text.GetColumns ());

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (btn);
        var top = new Toplevel ();
        top.Add (win);

        Assert.True (btn.AutoSize);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        var expected = @$"
┌────────────────────────────┐
│                            │
│            {
    btnTxt
}│
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        Assert.True (btn.AutoSize);
        btn.Text = "Say Hello 你 changed";
        btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";
        Assert.True (btn.AutoSize);
        Application.Refresh ();

        expected = @$"
┌────────────────────────────┐
│                            │
│    {
    btnTxt
}│
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Stays_True_Center ()
    {
        var btn = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (btn);
        var top = new Toplevel ();
        top.Add (win);

        Assert.True (btn.AutoSize);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        var expected = @$"
┌────────────────────────────┐
│                            │
│      {
    CM.Glyphs.LeftBracket
} Say Hello 你 {
    CM.Glyphs.RightBracket
}      │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        Assert.True (btn.AutoSize);
        btn.Text = "Say Hello 你 changed";
        Assert.True (btn.AutoSize);
        Application.Refresh ();

        expected = @$"
┌────────────────────────────┐
│                            │
│  {
    CM.Glyphs.LeftBracket
} Say Hello 你 changed {
    CM.Glyphs.RightBracket
}  │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Stays_True_With_EmptyText ()
    {
        var btn = new Button { X = Pos.Center (), Y = Pos.Center (), AutoSize = true };

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (btn);
        var top = new Toplevel ();
       top.Add (win);

        Assert.True (btn.AutoSize);

        btn.Text = "Say Hello 你";

        Assert.True (btn.AutoSize);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        var expected = @$"
┌────────────────────────────┐
│                            │
│      {
    CM.Glyphs.LeftBracket
} Say Hello 你 {
    CM.Glyphs.RightBracket
}      │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Button_AutoSize_False_With_Fixed_Width ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (20, 5);

        var top = new View { Width = 20, Height = 5 };

        var btn1 = new Button
        {
            AutoSize = false,
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 16,
            Height = 1,
            Text = "Open me!"
        };

        var btn2 = new Button
        {
            AutoSize = false,
            X = Pos.Center (),
            Y = Pos.Center () + 1,
            Width = 16,
            Height = 1,
            Text = "Close me!"
        };
        top.Add (btn1, btn2);
        top.BeginInit ();
        top.EndInit ();
        top.Draw ();

        Assert.Equal ("{Width=16, Height=1}", btn1.TextFormatter.Size.ToString ());
        Assert.Equal ("{Width=16, Height=1}", btn2.TextFormatter.Size.ToString ());

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @$"
    {
        CM.Glyphs.LeftBracket
    } {
        btn1.Text
    } {
        CM.Glyphs.RightBracket
    }
   {
       CM.Glyphs.LeftBracket
   } {
       btn2.Text
   } {
       CM.Glyphs.RightBracket
   }",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
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
    }

    [Fact]
    [AutoInitShutdown]
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
    }

    [Fact]
    [SetupFakeDriver]
    public void Constructors_Defaults ()
    {
        var btn = new Button ();
        Assert.Equal (string.Empty, btn.Text);
        btn.BeginInit ();
        btn.EndInit ();

        Assert.Equal ($"{CM.Glyphs.LeftBracket}  {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.False (btn.IsDefault);
        Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.True (btn.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 4, 1), btn.Bounds);
        Assert.Equal (new Rectangle (0, 0, 4, 1), btn.Frame);
        Assert.Equal ($"{CM.Glyphs.LeftBracket}  {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.False (btn.IsDefault);
        Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.True (btn.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 4, 1), btn.Bounds);
        Assert.Equal (new Rectangle (0, 0, 4, 1), btn.Frame);

        Assert.Equal (string.Empty, btn.Title);
        Assert.Equal (KeyCode.Null, btn.HotKey);

        btn.Draw ();

        var expected = @$"
{
    CM.Glyphs.LeftBracket
}  {
    CM.Glyphs.RightBracket
}
";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        btn = new Button { Text = "_Test", IsDefault = true };
        btn.BeginInit ();
        btn.EndInit ();
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.Equal (Key.T, btn.HotKey);
        Assert.Equal ("_Test", btn.Text);

        Assert.Equal (
                      $"{
                          CM.Glyphs.LeftBracket
                      }{
                          CM.Glyphs.LeftDefaultIndicator
                      } Test {
                          CM.Glyphs.RightDefaultIndicator
                      }{
                          CM.Glyphs.RightBracket
                      }",
                      btn.TextFormatter.Format ()
                     );
        Assert.True (btn.IsDefault);
        Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
        Assert.True (btn.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 10, 1), btn.Bounds);
        Assert.Equal (new Rectangle (0, 0, 10, 1), btn.Frame);
        Assert.Equal (KeyCode.T, btn.HotKey);

        btn = new Button { X = 1, Y = 2, Text = "_abc", IsDefault = true };
        btn.BeginInit ();
        btn.EndInit ();
        Assert.Equal ("_abc", btn.Text);
        Assert.Equal (Key.A, btn.HotKey);

        Assert.Equal (
                      $"{
                          CM.Glyphs.LeftBracket
                      }{
                          CM.Glyphs.LeftDefaultIndicator
                      } abc {
                          CM.Glyphs.RightDefaultIndicator
                      }{
                          CM.Glyphs.RightBracket
                      }",
                      btn.TextFormatter.Format ()
                     );
        Assert.True (btn.IsDefault);
        Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
        Assert.Equal ('_', btn.HotKeySpecifier.Value);
        Assert.True (btn.CanFocus);

        Application.Driver.ClearContents ();
        btn.Draw ();

        expected = @$"
 {
     CM.Glyphs.LeftBracket
 }{
     CM.Glyphs.LeftDefaultIndicator
 } abc {
     CM.Glyphs.RightDefaultIndicator
 }{
     CM.Glyphs.RightBracket
 }
";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        Assert.Equal (new Rectangle (0, 0, 9, 1), btn.Bounds);
        Assert.Equal (new Rectangle (1, 2, 9, 1), btn.Frame);
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKeyChange_Works ()
    {
        var clicked = false;
        var btn = new Button { Text = "_Test" };
        btn.Accept += (s, e) => clicked = true;
        var top = new Toplevel ();
        top.Add (btn);
        Application.Begin (top);

        Assert.Equal (KeyCode.T, btn.HotKey);
        Assert.True (btn.NewKeyDownEvent (Key.T));
        Assert.True (clicked);

        clicked = false;
        Assert.True (btn.NewKeyDownEvent (Key.T.WithAlt));
        Assert.True (clicked);

        clicked = false;
        btn.HotKey = KeyCode.E;
        Assert.True (btn.NewKeyDownEvent (Key.E.WithAlt));
        Assert.True (clicked);
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

        btn.Accept += (s, e) => pressed++;

        // The Button class supports the Default and Accept command
        Assert.Contains (Command.HotKey, btn.GetSupportedCommands ());
        Assert.Contains (Command.Accept, btn.GetSupportedCommands ());

        var top = new Toplevel ();
        top.Add (btn);
        Application.Begin (top);

        // default keybinding is Space which results in keypress
        Application.OnKeyDown (new Key ((KeyCode)' '));
        Assert.Equal (1, pressed);

        // remove the default keybinding (Space)
        btn.KeyBindings.Clear (Command.HotKey);
        btn.KeyBindings.Clear (Command.Accept);

        // After clearing the default keystroke the Space button no longer does anything for the Button
        Application.OnKeyDown (new Key ((KeyCode)' '));
        Assert.Equal (1, pressed);

        // Set a new binding of b for the click (Accept) event
        btn.KeyBindings.Add (Key.B, Command.HotKey);
        btn.KeyBindings.Add (Key.B, Command.Accept);

        // now pressing B should call the button click event
        Application.OnKeyDown (Key.B);
        Assert.Equal (2, pressed);

        // now pressing Shift-B should NOT call the button click event
        Application.OnKeyDown (Key.B.WithShift);
        Assert.Equal (2, pressed);

        // now pressing Alt-B should NOT call the button click event
        Application.OnKeyDown (Key.B.WithAlt);
        Assert.Equal (2, pressed);

        // now pressing Shift-Alt-B should NOT call the button click event
        Application.OnKeyDown (Key.B.WithAlt.WithShift);
        Assert.Equal (2, pressed);
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Command ()
    {
        var clicked = false;
        var btn = new Button { Text = "_Test" };
        btn.Accept += (s, e) => clicked = true;
        var top = new Toplevel ();
        top.Add (btn);
        Application.Begin (top);

        // Hot key. Both alone and with alt
        Assert.Equal (KeyCode.T, btn.HotKey);
        Assert.True (btn.NewKeyDownEvent (Key.T));
        Assert.True (clicked);
        clicked = false;

        Assert.True (btn.NewKeyDownEvent (Key.T.WithAlt));
        Assert.True (clicked);
        clicked = false;

        Assert.True (btn.NewKeyDownEvent (btn.HotKey));
        Assert.True (clicked);
        clicked = false;
        Assert.True (btn.NewKeyDownEvent (btn.HotKey));
        Assert.True (clicked);
        clicked = false;

        // IsDefault = false
        // Space and Enter should work
        Assert.False (btn.IsDefault);
        Assert.True (btn.NewKeyDownEvent (Key.Enter));
        Assert.True (clicked);
        clicked = false;

        // IsDefault = true
        // Space and Enter should work
        btn.IsDefault = true;
        Assert.True (btn.NewKeyDownEvent (Key.Enter));
        Assert.True (clicked);
        clicked = false;

        // Toplevel does not handle Enter, so it should get passed on to button
        Assert.True (Application.Top.NewKeyDownEvent (Key.Enter));
        Assert.True (clicked);
        clicked = false;

        // Direct
        Assert.True (btn.NewKeyDownEvent (Key.Enter));
        Assert.True (clicked);
        clicked = false;

        Assert.True (btn.NewKeyDownEvent (Key.Space));
        Assert.True (clicked);
        clicked = false;

        Assert.True (btn.NewKeyDownEvent (new Key ((KeyCode)'T')));
        Assert.True (clicked);
        clicked = false;

        // Change hotkey:
        btn.Text = "Te_st";
        Assert.True (btn.NewKeyDownEvent (btn.HotKey));
        Assert.True (clicked);
        clicked = false;
    }

    [Fact]
    public void HotKey_Command_Accepts ()
    {
        var button = new Button ();
        var accepted = false;

        button.Accept += ButtonOnAccept;
        button.InvokeCommand (Command.HotKey);

        Assert.True (accepted);

        return;
        void ButtonOnAccept (object sender, CancelEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var button = new Button ();
        var acceptInvoked = false;

        button.Accept += ButtonAccept;

        var ret = button.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        return;
        void ButtonAccept (object sender, CancelEventArgs e)
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
    }

    [Fact]
    [AutoInitShutdown]
    public void Update_Only_On_Or_After_Initialize ()
    {
        var btn = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (btn);
        var top = new Toplevel ();
        top.Add (win);

        Assert.False (btn.IsInitialized);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.True (btn.IsInitialized);
        Assert.Equal ("Say Hello 你", btn.Text);
        Assert.Equal ($"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.Equal (new Rectangle (0, 0, 16, 1), btn.Bounds);
        var btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";

        var expected = @$"
┌────────────────────────────┐
│                            │
│      {
    btnTxt
}      │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 30, 5), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Update_Parameterless_Only_On_Or_After_Initialize ()
    {
        var btn = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (btn);
        var top = new Toplevel ();
        top.Add (win);

        Assert.False (btn.IsInitialized);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.True (btn.IsInitialized);
        Assert.Equal ("Say Hello 你", btn.Text);
        Assert.Equal ($"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
        Assert.Equal (new Rectangle (0, 0, 16, 1), btn.Bounds);
        var btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";

        var expected = @$"
┌────────────────────────────┐
│                            │
│      {
    btnTxt
}      │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 30, 5), pos);
    }
}
