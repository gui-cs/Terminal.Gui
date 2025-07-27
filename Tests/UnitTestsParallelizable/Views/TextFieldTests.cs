using System.Text;

namespace Terminal.Gui.ViewsTests;

public class TextFieldTests
{
    [Fact]
    public void Cancel_TextChanging_ThenBackspace ()
    {
        var tf = new TextField ();
        tf.SetFocus ();
        tf.NewKeyDownEvent (Key.A.WithShift);
        Assert.Equal ("A", tf.Text);

        // cancel the next keystroke
        tf.TextChanging += (s, e) => e.Handled = e.Result == "AB";
        tf.NewKeyDownEvent (Key.B.WithShift);

        // B was canceled so should just be A
        Assert.Equal ("A", tf.Text);

        // now delete the A
        tf.NewKeyDownEvent (Key.Backspace);

        Assert.Equal ("", tf.Text);
    }

    [Fact]
    public void HistoryText_IsDirty_ClearHistoryChanges ()
    {
        var text = "Testing";
        var tf = new TextField { Text = text };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (text, tf.Text);
        tf.ClearHistoryChanges ();
        Assert.False (tf.IsDirty);

        Assert.True (tf.NewKeyDownEvent (Key.A.WithShift));
        Assert.Equal ($"{text}A", tf.Text);
        Assert.True (tf.IsDirty);
    }

    [Fact]
    public void Space_Does_Not_Raise_Selected ()
    {
        TextField tf = new ();

        tf.Selecting += (sender, args) => Assert.Fail ("Selected should not be raied.");

        Toplevel top = new ();
        top.Add (tf);
        tf.SetFocus ();
        top.NewKeyDownEvent (Key.Space);

        top.Dispose ();
    }

    [Fact]
    public void Enter_Does_Not_Raise_Selected ()
    {
        TextField tf = new ();

        var selectingCount = 0;
        tf.Selecting += (sender, args) => selectingCount++;

        Toplevel top = new ();
        top.Add (tf);
        tf.SetFocus ();
        top.NewKeyDownEvent (Key.Enter);

        Assert.Equal (0, selectingCount);

        top.Dispose ();
    }

    [Fact]
    public void Enter_Raises_Accepted ()
    {
        TextField tf = new ();

        var acceptedCount = 0;
        tf.Accepting += (sender, args) => acceptedCount++;

        Toplevel top = new ();
        top.Add (tf);
        tf.SetFocus ();
        top.NewKeyDownEvent (Key.Enter);

        Assert.Equal (1, acceptedCount);

        top.Dispose ();
    }

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new TextField ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var view = new TextField ();
        var accepted = false;
        view.Accepting += OnAccept;
        view.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accepted_Command_Fires_Accept ()
    {
        var view = new TextField ();

        var accepted = false;
        view.Accepting += Accept;
        view.InvokeCommand (Command.Accept);
        Assert.True (accepted);

        return;

        void Accept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accepted_No_Handler_Enables_Default_Button_Accept ()
    {
        var superView = new Window
        {
            Id = "superView"
        };

        var tf = new TextField
        {
            Id = "tf"
        };

        var button = new Button
        {
            Id = "button",
            IsDefault = true
        };

        superView.Add (tf, button);

        var buttonAccept = 0;
        button.Accepting += ButtonAccept;

        tf.SetFocus ();
        Assert.True (tf.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (1, buttonAccept);

        button.SetFocus ();
        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (2, buttonAccept);

        return;

        void ButtonAccept (object sender, CommandEventArgs e) { buttonAccept++; }
    }

    [Fact]
    public void Accepted_Cancel_Event_HandlesCommand ()
    {
        //var super = new View ();
        var view = new TextField ();

        //super.Add (view);

        //var superAcceptedInvoked = false;

        var tfAcceptedInvoked = false;
        var handle = false;
        view.Accepting += TextViewAccept;
        Assert.False (view.InvokeCommand (Command.Accept));
        Assert.True (tfAcceptedInvoked);

        tfAcceptedInvoked = false;
        handle = true;
        view.Accepting += TextViewAccept;
        Assert.True (view.InvokeCommand (Command.Accept));
        Assert.True (tfAcceptedInvoked);

        return;

        void TextViewAccept (object sender, CommandEventArgs e)
        {
            tfAcceptedInvoked = true;
            e.Handled = handle;
        }
    }

    [Fact]
    public void OnEnter_Does_Not_Throw_If_Not_IsInitialized_SetCursorVisibility ()
    {
        var top = new Toplevel ();
        var tf = new TextField { Width = 10 };
        top.Add (tf);

        Exception exception = Record.Exception (() => tf.SetFocus ());
        Assert.Null (exception);
    }

    [Fact]
    public void Backspace_From_End ()
    {
        var tf = new TextField { Text = "ABC" };
        tf.SetFocus ();
        Assert.Equal ("ABC", tf.Text);
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (3, tf.CursorPosition);

        // now delete the C
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("AB", tf.Text);
        Assert.Equal (2, tf.CursorPosition);

        // then delete the B
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("A", tf.Text);
        Assert.Equal (1, tf.CursorPosition);

        // then delete the A
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("", tf.Text);
        Assert.Equal (0, tf.CursorPosition);
    }

    [Fact]
    public void Backspace_From_Middle ()
    {
        var tf = new TextField { Text = "ABC" };
        tf.SetFocus ();
        tf.CursorPosition = 2;
        Assert.Equal ("ABC", tf.Text);

        // now delete the B
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("AC", tf.Text);

        // then delete the A
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("C", tf.Text);

        // then delete nothing
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("C", tf.Text);

        // now delete the C
        tf.CursorPosition = 1;
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("", tf.Text);
    }

    [Fact]
    public void KeyDown_Handled_Prevents_Input ()
    {
        var tf = new TextField ();
        tf.KeyDown += HandleJKey;

        tf.NewKeyDownEvent (Key.A);
        Assert.Equal ("a", tf.Text);

        // SuppressKey suppresses the 'j' key
        tf.NewKeyDownEvent (Key.J);
        Assert.Equal ("a", tf.Text);

        tf.KeyDown -= HandleJKey;

        // Now that the delegate has been removed we can type j again
        tf.NewKeyDownEvent (Key.J);
        Assert.Equal ("aj", tf.Text);

        return;

        void HandleJKey (object s, Key arg)
        {
            if (arg.AsRune == new Rune ('j'))
            {
                arg.Handled = true;
            }
        }
    }

    [InlineData ("a")] // Lower than selection
    [InlineData ("aaaaaaaaaaa")] // Greater than selection
    [InlineData ("aaaa")] // Equal than selection
    [Theory]
    public void SetTextAndMoveCursorToEnd_WhenExistingSelection (string newText)
    {
        var tf = new TextField ();
        tf.Text = "fish";
        tf.CursorPosition = tf.Text.Length;

        tf.NewKeyDownEvent (Key.CursorLeft);

        tf.NewKeyDownEvent (Key.CursorLeft.WithShift);
        tf.NewKeyDownEvent (Key.CursorLeft.WithShift);

        Assert.Equal (1, tf.CursorPosition);
        Assert.Equal (2, tf.SelectedLength);
        Assert.Equal ("is", tf.SelectedText);

        tf.Text = newText;
        tf.CursorPosition = tf.Text.Length;

        Assert.Equal (newText.Length, tf.CursorPosition);
        Assert.Equal (0, tf.SelectedLength);
        Assert.Null (tf.SelectedText);
    }

    [Fact]
    public void SpaceHandling ()
    {
        var tf = new TextField { Width = 10, Text = " " };

        var ev = new MouseEventArgs { Position = new (0, 0), Flags = MouseFlags.Button1DoubleClicked };

        tf.NewMouseEvent (ev);
        Assert.Equal (1, tf.SelectedLength);

        ev = new () { Position = new (1, 0), Flags = MouseFlags.Button1DoubleClicked };

        tf.NewMouseEvent (ev);
        Assert.Equal (1, tf.SelectedLength);
    }

    [Fact]
    public void WordBackward_WordForward_Mixed ()
    {
        var tf = new TextField { Width = 30, Text = "Test with0. and!.?;-@+" };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.False (tf.UseSameRuneTypeForWords);
        Assert.Equal (22, tf.CursorPosition);

        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (15, tf.CursorPosition);
        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (12, tf.CursorPosition);
        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (10, tf.CursorPosition);
        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (5, tf.CursorPosition);
        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (0, tf.CursorPosition);

        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (5, tf.CursorPosition);
        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (10, tf.CursorPosition);
        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (12, tf.CursorPosition);
        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (15, tf.CursorPosition);
        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (22, tf.CursorPosition);
    }

    [Fact]
    public void WordBackward_WordForward_SelectedText_With_Accent ()
    {
        var text = "Les Misérables movie.";
        var tf = new TextField { Width = 30, Text = text };

        Assert.Equal (21, text.Length);
        Assert.Equal (21, tf.Text.GetRuneCount ());
        Assert.Equal (21, tf.Text.GetColumns ());

        List<Rune> runes = tf.Text.ToRuneList ();
        Assert.Equal (21, runes.Count);
        Assert.Equal (21, tf.Text.Length);

        for (var i = 0; i < runes.Count; i++)
        {
            char cs = text [i];
            var cus = (char)runes [i].Value;
            Assert.Equal (cs, cus);
        }

        var idx = 15;
        Assert.Equal ('m', text [idx]);
        Assert.Equal ('m', (char)runes [idx].Value);
        Assert.Equal ("m", runes [idx].ToString ());

        Assert.True (
                     tf.NewMouseEvent (
                                       new () { Position = new (idx, 1), Flags = MouseFlags.Button1DoubleClicked, View = tf }
                                      )
                    );
        Assert.Equal ("movie", tf.SelectedText);

        Assert.True (
                     tf.NewMouseEvent (
                                       new () { Position = new (idx + 1, 1), Flags = MouseFlags.Button1DoubleClicked, View = tf }
                                      )
                    );
        Assert.Equal ("movie", tf.SelectedText);
    }

    [Fact]
    public void Autocomplete_Popup_Added_To_SuperView_On_Init ()
    {
        View superView = new ()
        {
            CanFocus = true
        };

        TextField t = new ();

        superView.Add (t);
        Assert.Single (superView.SubViews);

        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Autocomplete__Added_To_SuperView_On_Add ()
    {
        View superView = new ()
        {
            CanFocus = true,
            Id = "superView"
        };

        superView.BeginInit ();
        superView.EndInit ();
        Assert.Empty (superView.SubViews);

        TextField t = new ()
        {
            Id = "t"
        };

        superView.Add (t);

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Right_CursorAtEnd_WithSelection_ShouldClearSelection ()
    {
        var tf = new TextField
        {
            Text = "Hello"
        };
        tf.SetFocus ();
        tf.SelectAll ();
        tf.CursorPosition = 5;

        // When there is selected text and the cursor is at the end of the text field
        Assert.Equal ("Hello", tf.SelectedText);

        // Pressing right should not move focus, instead it should clear selection
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Null (tf.SelectedText);

        // Now that the selection is cleared another right keypress should move focus
        Assert.False (tf.NewKeyDownEvent (Key.CursorRight));
    }

    [Fact]
    public void Left_CursorAtStart_WithSelection_ShouldClearSelection ()
    {
        var tf = new TextField
        {
            Text = "Hello"
        };
        tf.SetFocus ();

        tf.CursorPosition = 2;
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithShift));
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithShift));

        // When there is selected text and the cursor is at the start of the text field
        Assert.Equal ("He", tf.SelectedText);

        // Pressing left should not move focus, instead it should clear selection
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft));
        Assert.Null (tf.SelectedText);

        // When clearing selected text with left the cursor should be at the start of the selection
        Assert.Equal (0, tf.CursorPosition);

        // Now that the selection is cleared another left keypress should move focus
        Assert.False (tf.NewKeyDownEvent (Key.CursorLeft));
    }

    [Fact]
    public void Autocomplete_Visible_False_By_Default ()
    {
        View superView = new ()
        {
            CanFocus = true
        };

        TextField t = new ();

        superView.Add (t);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (2, superView.SubViews.Count);

        Assert.True (t.Visible);
        Assert.False (t.Autocomplete.Visible);
    }

    [Fact]
    public void InsertText_Bmp_SurrogatePair_Non_Bmp_Invalid_SurrogatePair ()
    {
        var tf = new TextField ();

        //📄 == \ud83d\udcc4 == \U0001F4C4
        // � == Rune.ReplacementChar
        tf.InsertText ("aA,;\ud83d\udcc4\U0001F4C4\udcc4\ud83d");
        Assert.Equal ("aA,;📄📄��", tf.Text);
    }

    [Fact]
    public void PositionCursor_Respect_GetColumns ()
    {
        var tf = new TextField { Width = 5 };
        tf.BeginInit ();
        tf.EndInit ();

        tf.NewKeyDownEvent (new ("📄"));
        Assert.Equal (1, tf.CursorPosition);
        Assert.Equal (new (2, 0), tf.PositionCursor ());
        Assert.Equal ("📄", tf.Text);

        tf.NewKeyDownEvent (new (KeyCode.A));
        Assert.Equal (2, tf.CursorPosition);
        Assert.Equal (new (3, 0), tf.PositionCursor ());
        Assert.Equal ("📄a", tf.Text);
    }
}
