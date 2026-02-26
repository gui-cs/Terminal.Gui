using System.Reflection;
using System.Text;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests;

public partial class TextViewTests
{
    private static TextView _textView;
    public TextViewTests (ITestOutputHelper output) => _output = output;
    private readonly ITestOutputHelper _output;

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void CanFocus_False_Wont_Focus_With_Mouse ()
    {
        Runnable top = new ();
        var tv = new TextView { Width = Dim.Fill (), CanFocus = false, ReadOnly = true, Text = "some text" };

        var fv = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = false, Title = "I shouldn't get focus" };
        fv.Add (tv);
        top.Add (fv);

        Application.Begin (top);

        Assert.False (tv.CanFocus);
        Assert.False (tv.HasFocus);
        Assert.False (fv.CanFocus);
        Assert.False (fv.HasFocus);

        tv.NewMouseEvent (new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonDoubleClicked });

        Assert.NotEmpty (tv.SelectedText);
        Assert.False (tv.CanFocus);
        Assert.False (tv.HasFocus);
        Assert.False (fv.CanFocus);
        Assert.False (fv.HasFocus);

        fv.CanFocus = true;
        tv.CanFocus = true;
        tv.NewMouseEvent (new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonDoubleClicked });

        Assert.Equal ("some ", tv.SelectedText);
        Assert.True (tv.CanFocus);
        Assert.True (tv.HasFocus);
        Assert.True (fv.CanFocus);
        Assert.True (fv.HasFocus);

        fv.CanFocus = false;
        tv.NewMouseEvent (new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonDoubleClicked });

        Assert.Equal ("some ", tv.SelectedText); // Setting CanFocus to false don't change the SelectedText
        Assert.True (tv.CanFocus); // v2: CanFocus is not longer automatically changed
        Assert.False (tv.HasFocus);
        Assert.False (fv.CanFocus);
        Assert.False (fv.HasFocus);
        top.Dispose ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Changing_Selection_Or_InsertionPoint_Update_SelectedLength_And_SelectedText ()
    {
        _textView.SelectionStartColumn = 2;
        _textView.SelectionStartRow = 0;
        Assert.Equal (0, _textView.InsertionPoint.X);
        Assert.Equal (0, _textView.InsertionPoint.Y);
        Assert.Equal (2, _textView.SelectedLength);
        Assert.Equal ("TA", _textView.SelectedText);
        _textView.InsertionPoint = new Point (20, 0);
        Assert.Equal (2, _textView.SelectionStartColumn);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (18, _textView.SelectedLength);
        Assert.Equal ("B to jump between ", _textView.SelectedText);
    }

    [Fact]
    [SetupFakeApplication]
    public void ContentsChanged_Event_Fires_On_InsertText ()
    {
        var eventcount = 0;

        var tv = new TextView { Width = 50, Height = 10 };
        tv.InsertionPoint = Point.Empty;

        tv.ContentsChanged += (s, e) => { eventcount++; };

        Assert.Equal (0, eventcount);

        tv.InsertText ("a");
        Assert.Equal (1, eventcount);

        tv.InsertionPoint = Point.Empty;
        tv.InsertText ("bcd");
        Assert.Equal (4, eventcount);

        tv.InsertText ("e");
        Assert.Equal (5, eventcount);

        tv.InsertText ("\n");
        Assert.Equal (6, eventcount);

        tv.InsertText ("1234");
        Assert.Equal (10, eventcount);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void ContentsChanged_Event_Fires_On_Undo_Redo ()
    {
        var eventcount = 0;
        var expectedEventCount = 0;

        _textView.ContentsChanged += (s, e) => { eventcount++; };

        expectedEventCount++;
        _textView.Text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount++;
        Assert.True (_textView.NewKeyDownEvent (Key.Enter));
        Assert.Equal (expectedEventCount, eventcount);

        // Undo
        expectedEventCount++;
        Assert.True (_textView.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal (expectedEventCount, eventcount);

        // Redo
        expectedEventCount++;
        Assert.True (_textView.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal (expectedEventCount, eventcount);

        // Undo
        expectedEventCount++;
        Assert.True (_textView.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal (expectedEventCount, eventcount);

        // Redo
        expectedEventCount++;
        Assert.True (_textView.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal (expectedEventCount, eventcount);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void ContentsChanged_Event_Fires_Using_Copy_Or_Cut_Tests ()
    {
        var eventcount = 0;

        _textView.ContentsChanged += (s, e) => { eventcount++; };

        var expectedEventCount = 1;

        // reset
        _textView.Text = TextViewTestsSetupFakeApplication.Txt;
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount += 3;
        Copy_Or_Cut_And_Paste_With_No_Selection ();
        Assert.Equal (expectedEventCount, eventcount);

        // reset
        expectedEventCount += 1;
        _textView.Text = TextViewTestsSetupFakeApplication.Txt;
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount += 3;
        Copy_Or_Cut_And_Paste_With_Selection ();
        Assert.Equal (expectedEventCount, eventcount);

        // reset
        expectedEventCount += 1;
        _textView.Text = TextViewTestsSetupFakeApplication.Txt;
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount += 1;
        Copy_Or_Cut_Not_Null_If_Has_Selection ();
        Assert.Equal (expectedEventCount, eventcount);

        // reset
        expectedEventCount += 1;
        _textView.Text = TextViewTestsSetupFakeApplication.Txt;
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount += 1;
        Copy_Or_Cut_Null_If_No_Selection ();
        Assert.Equal (expectedEventCount, eventcount);

        // reset
        expectedEventCount += 1;
        _textView.Text = TextViewTestsSetupFakeApplication.Txt;
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount += 4;
        Copy_Without_Selection ();
        Assert.Equal (expectedEventCount, eventcount);

        // reset
        expectedEventCount += 1;
        _textView.Text = TextViewTestsSetupFakeApplication.Txt;
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount += 4;
        Copy_Without_Selection ();
        Assert.Equal (expectedEventCount, eventcount);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void ContentsChanged_Event_Fires_Using_Kill_Delete_Tests ()
    {
        var eventcount = 0;

        _textView.ContentsChanged += (s, e) => { eventcount++; };

        var expectedEventCount = 1;
        Kill_Delete_WordForward ();
        Assert.Equal (expectedEventCount, eventcount); // for Initialize

        expectedEventCount += 1;
        Kill_Delete_WordBackward ();
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount += 2;
        Kill_To_End_Delete_Forwards_Copy_To_The_Clipboard_And_Paste ();
        Assert.Equal (expectedEventCount, eventcount);

        expectedEventCount += 2;
        Kill_To_Start_Delete_Backwards_Copy_To_The_Clipboard_And_Paste ();
        Assert.Equal (expectedEventCount, eventcount);
    }

    [Fact]
    [SetupFakeApplication]
    public void ContentsChanged_Event_NoFires_On_InsertionPoint ()
    {
        var eventcount = 0;

        var tv = new TextView { Width = 50, Height = 10 };

        tv.ContentsChanged += (s, e) => { eventcount++; };
        Assert.Equal (0, eventcount);

        tv.InsertionPoint = Point.Empty;

        Assert.Equal (0, eventcount);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Copy_Or_Cut_And_Paste_With_No_Selection ()
    {
        _textView.SelectionStartColumn = 20;
        _textView.SelectionStartRow = 0;
        _textView.InsertionPoint = new Point (24, 0);
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        Assert.Equal ("text", _textView.SelectedText);
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
        _textView.SelectionStartColumn = 0;
        _textView.SelectionStartRow = 0;
        Assert.Equal (new Point (24, 0), _textView.InsertionPoint);
        Assert.True (_textView.IsSelecting);
        _textView.IsSelecting = false;
        _textView.NewKeyDownEvent (Key.Y.WithCtrl); // Paste
        Assert.Equal (new Point (28, 0), _textView.InsertionPoint);
        Assert.False (_textView.IsSelecting);
        Assert.Equal ("TAB to jump between texttext fields.", _textView.Text);
        _textView.SelectionStartColumn = 24;
        _textView.SelectionStartRow = 0;
        _textView.NewKeyDownEvent (Key.W.WithCtrl); // Cut
        Assert.Equal (new Point (24, 0), _textView.InsertionPoint);
        Assert.False (_textView.IsSelecting);
        Assert.Equal ("", _textView.SelectedText);
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
        _textView.SelectionStartColumn = 0;
        _textView.SelectionStartRow = 0;
        _textView.IsSelecting = false;
        _textView.NewKeyDownEvent (Key.Y.WithCtrl); // Paste
        Assert.Equal (new Point (28, 0), _textView.InsertionPoint);
        Assert.False (_textView.IsSelecting);
        Assert.Equal ("TAB to jump between texttext fields.", _textView.Text);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Copy_Or_Cut_And_Paste_With_Selection ()
    {
        _textView.SelectionStartColumn = 20;
        _textView.SelectionStartRow = 0;
        _textView.InsertionPoint = new Point (24, 0);
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        Assert.Equal ("text", _textView.SelectedText);
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.Y.WithCtrl); // Paste
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
        _textView.SelectionStartColumn = 20;
        _textView.SelectionStartRow = 0;
        _textView.NewKeyDownEvent (Key.W.WithCtrl); // Cut
        _textView.NewKeyDownEvent (Key.Y.WithCtrl); // Paste
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Copy_Or_Cut_Not_Null_If_Has_Selection ()
    {
        _textView.SelectionStartColumn = 20;
        _textView.SelectionStartRow = 0;
        _textView.InsertionPoint = new Point (24, 0);
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        Assert.Equal ("text", _textView.SelectedText);
        _textView.NewKeyDownEvent (Key.W.WithCtrl); // Cut
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Copy_Or_Cut_Null_If_No_Selection ()
    {
        _textView.SelectionStartColumn = 0;
        _textView.SelectionStartRow = 0;
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        Assert.Equal ("", _textView.SelectedText);
        _textView.NewKeyDownEvent (Key.W.WithCtrl); // Cut
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Copy_Paste_Surrogate_Pairs ()
    {
        _textView.Text = "TextView with some more test text. Unicode shouldn't 𝔹Aℝ𝔽!";
        _textView.SelectAll ();
        _textView.Cut ();

        Assert.Equal ("TextView with some more test text. Unicode shouldn't 𝔹Aℝ𝔽!", Application.Driver?.Clipboard.GetClipboardData ());
        Assert.Equal (string.Empty, _textView.Text);
        _textView.Paste ();
        Assert.Equal ("TextView with some more test text. Unicode shouldn't 𝔹Aℝ𝔽!", _textView.Text);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Copy_Without_Selection ()
    {
        _textView.Text = "This is the first line.\nThis is the second line.\n";
        _textView.InsertionPoint = new Point (0, _textView.Lines - 1);
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        _textView.NewKeyDownEvent (Key.Y.WithCtrl); // Paste

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}{Environment.NewLine}", _textView.Text);
        _textView.InsertionPoint = new Point (3, 1);
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        _textView.NewKeyDownEvent (Key.Y.WithCtrl); // Paste

        Assert.Equal ($"This is the first line.{
            Environment.NewLine
        }This is the second line.{
            Environment.NewLine
        }This is the second line.{
            Environment.NewLine
        }{
            Environment.NewLine
        }",
                      _textView.Text);
        Assert.Equal (new Point (3, 2), _textView.InsertionPoint);
        _textView.NewKeyDownEvent (Key.Y.WithCtrl); // Paste

        Assert.Equal ($"This is the first line.{
            Environment.NewLine
        }This is the second line.{
            Environment.NewLine
        }This is the second line.{
            Environment.NewLine
        }This is the second line.{
            Environment.NewLine
        }{
            Environment.NewLine
        }",
                      _textView.Text);
        Assert.Equal (new Point (3, 3), _textView.InsertionPoint);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Cursor_Horizontal_Navigation ()
    {
        var text = "";

        for (var i = 0; i < 12; i++)
        {
            text += $"{i.ToString () [^1]}";
        }

        var tv = new TextView { Width = 10, Height = 10 };
        tv.Text = text;
        var top = new Runnable ();
        top.Add (tv);
        Application.Begin (top);
        Assert.False (Application.Driver!.GetCursor ().IsVisible);
        Application.Navigation.UpdateCursor ();
        Assert.True (Application.Driver!.GetCursor ().IsVisible);

        Assert.True (tv.HasFocus);
        Assert.Equal (0, tv.Viewport.X);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.True (tv.Cursor.IsVisible);

        for (var i = 0; i < 12; i++)
        {
            // Scroll right, hiding the insertion point
            tv.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledRight });
            Application.Navigation.UpdateCursor ();
            Assert.Equal (Math.Min (i + 1, 11), tv.Viewport.X);
            Assert.False (tv.Cursor.IsVisible);
            Assert.False (Application.Driver!.GetCursor ().IsVisible);
        }

        for (var i = 11; i > 0; i--)
        {
            // Scroll left, eventually showing insertion point
            tv.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledLeft });
            Application.Navigation.UpdateCursor ();
            Assert.Equal (i - 1, tv.Viewport.X);

            if (i - 1 == 0)
            {
                Assert.True (tv.Cursor.IsVisible);
                Assert.True (Application.Driver!.GetCursor ().IsVisible);
            }
            else
            {
                Assert.False (tv.Cursor.IsVisible);
                Assert.False (Application.Driver!.GetCursor ().IsVisible);
            }
        }

        top.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Cursor_Position_Multiline_False_Initialization ()
    {
        Assert.False (_textView.IsInitialized);
        Assert.True (_textView.Multiline);
        _textView.Multiline = false;
        Assert.Equal (32, _textView.InsertionPoint.X);
        Assert.Equal (0, _textView.InsertionPoint.Y);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.Equal ("", _textView.SelectedText);
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Cursor_Vertical_Navigation ()
    {
        var text = "";

        for (var i = 0; i < 12; i++)
        {
            text += $"This is the line {i}\n";
        }

        var tv = new TextView { Width = 10, Height = 10 };
        tv.Text = text;
        var top = new Runnable ();
        top.Add (tv);
        Application.Begin (top);

        Assert.Equal (0, tv.Viewport.Y);

        for (var i = 0; i < 12; i++)
        {
            tv.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledDown });
            Application.Navigation.UpdateCursor ();
            Assert.Equal (i + 1, tv.Viewport.Y);
            Assert.False (Application.Driver!.GetCursor ().IsVisible);
        }

        for (var i = 12; i > 0; i--)
        {
            tv.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledUp });
            Application.Navigation.UpdateCursor ();
            Assert.Equal (i - 1, tv.Viewport.Y);

            if (i - 1 == 0)
            {
                Assert.True (Application.Driver!.GetCursor ().IsVisible);
            }
            else
            {
                Assert.False (Application.Driver!.GetCursor ().IsVisible);
            }
        }

        top.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Cut_Not_Allowed_If_ReadOnly_Is_True ()
    {
        _textView.ReadOnly = true;
        _textView.SelectionStartColumn = 20;
        _textView.SelectionStartRow = 0;
        _textView.InsertionPoint = new Point (24, 0);
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        Assert.Equal ("text", _textView.SelectedText);

        _textView.NewKeyDownEvent (Key.W.WithCtrl); // Selecting is set to false after Cut.
        Assert.Equal ("", _textView.SelectedText);
        _textView.ReadOnly = false;
        Assert.False (_textView.IsSelecting);
        _textView.IsSelecting = true; // Needed to set Selecting to true.
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        Assert.Equal ("text", _textView.SelectedText);
        _textView.NewKeyDownEvent (Key.W.WithCtrl); // Cut
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [SetupFakeApplication]
    public void DeleteTextBackwards_WordWrap_False_Return_Undo ()
    {
        const string text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView { Width = Dim.Fill (), Height = Dim.Fill (), Text = text };
        string envText = tv.Text;
        var top = new Runnable ();
        top.Add (tv);
        SessionToken rs = Application.Begin (top);
        SetupFakeApplicationAttribute.RunIteration ();

        Assert.False (tv.WordWrap);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
",
                                                       _output);

        tv.InsertionPoint = new Point (3, 0);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);
        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
",
                                                       _output);

        tv.InsertionPoint = new Point (0, 1);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (22, 0), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.This is the second line.
",
                                                       _output);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
",
                                                       _output);

        while (tv.Text != envText)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (envText, tv.Text);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void DeleteTextBackwards_WordWrap_True_Return_Undo ()
    {
        const string text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView { Width = Dim.Fill (), Height = Dim.Fill (), Text = text, WordWrap = true };
        string envText = tv.Text;
        var top = new Runnable ();
        top.Add (tv);
        SessionToken rs = Application.Begin (top);
        SetupFakeApplicationAttribute.RunIteration ();

        Assert.True (tv.WordWrap);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
",
                                                       _output);

        tv.InsertionPoint = new Point (3, 0);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);
        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
",
                                                       _output);

        tv.InsertionPoint = new Point (0, 1);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (22, 0), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.This is the second line.
",
                                                       _output);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
",
                                                       _output);

        while (tv.Text != envText)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        SetupFakeApplicationAttribute.RunIteration ();

        Assert.Equal (envText, tv.Text);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void DeleteTextForwards_WordWrap_False_Return_Undo ()
    {
        const string text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView { Width = Dim.Fill (), Height = Dim.Fill (), Text = text };
        string envText = tv.Text;
        var top = new Runnable ();
        top.Add (tv);
        SessionToken rs = Application.Begin (top);
        SetupFakeApplicationAttribute.RunIteration ();

        Assert.False (tv.WordWrap);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
",
                                                       _output);

        tv.InsertionPoint = new Point (2, 0);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);
        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
",
                                                       _output);

        tv.InsertionPoint = new Point (22, 0);
        Assert.Equal (new Point (22, 0), tv.InsertionPoint);
        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (22, 0), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.This is the second line.
",
                                                       _output);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
",
                                                       _output);

        while (tv.Text != envText)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        Assert.Equal (envText, tv.Text);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void DeleteTextForwards_WordWrap_True_Return_Undo ()
    {
        const string text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView { Width = Dim.Fill (), Height = Dim.Fill (), Text = text, WordWrap = true };
        string envText = tv.Text;
        var top = new Runnable ();
        top.Add (tv);
        SessionToken rs = Application.Begin (top);
        SetupFakeApplicationAttribute.RunIteration ();

        Assert.True (tv.WordWrap);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
",
                                                       _output);

        tv.InsertionPoint = new Point (2, 0);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);
        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
",
                                                       _output);

        tv.InsertionPoint = new Point (22, 0);
        Assert.Equal (new Point (22, 0), tv.InsertionPoint);
        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (22, 0), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.This is the second line.
",
                                                       _output);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Ths is the first line.  
This is the second line.
",
                                                       _output);

        while (tv.Text != envText)
        {
            Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        }

        SetupFakeApplicationAttribute.RunIteration ();

        Assert.Equal (envText, tv.Text);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void Draw_Esc_Rune ()
    {
        var tv = new TextView { Driver = ApplicationImpl.Instance.Driver, Width = 5, Height = 1, Text = "\u001b" };
        tv.BeginInit ();
        tv.EndInit ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre ("\u241b", _output);

        tv.Dispose ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void InsertionPoint_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
    {
        _textView.InsertionPoint = new Point (33, 1);
        Assert.Equal (32, _textView.InsertionPoint.X);
        Assert.Equal (0, _textView.InsertionPoint.Y);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void InsertionPoint_With_Value_Less_Than_Zero_Changes_To_Zero ()
    {
        _textView.InsertionPoint = new Point (-1, -1);
        Assert.Equal (0, _textView.InsertionPoint.X);
        Assert.Equal (0, _textView.InsertionPoint.Y);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void IsSelecting_False_If_SelectedLength_Is_Zero_On_Mouse_Click ()
    {
        _textView.Text = "This is the first line.";
        var top = new Runnable ();
        top.Add (_textView);
        Application.Begin (top);

        Application.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (22, 0), Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (22, _textView.InsertionPoint.X);
        Assert.Equal (0, _textView.InsertionPoint.Y);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.True (_textView.IsSelecting);

        Application.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (22, 0), Flags = MouseFlags.LeftButtonReleased });
        Assert.Equal (22, _textView.InsertionPoint.X);
        Assert.Equal (0, _textView.InsertionPoint.Y);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.True (_textView.IsSelecting);

        Application.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (22, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (22, _textView.InsertionPoint.X);
        Assert.Equal (0, _textView.InsertionPoint.Y);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.False (_textView.IsSelecting);

        top.Dispose ();
        Application.Shutdown ();
    }

    // KeyBindings_Commands test replaced by new tests

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Kill_Delete_WordBackward ()
    {
        _textView.Text = "This is the first line.";
        _textView.MoveEnd ();
        var iteration = 0;
        var iterationsFinished = false;

        while (!iterationsFinished)
        {
            _textView.NewKeyDownEvent (Key.Backspace.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (22, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the first line", _textView.Text);

                    break;

                case 1:
                    Assert.Equal (18, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the first ", _textView.Text);

                    break;

                case 2:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the ", _textView.Text);

                    break;

                case 3:
                    Assert.Equal (8, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is ", _textView.Text);

                    break;

                case 4:
                    Assert.Equal (5, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This ", _textView.Text);

                    break;

                case 5:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("", _textView.Text);

                    break;

                default:
                    iterationsFinished = true;

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Kill_Delete_WordBackward_Multiline ()
    {
        _textView.Text = "This is the first line.\nThis is the second line.";
        _textView.Width = 4;
        _textView.MoveEnd ();
        var iteration = 0;
        var iterationsFinished = false;

        while (!iterationsFinished)
        {
            _textView.NewKeyDownEvent (Key.Backspace.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (23, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);

                    Assert.Equal ("This is the first line." + Environment.NewLine + "This is the second line", _textView.Text);

                    break;

                case 1:
                    Assert.Equal (19, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);

                    Assert.Equal ("This is the first line." + Environment.NewLine + "This is the second ", _textView.Text);

                    break;

                case 2:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);

                    Assert.Equal ("This is the first line." + Environment.NewLine + "This is the ", _textView.Text);

                    break;

                case 3:
                    Assert.Equal (8, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);

                    Assert.Equal ("This is the first line." + Environment.NewLine + "This is ", _textView.Text);

                    break;

                case 4:
                    Assert.Equal (5, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);

                    Assert.Equal ("This is the first line." + Environment.NewLine + "This ", _textView.Text);

                    break;

                case 5:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the first line." + Environment.NewLine, _textView.Text);

                    break;

                case 6:
                    Assert.Equal (23, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the first line.", _textView.Text);

                    break;

                case 7:
                    Assert.Equal (22, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the first line", _textView.Text);

                    break;

                case 8:
                    Assert.Equal (18, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the first ", _textView.Text);

                    break;

                case 9:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the ", _textView.Text);

                    break;

                case 10:
                    Assert.Equal (8, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is ", _textView.Text);

                    break;

                case 11:
                    Assert.Equal (5, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This ", _textView.Text);

                    break;

                case 12:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("", _textView.Text);

                    break;

                default:
                    iterationsFinished = true;

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Kill_Delete_WordForward ()
    {
        _textView.Text = "This is the first line.";
        var iteration = 0;
        var iterationsFinished = false;

        while (!iterationsFinished)
        {
            _textView.NewKeyDownEvent (Key.Delete.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("is the first line.", _textView.Text);

                    break;

                case 1:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("the first line.", _textView.Text);

                    break;

                case 2:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("first line.", _textView.Text);

                    break;

                case 3:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("line.", _textView.Text);

                    break;

                case 4:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (".", _textView.Text);

                    break;

                case 5:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("", _textView.Text);

                    break;

                default:
                    iterationsFinished = true;

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Kill_Delete_WordForward_Multiline ()
    {
        _textView.Text = "This is the first line.\nThis is the second line.";
        _textView.Width = 4;
        var iteration = 0;
        var iterationsFinished = false;

        while (!iterationsFinished)
        {
            _textView.NewKeyDownEvent (Key.Delete.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);

                    Assert.Equal ("is the first line." + Environment.NewLine + "This is the second line.", _textView.Text);

                    break;

                case 1:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);

                    Assert.Equal ("the first line." + Environment.NewLine + "This is the second line.", _textView.Text);

                    break;

                case 2:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);

                    Assert.Equal ("first line." + Environment.NewLine + "This is the second line.", _textView.Text);

                    break;

                case 3:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);

                    Assert.Equal ("line." + Environment.NewLine + "This is the second line.", _textView.Text);

                    break;

                case 4:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);

                    Assert.Equal ("." + Environment.NewLine + "This is the second line.", _textView.Text);

                    break;

                case 5:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);

                    Assert.Equal ("" + Environment.NewLine + "This is the second line.", _textView.Text);

                    break;

                case 6:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the second line.", _textView.Text);

                    break;

                case 7:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("is the second line.", _textView.Text);

                    break;

                case 8:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("the second line.", _textView.Text);

                    break;

                case 9:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("second line.", _textView.Text);

                    break;

                case 10:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("line.", _textView.Text);

                    break;

                case 11:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (".", _textView.Text);

                    break;

                case 12:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("", _textView.Text);

                    break;

                default:
                    iterationsFinished = true;

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Kill_To_End_Delete_Forwards_Copy_To_The_Clipboard_And_Paste ()
    {
        _textView.Text = "This is the first line.\nThis is the second line.";
        var iteration = 0;
        var iterationsFinished = false;

        while (!iterationsFinished)
        {
            switch (iteration)
            {
                case 0:
                    _textView.NewKeyDownEvent (Key.K.WithCtrl);
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ($"{Environment.NewLine}This is the second line.", _textView.Text);
                    Assert.Equal ("This is the first line.", Clipboard.Contents);

                    break;

                case 1:
                    _textView.NewKeyDownEvent (new Key (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.ShiftMask));
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the second line.", _textView.Text);
                    Assert.Equal ($"This is the first line.{Environment.NewLine}", Clipboard.Contents);

                    break;

                case 2:
                    _textView.NewKeyDownEvent (Key.K.WithCtrl);
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("", _textView.Text);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", Clipboard.Contents);

                    // Paste
                    _textView.NewKeyDownEvent (Key.Y.WithCtrl);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", _textView.Text);

                    break;

                default:
                    iterationsFinished = true;

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Kill_To_Start_Delete_Backwards_Copy_To_The_Clipboard_And_Paste ()
    {
        _textView.Text = "This is the first line.\nThis is the second line.";
        _textView.MoveEnd ();
        var iteration = 0;
        var iterationsFinished = false;

        while (!iterationsFinished)
        {
            switch (iteration)
            {
                case 0:
                    _textView.NewKeyDownEvent (Key.Backspace.WithCtrl.WithShift);
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal ($"This is the first line.{Environment.NewLine}", _textView.Text);
                    Assert.Equal ("This is the second line.", Clipboard.Contents);

                    break;

                case 1:
                    _textView.NewKeyDownEvent (new Key (KeyCode.Backspace | KeyCode.CtrlMask | KeyCode.ShiftMask));
                    Assert.Equal (23, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("This is the first line.", _textView.Text);
                    Assert.Equal ($"This is the second line.{Environment.NewLine}", Clipboard.Contents);

                    break;

                case 2:
                    _textView.NewKeyDownEvent (Key.Backspace.WithCtrl.WithShift);
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal ("", _textView.Text);

                    Assert.Equal ($"This is the second line.{Environment.NewLine}This is the first line.", Clipboard.Contents);

                    // Paste inverted
                    _textView.NewKeyDownEvent (Key.Y.WithCtrl);

                    Assert.Equal ($"This is the second line.{Environment.NewLine}This is the first line.", _textView.Text);

                    break;

                default:
                    iterationsFinished = true;

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Mouse_Button_Shift_Preserves_Selection ()
    {
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);

        Assert.True (_textView.NewMouseEvent (new Mouse { Position = new Point (12, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Shift }));
        Assert.Equal (0, _textView.SelectionStartColumn);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (new Point (12, 0), _textView.InsertionPoint);
        Assert.True (_textView.IsSelecting);
        Assert.Equal ("TAB to jump ", _textView.SelectedText);

        Assert.True (_textView.NewMouseEvent (new Mouse { Position = new Point (12, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (new Point (12, 0), _textView.InsertionPoint);
        Assert.True (_textView.IsSelecting);
        Assert.Equal ("TAB to jump ", _textView.SelectedText);

        Assert.True (_textView.NewMouseEvent (new Mouse { Position = new Point (19, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Shift }));
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (new Point (19, 0), _textView.InsertionPoint);
        Assert.True (_textView.IsSelecting);
        Assert.Equal ("TAB to jump between", _textView.SelectedText);

        Assert.True (_textView.NewMouseEvent (new Mouse { Position = new Point (19, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (new Point (19, 0), _textView.InsertionPoint);
        Assert.True (_textView.IsSelecting);
        Assert.Equal ("TAB to jump between", _textView.SelectedText);

        Assert.True (_textView.NewMouseEvent (new Mouse { Position = new Point (24, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Shift }));
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (new Point (24, 0), _textView.InsertionPoint);
        Assert.True (_textView.IsSelecting);
        Assert.Equal ("TAB to jump between text", _textView.SelectedText);

        Assert.True (_textView.NewMouseEvent (new Mouse { Position = new Point (24, 0), Flags = MouseFlags.LeftButtonClicked }));
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (new Point (24, 0), _textView.InsertionPoint);
        Assert.True (_textView.IsSelecting);
        Assert.Equal ("TAB to jump between text", _textView.SelectedText);

        Assert.True (_textView.NewMouseEvent (new Mouse { Position = new Point (24, 0), Flags = MouseFlags.LeftButtonPressed }));
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (new Point (24, 0), _textView.InsertionPoint);
        Assert.True (_textView.IsSelecting);
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [SetupFakeApplication]
    public void MoveDown_By_Setting_InsertionPoint ()
    {
        var tv = new TextView { Width = 10, Height = 5 };

        // add 100 lines of wide text to view
        for (var i = 0; i < 100; i++)
        {
            tv.Text += new string ('x', 100) + (i == 99 ? "" : Environment.NewLine);
        }

        Assert.Equal (Point.Empty, tv.InsertionPoint);
        tv.InsertionPoint = new Point (5, 50);
        Assert.Equal (new Point (5, 50), tv.InsertionPoint);

        tv.InsertionPoint = new Point (200, 200);
        Assert.Equal (new Point (100, 99), tv.InsertionPoint);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Multiline_Setting_Changes_AllowsReturn_AllowsTab_Height_WordWrap ()
    {
        Assert.True (_textView.Multiline);
        Assert.True (_textView.EnterKeyAddsLine);
        Assert.Equal (4, _textView.TabWidth);
        Assert.True (_textView.TabKeyAddsTab);
        Assert.False (_textView.WordWrap);

        _textView.WordWrap = true;
        Assert.True (_textView.WordWrap);
        _textView.Multiline = false;
        Assert.False (_textView.Multiline);
        Assert.False (_textView.EnterKeyAddsLine);
        Assert.Equal (0, _textView.TabWidth);
        Assert.False (_textView.TabKeyAddsTab);
        Assert.False (_textView.WordWrap);

        _textView.WordWrap = true;
        Assert.False (_textView.WordWrap);
        _textView.Multiline = true;
        Assert.True (_textView.Multiline);
        Assert.True (_textView.EnterKeyAddsLine);
        Assert.Equal (4, _textView.TabWidth);
        Assert.True (_textView.TabKeyAddsTab);
        Assert.False (_textView.WordWrap);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Paste_Always_Clear_The_SelectedText ()
    {
        _textView.SelectionStartColumn = 20;
        _textView.SelectionStartRow = 0;
        _textView.InsertionPoint = new Point (24, 0);
        _textView.NewKeyDownEvent (Key.C.WithCtrl); // Copy
        Assert.Equal ("text", _textView.SelectedText);
        _textView.NewKeyDownEvent (Key.Y.WithCtrl); // Paste
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [SetupFakeApplication]
    public void ScrollDownTillCaretOffscreen_ThenType ()
    {
        var tv = new TextView { Width = 10, Height = 5 };

        // add 100 lines of wide text to view
        for (var i = 0; i < 100; i++)
        {
            tv.Text += new string ('x', 100) + Environment.NewLine;
        }

        Assert.Equal (0, tv.InsertionPoint.Y);
        tv.ScrollTo (50);
        Assert.Equal (0, tv.InsertionPoint.Y);

        tv.NewKeyDownEvent (Key.P);
    }

    [Fact (Skip = "Broken with new scrollbar stuff")]
    [SetupFakeApplication]
    public void ScrollTo_InsertionPoint ()
    {
        var tv = new TextView { Width = 10, Height = 5 };

        // add 100 lines of wide text to view
        for (var i = 0; i < 100; i++)
        {
            tv.Text += new string ('x', 100) + (i == 99 ? "" : Environment.NewLine);
        }

        Assert.Equal (Point.Empty, tv.InsertionPoint);
        tv.ScrollTo (50);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        tv.InsertionPoint = new Point (tv.Viewport.X, tv.Viewport.Y);
        Assert.Equal (new Point (0, 50), tv.InsertionPoint);
    }

    [Fact (Skip = "This is a bogus test; TextView selection works fine.")]
    [TextViewTestsSetupFakeApplication]
    public void Selected_Text_Shows ()
    {
        // Proves #3022 is fixed (TextField selected text does not show in v2)
        Runnable top = new ();
        top.Add (_textView);
        SessionToken rs = Application.Begin (top);

        _textView.InsertionPoint = Point.Empty;
        _textView.SelectionStartColumn = 0;
        _textView.SelectionStartRow = 0;

        Attribute [] attributes = { _textView.GetScheme ().Focus, new (_textView.GetScheme ().Focus.Background, _textView.GetScheme ().Focus.Foreground) };

        //                                             TAB to jump between text fields.
        DriverAssert.AssertDriverAttributesAre ("0000000", _output, Application.Driver, attributes);
        Assert.Empty (_textView.SelectedCellsList);

        _textView.NewKeyDownEvent (Key.CursorRight.WithCtrl.WithShift);

        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (new Point (4, 0), _textView.InsertionPoint);

        //                                             TAB to jump between text fields.
        DriverAssert.AssertDriverAttributesAre ("1111000", _output, Application.Driver, attributes);
        Assert.Equal ("TAB ", Cell.ToString (_textView.SelectedCellsList [^1]));
        top.Dispose ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Selection_And_InsertionPoint_With_Value_Greater_Than_Text_Length_Changes_Both_To_Text_Length ()
    {
        _textView.InsertionPoint = new Point (33, 2);
        _textView.SelectionStartColumn = 33;
        _textView.SelectionStartRow = 33;
        Assert.Equal (32, _textView.InsertionPoint.X);
        Assert.Equal (0, _textView.InsertionPoint.Y);
        Assert.Equal (32, _textView.SelectionStartColumn);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Selection_With_Empty_Text ()
    {
        _textView = new TextView ();
        _textView.InsertionPoint = new Point (2, 0);
        _textView.SelectionStartColumn = 33;
        _textView.SelectionStartRow = 1;
        Assert.Equal (0, _textView.SelectionStartColumn);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Selection_With_Value_Greater_Than_Text_Length_Changes_To_Text_Length ()
    {
        _textView.InsertionPoint = new Point (2, 0);
        _textView.SelectionStartColumn = 33;
        _textView.SelectionStartRow = 1;
        Assert.Equal (32, _textView.SelectionStartColumn);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (30, _textView.SelectedLength);
        Assert.Equal ("B to jump between text fields.", _textView.SelectedText);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Selection_With_Value_Less_Than_Zero_Changes_To_Zero ()
    {
        _textView.SelectionStartColumn = -2;
        _textView.SelectionStartRow = -2;
        Assert.Equal (0, _textView.SelectionStartColumn);
        Assert.Equal (0, _textView.SelectionStartRow);
        Assert.Equal (0, _textView.SelectedLength);
        Assert.Equal ("", _textView.SelectedText);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Tab_Test_Follow_By_CursorLeft_And_Then_Follow_By_CursorRight ()
    {
        var top = new Runnable ();
        top.Add (_textView);

        Application.Iteration += OnApplicationOnIteration;

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication> a)
        {
            int width = _textView.Viewport.Width - 1;
            Assert.Equal (30, width + 1);
            Assert.Equal (10, _textView.Height);
            _textView.Text = "";
            var col = 0;
            var leftCol = 0;
            int tabWidth = _textView.TabWidth;

            while (col < 100)
            {
                col++;
                _textView.NewKeyDownEvent (Key.Tab);
                Assert.Equal (new Point (col, 0), _textView.InsertionPoint);
                leftCol = GetLeftCol (leftCol);
                Assert.Equal (leftCol, _textView.Viewport.X);
            }

            while (col > 0)
            {
                col--;
                _textView.NewKeyDownEvent (Key.CursorLeft);
                Assert.Equal (new Point (col, 0), _textView.InsertionPoint);
                leftCol = GetLeftCol (leftCol);
                Assert.Equal (leftCol, _textView.Viewport.X);
            }

            while (col < 100)
            {
                col++;
                _textView.NewKeyDownEvent (Key.CursorRight);
                Assert.Equal (new Point (col, 0), _textView.InsertionPoint);
                leftCol = GetLeftCol (leftCol);
                Assert.Equal (leftCol, _textView.Viewport.X);
            }

            top.Remove (_textView);
            Application.RequestStop ();
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Tab_Test_Follow_By_CursorLeft_And_Then_Follow_By_CursorRight_With_Text ()
    {
        var top = new Runnable ();
        top.Add (_textView);

        Application.Iteration += OnApplicationOnIteration;

        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;
        top.Dispose ();

        return;

        void OnApplicationOnIteration (object s, EventArgs<IApplication> a)
        {
            int width = _textView.Viewport.Width - 1;
            Assert.Equal (30, width + 1);
            Assert.Equal (10, _textView.Height);
            Assert.Equal ("TAB to jump between text fields.", _textView.Text);
            var col = 0;
            var leftCol = 0;
            int tabWidth = _textView.TabWidth;

            while (col < 100)
            {
                col++;
                _textView.NewKeyDownEvent (Key.Tab);
                Assert.Equal (new Point (col, 0), _textView.InsertionPoint);
                leftCol = GetLeftCol (leftCol);
                Assert.Equal (leftCol, _textView.Viewport.X);
            }

            Assert.Equal (132, _textView.Text.Length);

            while (col > 0)
            {
                col--;
                _textView.NewKeyDownEvent (Key.CursorLeft);
                Assert.Equal (new Point (col, 0), _textView.InsertionPoint);
                leftCol = GetLeftCol (leftCol);
                Assert.Equal (leftCol, _textView.Viewport.X);
            }

            while (col < 100)
            {
                col++;
                _textView.NewKeyDownEvent (Key.CursorRight);
                Assert.Equal (new Point (col, 0), _textView.InsertionPoint);
                leftCol = GetLeftCol (leftCol);
                Assert.Equal (leftCol, _textView.Viewport.X);
            }

            top.Remove (_textView);
            Application.RequestStop ();
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void TabWidth_Setting_To_Zero_Keeps_AllowsTab ()
    {
        Runnable top = new ();
        top.Add (_textView);
        _textView.HorizontalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
        _textView.HorizontalScrollBar.Visible = false;
        Application.Begin (top);

        Assert.Equal (4, _textView.TabWidth);
        Assert.True (_textView.TabKeyAddsTab);
        Assert.True (_textView.EnterKeyAddsLine);
        Assert.True (_textView.Multiline);
        _textView.TabWidth = -1;
        Assert.Equal (0, _textView.TabWidth);
        Assert.True (_textView.TabKeyAddsTab);
        Assert.True (_textView.EnterKeyAddsLine);
        Assert.True (_textView.Multiline);
        _textView.NewKeyDownEvent (Key.Tab);
        Assert.Equal ("\tTAB to jump between text fields.", _textView.Text);
        SetupFakeApplicationAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
TAB to jump between text field",
                                                       _output);

        _textView.TabWidth = 4;
        SetupFakeApplicationAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
    TAB to jump between text f",
                                                       _output);
        top.Dispose ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void TextChanged_Event ()
    {
        _textView.TextChanged += (s, e) =>
                                 {
                                     if (_textView.Text == "changing")
                                     {
                                         Assert.Equal ("changing", _textView.Text);
                                         _textView.Text = "changed";
                                     }
                                 };

        _textView.Text = "changing";
        Assert.Equal ("changed", _textView.Text);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void TextChanged_Event_NoFires_OnTyping ()
    {
        var eventcount = 0;
        _textView.TextChanged += (s, e) => { eventcount++; };

        _textView.Text = "ay";
        Assert.Equal (1, eventcount);
        _textView.NewKeyDownEvent (Key.Y.WithShift);
        Assert.Equal (1, eventcount);
        Assert.Equal ("Yay", _textView.Text);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void TextView_InsertText_Newline_CRLF ()
    {
        var tv = new TextView { Width = 10, Height = 10 };
        tv.InsertText ("\r\naaa\r\nbbb");
        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            Assert.Equal ("\r\naaa\r\nbbb", tv.Text);
        }
        else
        {
            Assert.Equal ("\naaa\nbbb", tv.Text);
        }

        Assert.Equal ($"{Environment.NewLine}aaa{Environment.NewLine}bbb", tv.Text);

        var win = new Window ();
        win.Add (tv);
        var top = new Runnable ();
        top.Add (win);
        Application.Begin (top);
        Application.Driver!.SetScreenSize (15, 15);
        SetupFakeApplicationAttribute.RunIteration ();

        //this passes
        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (@"
┌─────────────┐
│             │
│aaa          │
│bbb          │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘",
                                                                       _output);

        Assert.Equal (new Rectangle (0, 0, 15, 15), pos);

        Assert.True (tv.Used);
        tv.Used = false;
        tv.InsertionPoint = Point.Empty;
        tv.InsertText ("\r\naaa\r\nbbb");
        SetupFakeApplicationAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌─────────────┐
│             │
│aaa          │
│bbb          │
│aaa          │
│bbb          │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘",
                                                       _output);
        top.Dispose ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void TextView_InsertText_Newline_LF ()
    {
        var tv = new TextView { Width = 10, Height = 10 };
        tv.InsertText ("\naaa\nbbb");
        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            Assert.Equal ("\r\naaa\r\nbbb", tv.Text);
        }
        else
        {
            Assert.Equal ("\naaa\nbbb", tv.Text);
        }

        Assert.Equal ($"{Environment.NewLine}aaa{Environment.NewLine}bbb", tv.Text);

        var win = new Window ();
        win.Add (tv);
        var top = new Runnable ();
        top.Add (win);
        Application.Begin (top);
        Application.Driver!.SetScreenSize (15, 15);
        SetupFakeApplicationAttribute.RunIteration ();

        //this passes
        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (@"
┌─────────────┐
│             │
│aaa          │
│bbb          │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘",
                                                                       _output);

        Assert.Equal (new Rectangle (0, 0, 15, 15), pos);

        Assert.True (tv.Used);
        tv.Used = false;
        tv.InsertionPoint = Point.Empty;
        tv.InsertText ("\naaa\nbbb");
        SetupFakeApplicationAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌─────────────┐
│             │
│aaa          │
│bbb          │
│aaa          │
│bbb          │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘",
                                                       _output);
        top.Dispose ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void TextView_SpaceHandling ()
    {
        var tv = new TextView { Width = 10, Text = " " };

        var ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonDoubleClicked };

        tv.NewMouseEvent (ev);
        Assert.Equal (1, tv.SelectedLength);

        ev = new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonDoubleClicked };

        tv.NewMouseEvent (ev);
        Assert.Equal (1, tv.SelectedLength);
    }

    [Fact]
    [SetupFakeApplication]
    public void UnwrappedInsertionPoint_Event ()
    {
        var cp = Point.Empty;

        var tv = new TextView { Width = Dim.Fill (), Height = Dim.Fill (), Text = "This is the first line.\nThis is the second line.\n" };
        tv.UnwrappedCursorPositionChanged += (s, e) => { cp = e; };
        var top = new Runnable ();
        top.Add (tv);
        Application.Begin (top);
        SetupFakeApplicationAttribute.RunIteration ();

        Assert.False (tv.WordWrap);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (Point.Empty, cp);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
",
                                                       _output);

        tv.WordWrap = true;
        tv.InsertionPoint = new Point (12, 0);
        tv.Draw ();
        Assert.Equal (new Point (12, 0), tv.InsertionPoint);
        Assert.Equal (new Point (12, 0), cp);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is the first line. 
This is the second line.
",
                                                       _output);

        Application.Driver!.SetScreenSize (6, 25);
        Application.LayoutAndDraw ();
        tv.Draw ();
        Assert.Equal (new Point (4, 2), tv.InsertionPoint);
        Assert.Equal (new Point (12, 0), cp);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This 
is   
the  
first
     
line.
This 
is   
the  
secon
d    
line.
",
                                                       _output);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        tv.Draw ();
        Assert.Equal (new Point (0, 3), tv.InsertionPoint);
        Assert.Equal (new Point (12, 0), cp);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This 
is   
the  
first
     
line.
This 
is   
the  
secon
d    
line.
",
                                                       _output);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        tv.Draw ();
        Assert.Equal (new Point (1, 3), tv.InsertionPoint);
        Assert.Equal (new Point (13, 0), cp);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This 
is   
the  
first
     
line.
This 
is   
the  
secon
d    
line.
",
                                                       _output);

        Assert.True (tv.NewMouseEvent (new Mouse { Position = new Point (0, 3), Flags = MouseFlags.LeftButtonPressed }));
        tv.Draw ();
        Assert.Equal (new Point (0, 3), tv.InsertionPoint);
        Assert.Equal (new Point (12, 0), cp);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This 
is   
the  
first
     
line.
This 
is   
the  
secon
d    
line.
",
                                                       _output);
        top.Dispose ();
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Used_Is_False ()
    {
        _textView.Used = false;
        _textView.InsertionPoint = new Point (10, 0);
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.U); // u
        Assert.Equal ("TAB to jumu between text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.S); // s
        Assert.Equal ("TAB to jumusbetween text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.E); // e
        Assert.Equal ("TAB to jumuseetween text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.D); // d
        Assert.Equal ("TAB to jumusedtween text fields.", _textView.Text);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void Used_Is_True_By_Default ()
    {
        _textView.InsertionPoint = new Point (10, 0);
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.U); // u
        Assert.Equal ("TAB to jumup between text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.S); // s
        Assert.Equal ("TAB to jumusp between text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.E); // e
        Assert.Equal ("TAB to jumusep between text fields.", _textView.Text);
        _textView.NewKeyDownEvent (Key.D); // d
        Assert.Equal ("TAB to jumusedp between text fields.", _textView.Text);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordBackward_Multiline_With_Selection ()
    {
        //		          4         3          2         1
        //		  87654321098765432109876 54321098765432109876543210-Length
        //			    1         2              1         2
        //                01234567890123456789012  0123456789012345678901234
        _textView.Text = "This is the first line.\nThis is the second line.";

        _textView.MoveEnd ();
        _textView.SelectionStartColumn = _textView.CurrentColumn;
        _textView.SelectionStartRow = _textView.CurrentRow;
        var iteration = 0;
        var iterationsFinished = false;

        while (!iterationsFinished)
        {
            _textView.NewKeyDownEvent (Key.CursorLeft.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (23, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (1, _textView.SelectedLength);
                    Assert.Equal (".", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (19, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (5, _textView.SelectedLength);
                    Assert.Equal ("line.", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (12, _textView.SelectedLength);
                    Assert.Equal ("second line.", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (8, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (16, _textView.SelectedLength);
                    Assert.Equal ("the second line.", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (5, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (19, _textView.SelectedLength);
                    Assert.Equal ("is the second line.", _textView.SelectedText);

                    break;

                case 5:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (24, _textView.SelectedLength);
                    Assert.Equal ("This is the second line.", _textView.SelectedText);

                    break;

                case 6:
                    Assert.Equal (23, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (24 + Environment.NewLine.Length, _textView.SelectedLength);
                    Assert.Equal ($"{Environment.NewLine}This is the second line.", _textView.SelectedText);

                    break;

                case 7:
                    Assert.Equal (22, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (25 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($".{Environment.NewLine}This is the second line.", _textView.SelectedText);

                    break;

                case 8:
                    Assert.Equal (18, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (29 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"line.{Environment.NewLine}This is the second line.", _textView.SelectedText);

                    break;

                case 9:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (35 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);

                    break;

                case 10:
                    Assert.Equal (8, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (39 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"the first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);

                    break;

                case 11:
                    Assert.Equal (5, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (42 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"is the first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);

                    break;

                case 12:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (24, _textView.SelectionStartColumn);
                    Assert.Equal (1, _textView.SelectionStartRow);
                    Assert.Equal (47 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);

                    break;

                default:
                    iterationsFinished = true;

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordBackward_With_No_Selection ()
    {
        _textView.InsertionPoint = new Point (_textView.Text.Length, 0);
        var iteration = 0;

        while (_textView.InsertionPoint.X > 0)
        {
            _textView.NewKeyDownEvent (Key.CursorLeft.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (31, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (25, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (20, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (7, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 5:
                    Assert.Equal (4, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 6:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordBackward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
    {
        //                          1         2         3         4         5    
        //                0123456789012345678901234567890123456789012345678901234=55 (Length)
        _textView.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
        _textView.InsertionPoint = new Point (_textView.Text.Length, 0);
        var iteration = 0;

        while (_textView.InsertionPoint.X > 0)
        {
            _textView.NewKeyDownEvent (Key.CursorLeft.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (54, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (48, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (46, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (40, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (38, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 5:
                    Assert.Equal (28, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 6:
                    Assert.Equal (25, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 7:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 8:
                    Assert.Equal (9, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 9:
                    Assert.Equal (6, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 10:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordBackward_With_Selection ()
    {
        _textView.InsertionPoint = new Point (_textView.Text.Length, 0);
        _textView.SelectionStartColumn = _textView.Text.Length;
        _textView.SelectionStartRow = 0;
        var iteration = 0;

        while (_textView.InsertionPoint.X > 0)
        {
            _textView.NewKeyDownEvent (Key.CursorLeft.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (31, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (32, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (1, _textView.SelectedLength);
                    Assert.Equal (".", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (25, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (32, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (7, _textView.SelectedLength);
                    Assert.Equal ("fields.", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (20, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (32, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (12, _textView.SelectedLength);
                    Assert.Equal ("text fields.", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (32, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (20, _textView.SelectedLength);
                    Assert.Equal ("between text fields.", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (7, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (32, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (25, _textView.SelectedLength);
                    Assert.Equal ("jump between text fields.", _textView.SelectedText);

                    break;

                case 5:
                    Assert.Equal (4, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (32, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (28, _textView.SelectedLength);
                    Assert.Equal ("to jump between text fields.", _textView.SelectedText);

                    break;

                case 6:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (32, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (32, _textView.SelectedLength);
                    Assert.Equal ("TAB to jump between text fields.", _textView.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordBackward_With_The_Same_Values_For_SelectedStart_And_InsertionPoint_And_Not_Starting_At_Beginning_Of_The_Text ()
    {
        _textView.InsertionPoint = new Point (10, 0);
        _textView.SelectionStartColumn = 10;
        _textView.SelectionStartRow = 0;
        var iteration = 0;

        while (_textView.InsertionPoint.X > 0)
        {
            _textView.NewKeyDownEvent (Key.CursorLeft.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (7, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (10, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (3, _textView.SelectedLength);
                    Assert.Equal ("jum", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (4, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (10, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (6, _textView.SelectedLength);
                    Assert.Equal ("to jum", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (10, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (10, _textView.SelectedLength);
                    Assert.Equal ("TAB to jum", _textView.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordForward_Multiline_With_Selection ()
    {
        //			    1         2          3         4
        //		  01234567890123456789012 34567890123456789012345678-Length
        //			    1         2              1         2
        //                01234567890123456789012  0123456789012345678901234
        _textView.Text = "This is the first line.\nThis is the second line.";

        _textView.SelectionStartColumn = _textView.CurrentColumn;
        _textView.SelectionStartRow = _textView.CurrentRow;
        var iteration = 0;
        var iterationsFinished = false;

        while (!iterationsFinished)
        {
            _textView.NewKeyDownEvent (Key.CursorRight.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (5, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (5, _textView.SelectedLength);
                    Assert.Equal ("This ", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (8, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (8, _textView.SelectedLength);
                    Assert.Equal ("This is ", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (12, _textView.SelectedLength);
                    Assert.Equal ("This is the ", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (18, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (18, _textView.SelectedLength);
                    Assert.Equal ("This is the first ", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (22, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (22, _textView.SelectedLength);
                    Assert.Equal ("This is the first line", _textView.SelectedText);

                    break;

                case 5:
                    Assert.Equal (23, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (23, _textView.SelectedLength);
                    Assert.Equal ("This is the first line.", _textView.SelectedText);

                    break;

                case 6:
                    Assert.Equal (0, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (23 + Environment.NewLine.Length, _textView.SelectedLength);
                    Assert.Equal ($"This is the first line.{Environment.NewLine}", _textView.SelectedText);

                    break;

                case 7:
                    Assert.Equal (5, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (28 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This ", _textView.SelectedText);

                    break;

                case 8:
                    Assert.Equal (8, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (31 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This is ", _textView.SelectedText);

                    break;

                case 9:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (35 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This is the ", _textView.SelectedText);

                    break;

                case 10:
                    Assert.Equal (19, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (42 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second ", _textView.SelectedText);

                    break;

                case 11:
                    Assert.Equal (23, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (46 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line", _textView.SelectedText);

                    break;

                case 12:
                    Assert.Equal (24, _textView.InsertionPoint.X);
                    Assert.Equal (1, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (47 + Environment.NewLine.Length, _textView.SelectedLength);

                    Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.", _textView.SelectedText);

                    break;

                default:
                    iterationsFinished = true;

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordForward_With_No_Selection ()
    {
        _textView.InsertionPoint = Point.Empty;
        var iteration = 0;

        while (_textView.InsertionPoint.X < _textView.Text.Length)
        {
            _textView.NewKeyDownEvent (Key.CursorRight.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (4, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (7, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (20, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (25, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 5:
                    Assert.Equal (31, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 6:
                    Assert.Equal (32, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordForward_With_No_Selection_And_With_More_Than_Only_One_Whitespace_And_With_Only_One_Letter ()
    {
        //                          1         2         3         4         5    
        //                0123456789012345678901234567890123456789012345678901234=55 (Length)
        _textView.Text = "TAB   t  o  jump         b  etween    t ext   f ields .";
        _textView.InsertionPoint = Point.Empty;
        var iteration = 0;

        while (_textView.InsertionPoint.X < _textView.Text.Length)
        {
            _textView.NewKeyDownEvent (Key.CursorRight.WithCtrl);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (6, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (9, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (25, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (28, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 5:
                    Assert.Equal (38, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 6:
                    Assert.Equal (40, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 7:
                    Assert.Equal (46, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 8:
                    Assert.Equal (48, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 9:
                    Assert.Equal (54, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;

                case 10:
                    Assert.Equal (55, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (0, _textView.SelectedLength);
                    Assert.Equal ("", _textView.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordForward_With_Selection ()
    {
        _textView.InsertionPoint = Point.Empty;
        _textView.SelectionStartColumn = 0;
        _textView.SelectionStartRow = 0;
        var iteration = 0;

        while (_textView.InsertionPoint.X < _textView.Text.Length)
        {
            _textView.NewKeyDownEvent (Key.CursorRight.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (4, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (4, _textView.SelectedLength);
                    Assert.Equal ("TAB ", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (7, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (7, _textView.SelectedLength);
                    Assert.Equal ("TAB to ", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (12, _textView.SelectedLength);
                    Assert.Equal ("TAB to jump ", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (20, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (20, _textView.SelectedLength);
                    Assert.Equal ("TAB to jump between ", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (25, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (25, _textView.SelectedLength);
                    Assert.Equal ("TAB to jump between text ", _textView.SelectedText);

                    break;

                case 5:
                    Assert.Equal (31, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (31, _textView.SelectedLength);
                    Assert.Equal ("TAB to jump between text fields", _textView.SelectedText);

                    break;

                case 6:
                    Assert.Equal (32, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (0, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (32, _textView.SelectedLength);
                    Assert.Equal ("TAB to jump between text fields.", _textView.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordForward_With_The_Same_Values_For_SelectedStart_And_InsertionPoint_And_Not_Starting_At_Beginning_Of_The_Text ()
    {
        _textView.InsertionPoint = new Point (10, 0);
        _textView.SelectionStartColumn = 10;
        _textView.SelectionStartRow = 0;
        var iteration = 0;

        while (_textView.InsertionPoint.X < _textView.Text.Length)
        {
            _textView.NewKeyDownEvent (Key.CursorRight.WithCtrl.WithShift);

            switch (iteration)
            {
                case 0:
                    Assert.Equal (12, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (10, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (2, _textView.SelectedLength);
                    Assert.Equal ("p ", _textView.SelectedText);

                    break;

                case 1:
                    Assert.Equal (20, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (10, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (10, _textView.SelectedLength);
                    Assert.Equal ("p between ", _textView.SelectedText);

                    break;

                case 2:
                    Assert.Equal (25, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (10, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (15, _textView.SelectedLength);
                    Assert.Equal ("p between text ", _textView.SelectedText);

                    break;

                case 3:
                    Assert.Equal (31, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (10, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (21, _textView.SelectedLength);
                    Assert.Equal ("p between text fields", _textView.SelectedText);

                    break;

                case 4:
                    Assert.Equal (32, _textView.InsertionPoint.X);
                    Assert.Equal (0, _textView.InsertionPoint.Y);
                    Assert.Equal (10, _textView.SelectionStartColumn);
                    Assert.Equal (0, _textView.SelectionStartRow);
                    Assert.Equal (22, _textView.SelectedLength);
                    Assert.Equal ("p between text fields.", _textView.SelectedText);

                    break;
            }

            iteration++;
        }
    }

    [Fact]
    [SetupFakeApplication]
    public void WordWrap_Deleting_Backwards ()
    {
        var tv = new TextView { Width = 5, Height = 2, WordWrap = true, Text = "aaaa" };
        var top = new Runnable ();
        top.Add (tv);
        Application.Begin (top);
        SetupFakeApplicationAttribute.RunIteration ();

        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.Viewport.X);

        DriverAssert.AssertDriverContentsAre (@"
aaaa
",
                                              _output);

        tv.InsertionPoint = new Point (5, 0);
        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (0, tv.Viewport.X);

        DriverAssert.AssertDriverContentsAre (@"
aaa
",
                                              _output);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (0, tv.Viewport.X);

        DriverAssert.AssertDriverContentsAre (@"
aa
",
                                              _output);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (0, tv.Viewport.X);

        DriverAssert.AssertDriverContentsAre (@"
a
",
                                              _output);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (0, tv.Viewport.X);

        DriverAssert.AssertDriverContentsAre (@"

",
                                              _output);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        SetupFakeApplicationAttribute.RunIteration ();
        Assert.Equal (0, tv.Viewport.X);

        DriverAssert.AssertDriverContentsAre (@"

",
                                              _output);
        top.Dispose ();
    }

    [Theory]
    [TextViewTestsSetupFakeApplication]
    [InlineData (KeyCode.Delete)]
    public void WordWrap_Draw_Typed_Keys_After_Text_Is_Deleted (KeyCode del)
    {
        var top = new Runnable ();
        top.Add (_textView);
        _textView.Text = "Line 1.\nLine 2.";
        _textView.WordWrap = true;
        Application.Begin (top);
        SetupFakeApplicationAttribute.RunIteration ();

        Assert.True (_textView.WordWrap);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Line 1.
Line 2.",
                                                       _output);

        Assert.True (_textView.NewKeyDownEvent (Key.End.WithShift));
        Assert.Equal ("Line 1.", _textView.SelectedText);

        Assert.True (_textView.NewKeyDownEvent (new Key (del)));
        SetupFakeApplicationAttribute.RunIteration ();
        DriverAssert.AssertDriverContentsWithFrameAre ("Line 2.", _output);

        Assert.True (_textView.NewKeyDownEvent (Key.H.WithShift));
        Assert.NotEqual (Rectangle.Empty, _textView.NeedsDrawRect);
        SetupFakeApplicationAttribute.RunIteration ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
H      
Line 2.",
                                                       _output);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeApplication]
    public void WordWrap_Not_Throw_If_Width_Is_Less_Than_Zero ()
    {
        Exception exception = Record.Exception (() =>
                                                {
                                                    var tv = new TextView
                                                    {
                                                        Width = Dim.Fill (),
                                                        Height = Dim.Fill (),
                                                        WordWrap = true,
                                                        Text = "これは、左右のクリップ境界をテストするための非常に長いテキストです。"
                                                    };
                                                });
        Assert.Null (exception);
    }

    [Fact]
    [SetupFakeApplication]
    public void WordWrap_ReadOnly_InsertionPoint_SelectedText_Copy ()
    {
        //          0123456789
        var text = "This is the first line.\nThis is the second line.\n";

        var tv = new TextView { Width = 11, Height = 9, App = ApplicationImpl.Instance };
        tv.Text = text;

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
        tv.WordWrap = true;

        var top = new Runnable { Driver = ApplicationImpl.Instance.Driver };
        top.Add (tv);
        top.Layout ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is  
the first
line.    
This is  
the      
second   
line.    
",
                                                       _output);

        tv.ReadOnly = true;
        tv.InsertionPoint = new Point (6, 2);
        Assert.Equal (new Point (5, 2), tv.InsertionPoint);
        top.LayoutSubViews ();
        top.SetClipToScreen ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is   
the first 
line.     
This is   
the second
line.     
",
                                                       _output);

        tv.SelectionStartRow = 0;
        tv.SelectionStartColumn = 0;
        Assert.Equal ("This is the first line.", tv.SelectedText);

        tv.Copy ();
        Assert.Equal ("This is the first line.", Clipboard.Contents);
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordWrap_True_LoadStream_New_Text ()
    {
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
        _textView.WordWrap = true;
        Assert.Equal ("TAB to jump between text fields.", _textView.Text);
        var text = "This is the first line.\nThis is the second line.\n";

        using (var stream = new MemoryStream ())
        {
            var writer = new StreamWriter (stream);
            writer.Write (text);
            writer.Flush ();
            stream.Position = 0;

            _textView.Load (stream);

            Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", _textView.Text);
            Assert.True (_textView.WordWrap);
        }
    }

    [Fact]
    [TextViewTestsSetupFakeApplication]
    public void WordWrap_WrapModel_Output ()
    {
        //          0123456789
        var text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView { Width = 10, Height = 10 };
        tv.Text = text;

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
        tv.WordWrap = true;

        var top = new Runnable { Driver = ApplicationImpl.Instance.Driver };
        top.Add (tv);

        top.Layout ();
        tv.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
This is
the    
first  
line.  
This is
the    
second 
line.  
",
                                                       _output);
    }

    private TextView CreateTextView () => new () { Width = 30, Height = 10 };

    private int GetLeftCol (int start)
    {
        string [] lines = _textView.Text.Split (Environment.NewLine);

        if (lines is { Length: 0 })
        {
            return 0;
        }

        if (start == _textView.Viewport.X)
        {
            return start;
        }

        if (_textView.Viewport.X == _textView.CurrentColumn)
        {
            return _textView.CurrentColumn;
        }

        int cCol = _textView.CurrentColumn;
        string line = lines [_textView.CurrentRow];
        int lCount = cCol > line.Length - 1 ? line.Length - 1 : cCol;
        int width = _textView.Frame.Width;
        int tabWidth = _textView.TabWidth;
        var sumLength = 0;
        var col = 0;

        for (int i = lCount; i >= 0; i--)
        {
            char r = line [i];
            sumLength += ((Rune)r).GetColumns ();

            if (r == '\t')
            {
                sumLength += tabWidth + 1;
            }

            if (sumLength > width)
            {
                if (col + width == cCol)
                {
                    col++;
                }

                break;
            }

            if ((cCol < line.Length && col > 0 && start < cCol && col == start) || cCol - col == width - 1)
            {
                break;
            }

            col = i;
        }

        return col;
    }

    // This class enables test functions annotated with the [InitShutdown] attribute
    // to have a function called before the test function is called and after.
    // 
    // This is necessary because a) Application is a singleton and Init/Shutdown must be called
    // as a pair, and b) all unit test functions should be atomic.
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
    public class TextViewTestsSetupFakeApplication : SetupFakeApplicationAttribute
    {
        public static string Txt = "TAB to jump between text fields.";

        public override void After (MethodInfo methodUnderTest)
        {
            _textView = null;
            base.After (methodUnderTest);
        }

        public override void Before (MethodInfo methodUnderTest)
        {
            base.Before (methodUnderTest);

            //                   1         2         3 
            //         01234567890123456789012345678901=32 (Length)
            byte [] buff = Encoding.Unicode.GetBytes (Txt);
            byte [] ms = new MemoryStream (buff).ToArray ();

            _textView = new TextView { App = ApplicationImpl.Instance, Width = 30, Height = 10, SchemeName = "Base" };
            _textView.Text = Encoding.Unicode.GetString (ms);
        }
    }
}
