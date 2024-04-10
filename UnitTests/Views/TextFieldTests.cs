﻿using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using CsvHelper;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class TextFieldTests
{
    private static TextField _textField;
    private readonly ITestOutputHelper _output;
    public TextFieldTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void Accented_Letter_With_Three_Combining_Unicode_Chars ()
    {
        var tf = new TextField { Width = 3, Text = "ắ" };
        Toplevel top = new ();
        top.Add (tf);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
ắ",
                                                      _output
                                                     );

        tf.Text = "\u1eaf";
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
ắ",
                                                      _output
                                                     );

        tf.Text = "\u0103\u0301";
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
ắ",
                                                      _output
                                                     );

        tf.Text = "\u0061\u0306\u0301";
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
ắ",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void Adjust_First ()
    {
        var tf = new TextField { Width = Dim.Fill (), Text = "This is a test." };
        var top = new Toplevel ();
        top.Add (tf);
        Application.Begin (top);

        Assert.Equal ("This is a test. ", GetContents ());

        string GetContents ()
        {
            var item = "";

            for (var i = 0; i < 16; i++)
            {
                item += Application.Driver.Contents [0, i].Rune;
            }

            return item;
        }
    }

    [Fact]
    public void Cancel_TextChanging_ThenBackspace ()
    {
        var tf = new TextField ();
        tf.EnsureFocus ();
        tf.NewKeyDownEvent (Key.A.WithShift);
        Assert.Equal ("A", tf.Text);

        // cancel the next keystroke
        tf.TextChanging += (s, e) => e.Cancel = e.NewValue == "AB";
        tf.NewKeyDownEvent (Key.B.WithShift);

        // B was canceled so should just be A
        Assert.Equal ("A", tf.Text);

        // now delete the A
        tf.NewKeyDownEvent (Key.Backspace);

        Assert.Equal ("", tf.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void CanFocus_False_Wont_Focus_With_Mouse ()
    {
        Toplevel top = new ();
        var tf = new TextField { Width = Dim.Fill (), CanFocus = false, ReadOnly = true, Text = "some text" };

        var fv = new FrameView
        {
            Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = false, Title = "I shouldn't get focus"
        };
        fv.Add (tf);
        top.Add (fv);

        Application.Begin (top);

        Assert.False (tf.CanFocus);
        Assert.False (tf.HasFocus);
        Assert.False (fv.CanFocus);
        Assert.False (fv.HasFocus);

        tf.NewMouseEvent (
                       new MouseEvent { X = 1, Y = 0, Flags = MouseFlags.Button1DoubleClicked }
                      );

        Assert.Null (tf.SelectedText);
        Assert.False (tf.CanFocus);
        Assert.False (tf.HasFocus);
        Assert.False (fv.CanFocus);
        Assert.False (fv.HasFocus);

        Assert.Throws<InvalidOperationException> (() => tf.CanFocus = true);
        fv.CanFocus = true;
        tf.CanFocus = true;

        tf.NewMouseEvent (
                       new MouseEvent { X = 1, Y = 0, Flags = MouseFlags.Button1DoubleClicked }
                      );

        Assert.Equal ("some ", tf.SelectedText);
        Assert.True (tf.CanFocus);
        Assert.True (tf.HasFocus);
        Assert.True (fv.CanFocus);
        Assert.True (fv.HasFocus);

        fv.CanFocus = false;

        tf.NewMouseEvent (
                       new MouseEvent { X = 1, Y = 0, Flags = MouseFlags.Button1DoubleClicked }
                      );

        Assert.Equal ("some ", tf.SelectedText); // Setting CanFocus to false don't change the SelectedText
        Assert.False (tf.CanFocus);
        Assert.False (tf.HasFocus);
        Assert.False (fv.CanFocus);
        Assert.False (fv.HasFocus);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData ("0123456789", "0123456789")]
    [InlineData ("01234567890", "0123456789")]
    public void CaptionedTextField_DoesNotOverspillBounds (string caption, string expectedRender)
    {
        TextField tf = GetTextFieldsInView ();

        // Caption has no effect when focused
        tf.Caption = caption;
        Application.Driver.SendKeys ('\t', ConsoleKey.Tab, false, false, false);
        Assert.False (tf.HasFocus);

        tf.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedRender, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void CaptionedTextField_DoesNotOverspillBounds_Unicode ()
    {
        string caption = "Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables";

        Assert.Equal (11, caption.Length);
        Assert.Equal (10, caption.EnumerateRunes ().Sum (c => c.GetColumns ()));

        TextField tf = GetTextFieldsInView ();

        tf.Caption = caption;
        Application.Driver.SendKeys ('\t', ConsoleKey.Tab, false, false, false);
        Assert.False (tf.HasFocus);

        tf.Draw ();
        TestHelpers.AssertDriverContentsAre ("Misérables", _output);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData ("blah")]
    [InlineData (" ")]
    public void CaptionedTextField_DoNotRenderCaption_WhenTextPresent (string content)
    {
        TextField tf = GetTextFieldsInView ();

        tf.Draw ();
        TestHelpers.AssertDriverContentsAre ("", _output);

        tf.Caption = "Enter txt";
        Application.Driver.SendKeys ('\t', ConsoleKey.Tab, false, false, false);

        // Caption should appear when not focused and no text
        Assert.False (tf.HasFocus);
        tf.Draw ();
        TestHelpers.AssertDriverContentsAre ("Enter txt", _output);

        // but disapear when text is added
        tf.Text = content;
        tf.Draw ();
        TestHelpers.AssertDriverContentsAre (content, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void CaptionedTextField_RendersCaption_WhenNotFocused ()
    {
        TextField tf = GetTextFieldsInView ();

        tf.Draw ();
        TestHelpers.AssertDriverContentsAre ("", _output);

        // Caption has no effect when focused
        tf.Caption = "Enter txt";
        Assert.True (tf.HasFocus);
        tf.Draw ();
        TestHelpers.AssertDriverContentsAre ("", _output);

        Application.Driver.SendKeys ('\t', ConsoleKey.Tab, false, false, false);

        Assert.False (tf.HasFocus);
        tf.Draw ();
        TestHelpers.AssertDriverContentsAre ("Enter txt", _output);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Changing_SelectedStart_Or_CursorPosition_Update_SelectedLength_And_SelectedText ()
    {
        _textField.BeginInit ();
        _textField.EndInit ();
        _textField.SelectedStart = 2;
        Assert.Equal (32, _textField.CursorPosition);
        Assert.Equal (30, _textField.SelectedLength);
        Assert.Equal ("B to jump between text fields.", _textField.SelectedText);
        _textField.CursorPosition = 20;
        Assert.Equal (2, _textField.SelectedStart);
        Assert.Equal (18, _textField.SelectedLength);
        Assert.Equal ("B to jump between ", _textField.SelectedText);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Copy_Or_Cut__Not_Allowed_If_Secret_Is_True ()
    {
        _textField.Secret = true;
        _textField.SelectedStart = 20;
        _textField.CursorPosition = 24;
        _textField.Copy ();
        Assert.Null (_textField.SelectedText);
        _textField.Cut ();
        Assert.Null (_textField.SelectedText);
        _textField.Secret = false;
        _textField.Copy ();
        Assert.Equal ("text", _textField.SelectedText);
        _textField.Cut ();
        Assert.Null (_textField.SelectedText);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Copy_Or_Cut_And_Paste_With_No_Selection ()
    {
        _textField.SelectedStart = 20;
        _textField.CursorPosition = 24;
        _textField.Copy ();
        Assert.Equal ("text", _textField.SelectedText);
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        _textField.SelectedStart = -1;
        _textField.Paste ();
        Assert.Equal ("TAB to jump between texttext fields.", _textField.Text);
        _textField.SelectedStart = 24;
        _textField.Cut ();
        Assert.Null (_textField.SelectedText);
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        _textField.SelectedStart = -1;
        _textField.Paste ();
        Assert.Equal ("TAB to jump between texttext fields.", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Copy_Or_Cut_And_Paste_With_Selection ()
    {
        _textField.SelectedStart = 20;
        _textField.CursorPosition = 24;
        _textField.Copy ();
        Assert.Equal ("text", _textField.SelectedText);
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        _textField.Paste ();
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        _textField.SelectedStart = 20;
        _textField.Cut ();
        _textField.Paste ();
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Copy_Or_Cut_Not_Null_If_Has_Selection ()
    {
        _textField.SelectedStart = 20;
        _textField.CursorPosition = 24;
        _textField.Copy ();
        Assert.Equal ("text", _textField.SelectedText);
        _textField.Cut ();
        Assert.Null (_textField.SelectedText);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Copy_Or_Cut_Null_If_No_Selection ()
    {
        _textField.SelectedStart = -1;
        _textField.Copy ();
        Assert.Null (_textField.SelectedText);
        _textField.Cut ();
        Assert.Null (_textField.SelectedText);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Copy_Paste_Surrogate_Pairs ()
    {
        _textField.Text = "TextField with some more test text. Unicode shouldn't 𝔹Aℝ𝔽!";
        _textField.SelectAll ();
        _textField.Cut ();

        Assert.Equal (
                      "TextField with some more test text. Unicode shouldn't 𝔹Aℝ𝔽!",
                      Application.Driver.Clipboard.GetClipboardData ()
                     );
        Assert.Equal (string.Empty, _textField.Text);
        _textField.Paste ();
        Assert.Equal ("TextField with some more test text. Unicode shouldn't 𝔹Aℝ𝔽!", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Copy_Paste_Text_Changing_Updates_Cursor_Position ()
    {
        _textField.BeginInit ();
        _textField.EndInit ();

        _textField.TextChanging += _textField_TextChanging;

        void _textField_TextChanging (object sender, StateEventArgs<string> e)
        {
            if (e.NewValue.GetRuneCount () > 11)
            {
                e.NewValue = e.NewValue [..11];
            }
        }

        Assert.Equal (32, _textField.CursorPosition);
        _textField.SelectAll ();
        _textField.Cut ();
        Assert.Equal ("TAB to jump between text fields.", Application.Driver.Clipboard.GetClipboardData ());
        Assert.Equal (string.Empty, _textField.Text);
        Assert.Equal (0, _textField.CursorPosition);
        _textField.Paste ();
        Assert.Equal ("TAB to jump", _textField.Text);
        Assert.Equal (11, _textField.CursorPosition);

        _textField.TextChanging -= _textField_TextChanging;
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Cursor_Position_Initialization ()
    {
        Assert.False (_textField.IsInitialized);

        // BUGBUG: IsInitialized is false and
        // CursorPosition wasn't calculated yet
        Assert.Equal (0, _textField.CursorPosition);
        _textField.BeginInit ();
        _textField.EndInit ();
        Assert.Equal (32, _textField.CursorPosition);
        Assert.Equal (0, _textField.SelectedLength);
        Assert.Null (_textField.SelectedText);
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void CursorPosition_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
    {
        _textField.CursorPosition = 33;
        Assert.Equal (32, _textField.CursorPosition);
        Assert.Equal (0, _textField.SelectedLength);
        Assert.Null (_textField.SelectedText);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void CursorPosition_With_Value_Less_Than_Zero_Changes_To_Zero ()
    {
        _textField.CursorPosition = -1;
        Assert.Equal (0, _textField.CursorPosition);
        Assert.Equal (0, _textField.SelectedLength);
        Assert.Null (_textField.SelectedText);
    }

    [Fact]
    [AutoInitShutdown]
    public void DeleteSelectedText_InsertText_DeleteCharLeft_DeleteCharRight_Cut ()
    {
        var newText = "";
        var oldText = "";
        var tf = new TextField { Width = 10, Text = "-1" };

        tf.TextChanging += (s, e) => newText = e.NewValue;
        tf.TextChanged += (s, e) => oldText = e.OldValue;

        var top = new Toplevel ();
        top.Add (tf);
        Application.Begin (top);

        Assert.Equal ("-1", tf.Text);

        // InsertText
        tf.SelectedStart = 1;
        tf.CursorPosition = 2;
        Assert.Equal (1, tf.SelectedLength);
        Assert.Equal ("1", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.D2));
        Assert.Equal ("-2", newText);
        Assert.Equal ("-1", oldText);
        Assert.Equal ("-2", tf.Text);

        // DeleteCharLeft
        tf.SelectedStart = 1;
        tf.CursorPosition = 2;
        Assert.Equal (1, tf.SelectedLength);
        Assert.Equal ("2", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("-", newText);
        Assert.Equal ("-2", oldText);
        Assert.Equal ("-", tf.Text);

        // DeleteCharRight
        tf.Text = "-1";
        tf.SelectedStart = 1;
        tf.CursorPosition = 2;
        Assert.Equal (1, tf.SelectedLength);
        Assert.Equal ("1", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.Delete));
        Assert.Equal ("-", newText);
        Assert.Equal ("-1", oldText);
        Assert.Equal ("-", tf.Text);

        // Cut
        tf.Text = "-1";
        tf.SelectedStart = 1;
        tf.CursorPosition = 2;
        Assert.Equal (1, tf.SelectedLength);
        Assert.Equal ("1", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.X.WithCtrl));
        Assert.Equal ("-", newText);
        Assert.Equal ("-1", oldText);
        Assert.Equal ("-", tf.Text);

        // Delete word with accented char
        tf.Text = "Les Misérables movie.";

        Assert.True (
                     tf.NewMouseEvent (
                                    new MouseEvent { X = 7, Y = 1, Flags = MouseFlags.Button1DoubleClicked, View = tf }
                                   )
                    );
        Assert.Equal ("Misérables ", tf.SelectedText);
        Assert.Equal (11, tf.SelectedLength);
        Assert.True (tf.NewKeyDownEvent (Key.Delete));
        Assert.Equal ("Les movie.", newText);
        Assert.Equal ("Les Misérables movie.", oldText);
        Assert.Equal ("Les movie.", tf.Text);
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
    [AutoInitShutdown (useFakeClipboard: true)]
    public void KeyBindings_Command ()
    {
        var tf = new TextField { Width = 20, Text = "This is a test." };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (15, tf.Text.Length);
        Assert.Equal (15, tf.CursorPosition);
        Assert.False (tf.ReadOnly);

        Assert.True (tf.NewKeyDownEvent (Key.Delete));
        Assert.Equal ("This is a test.", tf.Text);
        tf.CursorPosition = 0;
        Assert.True (tf.NewKeyDownEvent (Key.Delete));
        Assert.Equal ("his is a test.", tf.Text);
        tf.ReadOnly = true;
        Assert.True (tf.NewKeyDownEvent (Key.D.WithCtrl));
        Assert.Equal ("his is a test.", tf.Text);
        Assert.True (tf.NewKeyDownEvent (Key.Delete));
        Assert.Equal ("his is a test.", tf.Text);
        tf.ReadOnly = false;
        tf.CursorPosition = 1;
        Assert.True (tf.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("is is a test.", tf.Text);
        tf.CursorPosition = 5;
        Assert.True (tf.NewKeyDownEvent (Key.Home.WithShift));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is is", tf.SelectedText);
        tf.CursorPosition = 5;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.Home.WithShift.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is is", tf.SelectedText);
        tf.CursorPosition = 5;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.A.WithShift.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is is", tf.SelectedText);
        tf.CursorPosition = 5;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.End.WithShift));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (" a test.", tf.SelectedText);
        tf.CursorPosition = 5;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.End.WithShift.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (" a test.", tf.SelectedText);
        tf.CursorPosition = 5;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.E.WithShift.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (" a test.", tf.SelectedText);
        tf.CursorPosition = 5;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.Home));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (0, tf.CursorPosition);
        tf.CursorPosition = 5;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.Home.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (0, tf.CursorPosition);
        tf.CursorPosition = 5;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.A.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (0, tf.CursorPosition);
        tf.CursorPosition = 5;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithShift));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("s", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.CursorUp.WithShift));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("s", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.CursorDown.WithShift));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Null (tf.SelectedText);
        tf.CursorPosition = 7;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithShift.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("a", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.CursorUp.WithShift.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is a", tf.SelectedText);
#if UNIX_KEY_BINDINGS
        Assert.True (tf.NewKeyDownEvent (Key.B.WithShift.WithAlt));
#else
        Assert.True (tf.NewKeyDownEvent (Key.CursorUp.WithShift.WithCtrl));
#endif
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is is a", tf.SelectedText);
        tf.CursorPosition = 3;
        tf.SelectedStart = -1;
        Assert.Null (tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight.WithShift.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is ", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.CursorDown.WithShift.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is a ", tf.SelectedText);
#if UNIX_KEY_BINDINGS
        Assert.True (tf.NewKeyDownEvent (Key.F.WithShift.WithAlt));
#else
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight.WithShift.WithCtrl));
#endif
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal ("is a test.", tf.SelectedText);
        Assert.Equal (13, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Null (tf.SelectedText);
        Assert.Equal (12, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (11, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.End));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (13, tf.CursorPosition);
        tf.CursorPosition = 0;
        Assert.True (tf.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (13, tf.CursorPosition);
        tf.CursorPosition = 0;
        Assert.True (tf.NewKeyDownEvent (Key.E.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (13, tf.CursorPosition);
        tf.CursorPosition = 0;
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (1, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.F.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.Equal (2, tf.CursorPosition);
        tf.CursorPosition = 9;
        tf.ReadOnly = true;
        Assert.True (tf.NewKeyDownEvent (Key.K.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        tf.ReadOnly = false;
        Assert.True (tf.NewKeyDownEvent (Key.K.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal ("est.", Clipboard.Contents);
        Assert.True (tf.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("is is a test.", tf.Text);
        Assert.True (tf.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
#if UNIX_KEY_BINDINGS
        Assert.True (tf.NewKeyDownEvent (Key.Backspace.WithAlt));
#else
        Assert.True (tf.NewKeyDownEvent (Key.Z.WithCtrl));
#endif
        Assert.Equal ("is is a test.", tf.Text);
        Assert.True (tf.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal (8, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorUp.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal (6, tf.CursorPosition);
#if UNIX_KEY_BINDINGS
        Assert.True (tf.NewKeyDownEvent (Key.B.WithAlt));
#else
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl));
#endif
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal (3, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal (6, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.CursorDown.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal (8, tf.CursorPosition);
#if UNIX_KEY_BINDINGS
        Assert.True (tf.NewKeyDownEvent (Key.F.WithAlt));
#else
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight.WithCtrl));
#endif
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal (9, tf.CursorPosition);
        Assert.True (tf.Used);
        Assert.True (tf.NewKeyDownEvent (Key.InsertChar));
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal (9, tf.CursorPosition);
        Assert.False (tf.Used);
        tf.SelectedStart = 3;
        tf.CursorPosition = 7;
        Assert.Equal ("is a", tf.SelectedText);
        Assert.Equal ("est.", Clipboard.Contents);
        Assert.True (tf.NewKeyDownEvent (Key.C.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal ("is a", Clipboard.Contents);
        Assert.True (tf.NewKeyDownEvent (Key.X.WithCtrl));
        Assert.Equal ("is  t", tf.Text);
        Assert.Equal ("is a", Clipboard.Contents);
        Assert.True (tf.NewKeyDownEvent (Key.V.WithCtrl));
        Assert.Equal ("is is a t", tf.Text);
        Assert.Equal ("is a", Clipboard.Contents);
        Assert.Equal (7, tf.CursorPosition);
#if UNIX_KEY_BINDINGS
        Assert.True (tf.NewKeyDownEvent (Key.K.WithAlt));
#else
        Assert.True (tf.NewKeyDownEvent (Key.K.WithCtrl.WithShift));
#endif
        Assert.Equal (" t", tf.Text);
        Assert.Equal ("is is a", Clipboard.Contents);
        tf.Text = "TAB to jump between text fields.";
        Assert.Equal (0, tf.CursorPosition);
        Assert.True (tf.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("to jump between text fields.", tf.Text);
        tf.CursorPosition = tf.Text.Length;
        Assert.True (tf.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("to jump between text fields", tf.Text);
        Assert.True (tf.NewKeyDownEvent (Key.T.WithCtrl));
        Assert.Equal ("to jump between text fields", tf.SelectedText);
        Assert.True (tf.NewKeyDownEvent (Key.D.WithCtrl.WithShift));
        Assert.Equal ("", tf.Text);
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
        view.Accept += OnAccept;
        view.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;
        void OnAccept (object sender, CancelEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Command_Fires_Accept ()
    {
        var view = new TextField ();

        var accepted = false;
        view.Accept += Accept;
        view.InvokeCommand (Command.Accept);
        Assert.True (accepted);

        return;

        void Accept (object sender, CancelEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Cancel_Event_HandlesCommand ()
    {
        //var super = new View ();
        var view = new TextField ();
        //super.Add (view);

        //var superAcceptedInvoked = false;

        var tfAcceptedInvoked = false;
        var cancel = false;
        view.Accept += TextViewAccept;
        Assert.True (view.InvokeCommand (Command.Accept));
        Assert.True (tfAcceptedInvoked);

        tfAcceptedInvoked = false;
        cancel = true;
        view.Accept += TextViewAccept;
        Assert.False (view.InvokeCommand (Command.Accept));
        Assert.True (tfAcceptedInvoked);

        return;

        void TextViewAccept (object sender, CancelEventArgs e)
        {
            tfAcceptedInvoked = true;
            e.Cancel = cancel;
        }
    }

    [Fact]
    public void OnEnter_Does_Not_Throw_If_Not_IsInitialized_SetCursorVisibility ()
    {
        var top = new Toplevel ();
        var tf = new TextField { Width = 10 };
        top.Add (tf);

        Exception exception = Record.Exception (tf.SetFocus);
        Assert.Null (exception);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Paste_Always_Clear_The_SelectedText ()
    {
        _textField.SelectedStart = 20;
        _textField.CursorPosition = 24;
        _textField.Copy ();
        Assert.Equal ("text", _textField.SelectedText);
        _textField.Paste ();
        Assert.Null (_textField.SelectedText);
    }

    [Fact]
    public void Backspace_From_End ()
    {
        var tf = new TextField { Text = "ABC" };
        tf.EnsureFocus ();
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
        tf.EnsureFocus ();
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
    [AutoInitShutdown]
    public void ScrollOffset_Initialize ()
    {
        var tf = new TextField { X = 1, Y = 1, Width = 20, Text = "Testing Scrolls." };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (0, tf.ScrollOffset);
        Assert.Equal (16, tf.CursorPosition);

        var top = new Toplevel ();
        top.Add (tf);
        Application.Begin (top);

        Assert.Equal (0, tf.ScrollOffset);
        Assert.Equal (16, tf.CursorPosition);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Selected_Text_Shows ()
    {
        // Proves #3022 is fixed (TextField selected text does not show in v2)

        _textField.CursorPosition = 0;
        var top = new Toplevel ();
        top.Add (_textField);
        RunState rs = Application.Begin (top);

        Attribute [] attributes =
        {
            _textField.ColorScheme.Focus,
            new (
                 _textField.ColorScheme.Focus.Background,
                 _textField.ColorScheme.Focus.Foreground
                )
        };

        //                                             TAB to jump between text fields.
        TestHelpers.AssertDriverAttributesAre ("0000000", Application.Driver, attributes);

        // Cursor is at the end
        Assert.Equal (32, _textField.CursorPosition);
        _textField.CursorPosition = 0;
        _textField.NewKeyDownEvent (Key.CursorRight.WithCtrl.WithShift);

        var first = true;
        Application.RunIteration (ref rs, ref first);
        Assert.Equal (4, _textField.CursorPosition);

        //                                             TAB to jump between text fields.
        TestHelpers.AssertDriverAttributesAre ("1111000", Application.Driver, attributes);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void SelectedStart_And_CursorPosition_With_Value_Greater_Than_Text_Length_Changes_Both_To_Text_Length ()
    {
        _textField.CursorPosition = 33;
        _textField.SelectedStart = 33;
        Assert.Equal (32, _textField.CursorPosition);
        Assert.Equal (32, _textField.SelectedStart);
        Assert.Equal (0, _textField.SelectedLength);
        Assert.Null (_textField.SelectedText);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void SelectedStart_Greater_Than_CursorPosition_All_Selection_Is_Overwritten_On_Typing ()
    {
        _textField.SelectedStart = 19;
        _textField.CursorPosition = 12;
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.U); // u
        Assert.Equal ("TAB to jump u text fields.", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void SelectedStart_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
    {
        _textField.CursorPosition = 2;
        _textField.SelectedStart = 33;
        Assert.Equal (32, _textField.SelectedStart);
        Assert.Equal (30, _textField.SelectedLength);
        Assert.Equal ("B to jump between text fields.", _textField.SelectedText);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void SelectedStart_With_Value_Less_Than_Minus_One_Changes_To_Minus_One ()
    {
        _textField.SelectedStart = -2;
        Assert.Equal (-1, _textField.SelectedStart);
        Assert.Equal (0, _textField.SelectedLength);
        Assert.Null (_textField.SelectedText);
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

    [Fact]
    [AutoInitShutdown]
    public void MouseEvent_Handled_Prevents_RightClick ()
    {
        Application.MouseEvent += HandleRightClick;

        var tf = new TextField { Width = 10 };
        var clickCounter = 0;
        tf.MouseClick += (s, m) => { clickCounter++; };

        var top = new Toplevel ();
        top.Add (tf);
        Application.Begin (top);

        var mouseEvent = new MouseEvent { Flags = MouseFlags.Button1Clicked, View = tf };

        Application.OnMouseEvent (mouseEvent);
        Assert.Equal (1, clickCounter);

        // Get a fresh instance that represents a right click.
        // Should be ignored because of SuppressRightClick callback
        mouseEvent = new MouseEvent { Flags = MouseFlags.Button3Clicked, View = tf };
        Application.OnMouseEvent (mouseEvent);
        Assert.Equal (1, clickCounter);

        Application.MouseEvent -= HandleRightClick;

        // Get a fresh instance that represents a right click.
        // Should no longer be ignored as the callback was removed
        mouseEvent = new MouseEvent { Flags = MouseFlags.Button3Clicked, View = tf };

        // In #3183 OnMouseClicked is no longer called before MouseEvent().
        // This call causes the context menu to pop, and MouseEvent() returns true.
        // Thus, the clickCounter is NOT incremented.
        // Which is correct, because the user did NOT click with the left mouse button.
        Application.OnMouseEvent (mouseEvent);
        Assert.Equal (1, clickCounter);

        return;

        void HandleRightClick (object sender, MouseEvent arg)
        {
            if (arg.Flags.HasFlag (MouseFlags.Button3Clicked))
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
    [TextFieldTestsAutoInitShutdown]
    public void Text_Replaces_Tabs_With_Empty_String ()
    {
        _textField.Text = "\t\tTAB to jump between text fields.";
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        _textField.Text = "";
        Clipboard.Contents = "\t\tTAB to jump between text fields.";
        _textField.Paste ();
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void TextChanged_Event ()
    {
        _textField.TextChanged += (s, e) => { Assert.Equal ("TAB to jump between text fields.", e.OldValue); };

        _textField.Text = "changed";
        Assert.Equal ("changed", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void TextChanging_Event ()
    {
        var cancel = true;

        _textField.TextChanging += (s, e) =>
                                   {
                                       Assert.Equal ("changing", e.NewValue);

                                       if (cancel)
                                       {
                                           e.Cancel = true;
                                       }
                                   };

        _textField.Text = "changing";
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        cancel = false;
        _textField.Text = "changing";
        Assert.Equal ("changing", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void SpaceHandling ()
    {
        var tf = new TextField { Width = 10, Text = " " };

        var ev = new MouseEvent { X = 0, Y = 0, Flags = MouseFlags.Button1DoubleClicked };

        tf.NewMouseEvent (ev);
        Assert.Equal (1, tf.SelectedLength);

        ev = new MouseEvent { X = 1, Y = 0, Flags = MouseFlags.Button1DoubleClicked };

        tf.NewMouseEvent (ev);
        Assert.Equal (1, tf.SelectedLength);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Used_Is_False ()
    {
        _textField.Used = false;
        _textField.CursorPosition = 10;
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.U); // u
        Assert.Equal ("TAB to jumu between text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.S); // s
        Assert.Equal ("TAB to jumusbetween text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.E); // e
        Assert.Equal ("TAB to jumuseetween text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.D); // d
        Assert.Equal ("TAB to jumusedtween text fields.", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void Used_Is_True_By_Default ()
    {
        _textField.CursorPosition = 10;
        Assert.Equal ("TAB to jump between text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.U); // u
        Assert.Equal ("TAB to jumup between text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.S); // s
        Assert.Equal ("TAB to jumusp between text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.E); // e
        Assert.Equal ("TAB to jumusep between text fields.", _textField.Text);
        _textField.NewKeyDownEvent (Key.D); // d
        Assert.Equal ("TAB to jumusedp between text fields.", _textField.Text);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void WordBackward_With_No_Selection ()
    {
        _textField.CursorPosition = _textField.Text.Length;
        var iteration = 0;

        while (_textField.CursorPosition > 0)
        {
            _textField.NewKeyDownEvent (Key.CursorLeft.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (31, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 1:
                    Assert.Equal (25, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 2:
                    Assert.Equal (20, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 3:
                    Assert.Equal (12, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 4:
                    Assert.Equal (7, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 5:
                    Assert.Equal (4, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 6:
                    Assert.Equal (0, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void WordBackward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
    {
        //                           1         2         3         4         5    
        //                 0123456789012345678901234567890123456789012345678901234=55 (Length)
        _textField.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
        _textField.CursorPosition = _textField.Text.Length;
        var iteration = 0;

        while (_textField.CursorPosition > 0)
        {
            _textField.NewKeyDownEvent (Key.CursorLeft.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (54, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 1:
                    Assert.Equal (48, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 2:
                    Assert.Equal (46, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 3:
                    Assert.Equal (40, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 4:
                    Assert.Equal (38, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 5:
                    Assert.Equal (28, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 6:
                    Assert.Equal (25, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 7:
                    Assert.Equal (12, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 8:
                    Assert.Equal (9, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 9:
                    Assert.Equal (6, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 10:
                    Assert.Equal (0, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void WordBackward_With_Selection ()
    {
        _textField.CursorPosition = _textField.Text.Length;
        _textField.SelectedStart = _textField.Text.Length;
        var iteration = 0;

        while (_textField.CursorPosition > 0)
        {
            _textField.NewKeyDownEvent (Key.CursorLeft.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (31, _textField.CursorPosition);
                    Assert.Equal (32, _textField.SelectedStart);
                    Assert.Equal (1, _textField.SelectedLength);
                    Assert.Equal (".", _textField.SelectedText);

                    break;
                case 1:
                    Assert.Equal (25, _textField.CursorPosition);
                    Assert.Equal (32, _textField.SelectedStart);
                    Assert.Equal (7, _textField.SelectedLength);
                    Assert.Equal ("fields.", _textField.SelectedText);

                    break;
                case 2:
                    Assert.Equal (20, _textField.CursorPosition);
                    Assert.Equal (32, _textField.SelectedStart);
                    Assert.Equal (12, _textField.SelectedLength);
                    Assert.Equal ("text fields.", _textField.SelectedText);

                    break;
                case 3:
                    Assert.Equal (12, _textField.CursorPosition);
                    Assert.Equal (32, _textField.SelectedStart);
                    Assert.Equal (20, _textField.SelectedLength);
                    Assert.Equal ("between text fields.", _textField.SelectedText);

                    break;
                case 4:
                    Assert.Equal (7, _textField.CursorPosition);
                    Assert.Equal (32, _textField.SelectedStart);
                    Assert.Equal (25, _textField.SelectedLength);
                    Assert.Equal ("jump between text fields.", _textField.SelectedText);

                    break;
                case 5:
                    Assert.Equal (4, _textField.CursorPosition);
                    Assert.Equal (32, _textField.SelectedStart);
                    Assert.Equal (28, _textField.SelectedLength);
                    Assert.Equal ("to jump between text fields.", _textField.SelectedText);

                    break;
                case 6:
                    Assert.Equal (0, _textField.CursorPosition);
                    Assert.Equal (32, _textField.SelectedStart);
                    Assert.Equal (32, _textField.SelectedLength);
                    Assert.Equal ("TAB to jump between text fields.", _textField.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void
        WordBackward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
    {
        _textField.CursorPosition = 10;
        _textField.SelectedStart = 10;
        var iteration = 0;

        while (_textField.CursorPosition > 0)
        {
            _textField.NewKeyDownEvent (Key.CursorLeft.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (7, _textField.CursorPosition);
                    Assert.Equal (10, _textField.SelectedStart);
                    Assert.Equal (3, _textField.SelectedLength);
                    Assert.Equal ("jum", _textField.SelectedText);

                    break;
                case 1:
                    Assert.Equal (4, _textField.CursorPosition);
                    Assert.Equal (10, _textField.SelectedStart);
                    Assert.Equal (6, _textField.SelectedLength);
                    Assert.Equal ("to jum", _textField.SelectedText);

                    break;
                case 2:
                    Assert.Equal (0, _textField.CursorPosition);
                    Assert.Equal (10, _textField.SelectedStart);
                    Assert.Equal (10, _textField.SelectedLength);
                    Assert.Equal ("TAB to jum", _textField.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    public void WordBackward_WordForward_Mixed ()
    {
        var tf = new TextField { Width = 30, Text = "Test with0. and!.?;-@+" };
        tf.BeginInit ();
        tf.EndInit ();

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
                                    new MouseEvent { X = idx, Y = 1, Flags = MouseFlags.Button1DoubleClicked, View = tf }
                                   )
                    );
        Assert.Equal ("movie.", tf.SelectedText);

        Assert.True (
                     tf.NewMouseEvent (
                                    new MouseEvent { X = idx + 1, Y = 1, Flags = MouseFlags.Button1DoubleClicked, View = tf }
                                   )
                    );
        Assert.Equal ("movie.", tf.SelectedText);
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void WordForward_With_No_Selection ()
    {
        _textField.CursorPosition = 0;
        var iteration = 0;

        while (_textField.CursorPosition < _textField.Text.Length)
        {
            _textField.NewKeyDownEvent (Key.CursorRight.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (4, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 1:
                    Assert.Equal (7, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 2:
                    Assert.Equal (12, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 3:
                    Assert.Equal (20, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 4:
                    Assert.Equal (25, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 5:
                    Assert.Equal (32, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void WordForward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
    {
        //                           1         2         3         4         5    
        //                 0123456789012345678901234567890123456789012345678901234=55 (Length)
        _textField.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
        _textField.CursorPosition = 0;
        var iteration = 0;

        while (_textField.CursorPosition < _textField.Text.Length)
        {
            _textField.NewKeyDownEvent (Key.CursorRight.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (6, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 1:
                    Assert.Equal (9, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 2:
                    Assert.Equal (12, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 3:
                    Assert.Equal (25, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 4:
                    Assert.Equal (28, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 5:
                    Assert.Equal (38, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 6:
                    Assert.Equal (40, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 7:
                    Assert.Equal (46, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 8:
                    Assert.Equal (48, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 9:
                    Assert.Equal (54, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
                case 10:
                    Assert.Equal (55, _textField.CursorPosition);
                    Assert.Equal (-1, _textField.SelectedStart);
                    Assert.Equal (0, _textField.SelectedLength);
                    Assert.Null (_textField.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void WordForward_With_Selection ()
    {
        _textField.CursorPosition = 0;
        _textField.SelectedStart = 0;
        var iteration = 0;

        while (_textField.CursorPosition < _textField.Text.Length)
        {
            _textField.NewKeyDownEvent (Key.CursorRight.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (4, _textField.CursorPosition);
                    Assert.Equal (0, _textField.SelectedStart);
                    Assert.Equal (4, _textField.SelectedLength);
                    Assert.Equal ("TAB ", _textField.SelectedText);

                    break;
                case 1:
                    Assert.Equal (7, _textField.CursorPosition);
                    Assert.Equal (0, _textField.SelectedStart);
                    Assert.Equal (7, _textField.SelectedLength);
                    Assert.Equal ("TAB to ", _textField.SelectedText);

                    break;
                case 2:
                    Assert.Equal (12, _textField.CursorPosition);
                    Assert.Equal (0, _textField.SelectedStart);
                    Assert.Equal (12, _textField.SelectedLength);
                    Assert.Equal ("TAB to jump ", _textField.SelectedText);

                    break;
                case 3:
                    Assert.Equal (20, _textField.CursorPosition);
                    Assert.Equal (0, _textField.SelectedStart);
                    Assert.Equal (20, _textField.SelectedLength);
                    Assert.Equal ("TAB to jump between ", _textField.SelectedText);

                    break;
                case 4:
                    Assert.Equal (25, _textField.CursorPosition);
                    Assert.Equal (0, _textField.SelectedStart);
                    Assert.Equal (25, _textField.SelectedLength);
                    Assert.Equal ("TAB to jump between text ", _textField.SelectedText);

                    break;
                case 5:
                    Assert.Equal (32, _textField.CursorPosition);
                    Assert.Equal (0, _textField.SelectedStart);
                    Assert.Equal (32, _textField.SelectedLength);
                    Assert.Equal ("TAB to jump between text fields.", _textField.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextFieldTestsAutoInitShutdown]
    public void
        WordForward_With_The_Same_Values_For_SelectedStart_And_CursorPosition_And_Not_Starting_At_Beginning_Of_The_Text ()
    {
        _textField.CursorPosition = 10;
        _textField.SelectedStart = 10;
        var iteration = 0;

        while (_textField.CursorPosition < _textField.Text.Length)
        {
            _textField.NewKeyDownEvent (Key.CursorRight.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (12, _textField.CursorPosition);
                    Assert.Equal (10, _textField.SelectedStart);
                    Assert.Equal (2, _textField.SelectedLength);
                    Assert.Equal ("p ", _textField.SelectedText);

                    break;
                case 1:
                    Assert.Equal (20, _textField.CursorPosition);
                    Assert.Equal (10, _textField.SelectedStart);
                    Assert.Equal (10, _textField.SelectedLength);
                    Assert.Equal ("p between ", _textField.SelectedText);

                    break;
                case 2:
                    Assert.Equal (25, _textField.CursorPosition);
                    Assert.Equal (10, _textField.SelectedStart);
                    Assert.Equal (15, _textField.SelectedLength);
                    Assert.Equal ("p between text ", _textField.SelectedText);

                    break;
                case 3:
                    Assert.Equal (32, _textField.CursorPosition);
                    Assert.Equal (10, _textField.SelectedStart);
                    Assert.Equal (22, _textField.SelectedLength);
                    Assert.Equal ("p between text fields.", _textField.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Words_With_Accents_Incorrect_Order_Will_Result_With_Wrong_Accent_Place ()
    {
        var tf = new TextField { Width = 30, Text = "Les Misérables" };
        Toplevel top = new ();
        top.Add (tf);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
Les Misérables",
                                                      _output
                                                     );

        tf.Text = "Les Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables";
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
Les Misérables",
                                                      _output
                                                     );

        // incorrect order will result with a wrong accent place
        tf.Text = "Les Mis" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "erables";
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
Les Miśerables",
                                                      _output
                                                     );
    }

    private TextField GetTextFieldsInView ()
    {
        var tf = new TextField { Width = 10 };
        var tf2 = new TextField { Y = 1, Width = 10 };

        Toplevel top = new ();
        top.Add (tf);
        top.Add (tf2);

        Application.Begin (top);

        Assert.Same (tf, top.Focused);

        return tf;
    }

    // This class enables test functions annotated with the [InitShutdown] attribute
    // to have a function called before the test function is called and after.
    // 
    // This is necessary because a) Application is a singleton and Init/Shutdown must be called
    // as a pair, and b) all unit test functions should be atomic.
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
    public class TextFieldTestsAutoInitShutdown : AutoInitShutdownAttribute
    {
        public override void After (MethodInfo methodUnderTest)
        {
            _textField.Dispose ();
            _textField = null;
            base.After (methodUnderTest);
        }

        public override void Before (MethodInfo methodUnderTest)
        {
            base.Before (methodUnderTest);

            //Application.Top.ColorScheme = Colors.ColorSchemes ["Base"];
            _textField = new TextField
            {
                ColorScheme = new ColorScheme (Colors.ColorSchemes ["Base"]),

                //                1         2         3 
                //      01234567890123456789012345678901=32 (Length)
                Text = "TAB to jump between text fields.",
                Width = 32
            };
        }
    }
}
