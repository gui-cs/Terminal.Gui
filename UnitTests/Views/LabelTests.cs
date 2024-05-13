using System.ComponentModel;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class LabelTests
{
    private readonly ITestOutputHelper _output;
    public LabelTests (ITestOutputHelper output) { _output = output; }

    // Test that Title and Text are the same
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var label = new Label ();
        label.Title = "Hello";
        Assert.Equal ("Hello", label.Title);
        Assert.Equal ("Hello", label.TitleTextFormatter.Text);

        Assert.Equal ("Hello", label.Text);
        Assert.Equal ("Hello", label.TextFormatter.Text);
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var label = new Label ();
        label.Text = "Hello";
        Assert.Equal ("Hello", label.Text);
        Assert.Equal ("Hello", label.TextFormatter.Text);

        Assert.Equal ("Hello", label.Title);
        Assert.Equal ("Hello", label.TitleTextFormatter.Text);
    }

    [Fact]
    public void HotKey_Command_SetsFocus_OnNextSubview ()
    {
        var superView = new View () { CanFocus = true };
        var label = new Label ();
        var nextSubview = new View () { CanFocus = true };
        superView.Add (label, nextSubview);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (label.HasFocus);
        Assert.False (nextSubview.HasFocus);

        label.InvokeCommand (Command.HotKey);
        Assert.False (label.HasFocus);
        Assert.True (nextSubview.HasFocus);
    }


    [Fact]
    public void MouseClick_SetsFocus_OnNextSubview ()
    {
        var superView = new View () { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View () { CanFocus = true, Width = 1, Height = 1 };
        var label = new Label () { X = 2, Title = "_x" };
        var nextSubview = new View () { CanFocus = true, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, label, nextSubview);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (focusedView.HasFocus);
        Assert.False (label.HasFocus);
        Assert.False (nextSubview.HasFocus);

        label.NewMouseEvent (new MouseEvent () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked });
        Assert.False (label.HasFocus);
        Assert.True (nextSubview.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var label = new Label ();
        var accepted = false;

        label.Accept += LabelOnAccept;
        label.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;
        void LabelOnAccept (object sender, CancelEventArgs e) { accepted = true; }
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Stays_True_AnchorEnd ()
    {
        var label = new Label { Y = Pos.Center (), Text = "Say Hello 你" };
        label.X = Pos.AnchorEnd (0) - Pos.Function (() => label.TextFormatter.Text.GetColumns ());

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);

       

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        var expected = @"
┌────────────────────────────┐
│                            │
│                Say Hello 你│
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

       
        label.Text = "Say Hello 你 changed";
       
        Application.Refresh ();

        expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你 changed│
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Stays_True_Center ()
    {
        var label = new Label { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

       
        label.Text = "Say Hello 你 changed";
       
        Application.Refresh ();

        expected = @"
┌────────────────────────────┐
│                            │
│    Say Hello 你 changed    │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Stays_True_With_EmptyText ()
    {
        var label = new Label { X = Pos.Center (), Y = Pos.Center () };

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);

       

        label.Text = "Say Hello 你";

       

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var label = new Label ();
        Assert.Equal (string.Empty, label.Text);
        Assert.Equal (TextAlignment.Left, label.TextAlignment);
        Assert.False (label.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 0, 0), label.Frame);
        Assert.Equal (KeyCode.Null, label.HotKey);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_Draw_Fill_Remaining_AutoSize_False ()
    {
        Size tfSize = new Size (80, 1);

        var label = new Label { Text = "This label needs to be cleared before rewritten.", Width = tfSize.Width, Height = tfSize.Height };

        var tf1 = new TextFormatter { Direction = TextDirection.LeftRight_TopBottom, Size = tfSize };
        tf1.Text = "This TextFormatter (tf1) without fill will not be cleared on rewritten.";

        var tf2 = new TextFormatter { Direction = TextDirection.LeftRight_TopBottom, Size = tfSize, FillRemaining = true };
        tf2.Text = "This TextFormatter (tf2) with fill will be cleared on rewritten.";

        var top = new Toplevel ();
        top.Add (label);
        Application.Begin (top);

        Assert.False (label.TextFormatter.AutoSize);
        Assert.False (tf1.AutoSize);
        Assert.False (tf2.AutoSize);
        Assert.False (label.TextFormatter.FillRemaining);
        Assert.False (tf1.FillRemaining);
        Assert.True (tf2.FillRemaining);

        tf1.Draw (new Rectangle (new Point (0, 1), tfSize), label.GetNormalColor (), label.ColorScheme.HotNormal);

        tf2.Draw (new Rectangle (new Point (0, 2), tfSize), label.GetNormalColor (), label.ColorScheme.HotNormal);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This label needs to be cleared before rewritten.                       
This TextFormatter (tf1) without fill will not be cleared on rewritten.
This TextFormatter (tf2) with fill will be cleared on rewritten.       ",
                                                      _output
                                                     );

        Assert.False (label.NeedsDisplay);
        Assert.False (label.LayoutNeeded);
        Assert.False (label.SubViewNeedsDisplay);
        label.Text = "This label is rewritten.";
        Assert.True (label.NeedsDisplay);
        Assert.True (label.LayoutNeeded);
        Assert.False (label.SubViewNeedsDisplay);
        label.Draw ();

        tf1.Text = "This TextFormatter (tf1) is rewritten.";
        tf1.Draw (new Rectangle (new Point (0, 1), tfSize), label.GetNormalColor (), label.ColorScheme.HotNormal);

        tf2.Text = "This TextFormatter (tf2) is rewritten.";
        tf2.Draw (new Rectangle (new Point (0, 2), tfSize), label.GetNormalColor (), label.ColorScheme.HotNormal);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This label is rewritten.                                               
This TextFormatter (tf1) is rewritten.will not be cleared on rewritten.
This TextFormatter (tf2) is rewritten.                                 ",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_Draw_Horizontal_Simple_Runes ()
    {
        var label = new Label { Text = "Demo Simple Rune" };
        var top = new Toplevel ();
        top.Add (label);
        Application.Begin (top);

       
        Assert.Equal (new Rectangle (0, 0, 16, 1), label.Frame);

        var expected = @"
Demo Simple Rune
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 16, 1), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_Draw_Vertical_Simple_Runes ()
    {
        var label = new Label { TextDirection = TextDirection.TopBottom_LeftRight, Text = "Demo Simple Rune" };
        var top = new Toplevel ();
        top.Add (label);
        Application.Begin (top);

        Assert.NotNull (label.Width);
        Assert.NotNull (label.Height);

        var expected = @"
D
e
m
o
 
S
i
m
p
l
e
 
R
u
n
e
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 1, 16), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_Draw_Vertical_Wide_Runes ()
    {
        var label = new Label { TextDirection = TextDirection.TopBottom_LeftRight, Text = "デモエムポンズ" };
        var top = new Toplevel ();
        top.Add (label);
        Application.Begin (top);

        var expected = @"
デ
モ
エ
ム
ポ
ン
ズ
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 2, 7), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_HotKeyChanged_EventFires ()
    {
        var label = new Label { Text = "Yar" };
        label.HotKey = 'Y';

        object sender = null;
        KeyChangedEventArgs args = null;

        label.HotKeyChanged += (s, e) =>
                               {
                                   sender = s;
                                   args = e;
                               };

        label.HotKey = Key.R;
        Assert.Same (label, sender);
        Assert.Equal (KeyCode.Y | KeyCode.ShiftMask, args.OldKey);
        Assert.Equal (Key.R, args.NewKey);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_HotKeyChanged_EventFires_WithNone ()
    {
        var label = new Label ();

        object sender = null;
        KeyChangedEventArgs args = null;

        label.HotKeyChanged += (s, e) =>
                               {
                                   sender = s;
                                   args = e;
                               };

        label.HotKey = KeyCode.R;
        Assert.Same (label, sender);
        Assert.Equal (KeyCode.Null, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
    }

    [Fact]
    public void TestAssignTextToLabel ()
    {
        View b = new Label { Text = "heya" };
        Assert.Equal ("heya", b.Text);
        Assert.Contains ("heya", b.TextFormatter.Text);
        b.Text = "heyb";
        Assert.Equal ("heyb", b.Text);
        Assert.Contains ("heyb", b.TextFormatter.Text);

        // with cast
        Assert.Equal ("heyb", ((Label)b).Text);
    }

    [Fact]
    [AutoInitShutdown]
    public void Update_Only_On_Or_After_Initialize ()
    {
        var label = new Label { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);

        Assert.False (label.IsInitialized);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.True (label.IsInitialized);
        Assert.Equal ("Say Hello 你", label.Text);
        Assert.Equal ("Say Hello 你", label.TextFormatter.Text);
        Assert.Equal (new Rectangle (0, 0, 12, 1), label.Viewport);

        var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
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
        var label = new Label { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);

        Assert.False (label.IsInitialized);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.True (label.IsInitialized);
        Assert.Equal ("Say Hello 你", label.Text);
        Assert.Equal ("Say Hello 你", label.TextFormatter.Text);
        Assert.Equal (new Rectangle (0, 0, 12, 1), label.Viewport);

        var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 30, 5), pos);
    }


    [Fact]
    [SetupFakeDriver]
    public void Full_Border ()
    {
        var label = new Label { BorderStyle = LineStyle.Single , Text = "Test",} ;
        label.BeginInit();
        label.EndInit();
        label.SetRelativeLayout (Application.Driver.Screen.Size);

        Assert.Equal (new (0, 0, 4, 1), label.Viewport);
        Assert.Equal (new (0, 0, 6, 3), label.Frame);

        label.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌┤Te├┐
│Test│
└────┘",
                                                      _output
                                                     );
        label.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void With_Top_Margin_Without_Top_Border ()
    {
        var label = new Label { Text = "Test", /*Width = 6, Height = 3,*/ BorderStyle = LineStyle.Single };
        label.Margin.Thickness = new Thickness (0, 1, 0, 0);
        label.Border.Thickness = new Thickness (1, 0, 1, 1);
        var top = new Toplevel ();
        top.Add (label);
        Application.Begin (top);

        Assert.Equal (new (0, 0, 6, 3), label.Frame);
        Assert.Equal (new (0, 0, 4, 1), label.Viewport);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│Test│
└────┘",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void Without_Top_Border ()
    {
        var label = new Label { Text = "Test", /* Width = 6, Height = 3, */BorderStyle = LineStyle.Single };
        label.Border.Thickness = new Thickness (1, 0, 1, 1);
        var top = new Toplevel ();
        top.Add (label);
        Application.Begin (top);

        Assert.Equal (new (0, 0, 6, 2), label.Frame);
        Assert.Equal (new (0, 0, 4, 1), label.Viewport);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│Test│
└────┘",
                                                      _output
                                                     );
    }

    // These tests were formally in AutoSizetrue.cs. They are (poor) Label tests.
    private readonly string [] expecteds = new string [21]
  {
        @"
┌────────────────────┐
│View with long text │
│                    │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 0             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 1             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 2             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 3             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 4             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 5             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 6             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 7             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 8             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 9             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 10            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 11            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 12            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 13            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 14            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 15            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 16            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 17            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 18            │
│Label 18            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 18            │
│Label 19            │
│Label 19            │
└────────────────────┘"
  };

    private static readonly Size _size1x1 = new (1, 1);

    // TODO: This is a Label test. Move to label tests if there's not already a test for this.
    [Fact]
    [AutoInitShutdown]
    public void AnchorEnd_Better_Than_Bottom_Equal_Inside_Window ()
    {
        var win = new Window ();

        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            //Width = Dim.Fill (),
            X = 0, // keep unit test focused; don't use Center here
            Y = Pos.AnchorEnd (1)
        };

        win.Add (label);

        Toplevel top = new ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (40, 10);

        Assert.Equal (29, label.Text.Length);
        Assert.Equal (new Rectangle (0, 0, 40, 10), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 40, 10), win.Frame);
        Assert.Equal (new Rectangle (0, 7, 29, 1), label.Frame);

        var expected = @"
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│This should be the last line.         │
└──────────────────────────────────────┘
"
        ;

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void AnchorEnd_Better_Than_Bottom_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
    {
        var win = new Window ();

        // Label is AutoSize == true
        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            X = 0,
            Y = Pos.AnchorEnd (1)
        };

        win.Add (label);

        var menu = new MenuBar { Menus = new MenuBarItem [] { new ("Menu", "", null) } };
        var status = new StatusBar (new StatusItem [] { new (KeyCode.F1, "~F1~ Help", null) });
        Toplevel top = new ();
        top.Add (win, menu, status);
        RunState rs = Application.Begin (top);

        Assert.Equal (new Rectangle (0, 0, 80, 25), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 80, 1), menu.Frame);
        Assert.Equal (new Rectangle (0, 24, 80, 1), status.Frame);
        Assert.Equal (new Rectangle (0, 1, 80, 23), win.Frame);
        Assert.Equal (new Rectangle (0, 20, 29, 1), label.Frame);

        var expected = @"
 Menu                                                                           
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│This should be the last line.                                                 │
└──────────────────────────────────────────────────────────────────────────────┘
 F1 Help                                                                        
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    // TODO: This is a Label test. Move to label tests if there's not already a test for this.
    [Fact]
    [AutoInitShutdown]
    public void Bottom_Equal_Inside_Window ()
    {
        var win = new Window ();

        // Label is AutoSize == true
        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            //Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (win)
                - 3 // two lines top and bottom borders more one line above the bottom border
        };

        win.Add (label);

        Toplevel top = new ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (40, 10);

        Assert.Equal (new Rectangle (0, 0, 40, 10), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 40, 10), win.Frame);
        Assert.Equal (new Rectangle (0, 7, 29, 1), label.Frame);

        var expected = @"
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│This should be the last line.         │
└──────────────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    // TODO: This is a Label test. Move to label tests if there's not already a test for this.

    [Fact]
    [AutoInitShutdown]
    public void Bottom_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
    {
        var win = new Window ();

        // Label is AutoSize == true
        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            //Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (win) - 4 // two lines top and bottom borders more two lines above border
        };

        win.Add (label);

        var menu = new MenuBar { Menus = new MenuBarItem [] { new ("Menu", "", null) } };
        var status = new StatusBar (new StatusItem [] { new (KeyCode.F1, "~F1~ Help", null) });
        Toplevel top = new ();
        top.Add (win, menu, status);
        RunState rs = Application.Begin (top);

        Assert.Equal (new Rectangle (0, 0, 80, 25), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 80, 1), menu.Frame);
        Assert.Equal (new Rectangle (0, 24, 80, 1), status.Frame);
        Assert.Equal (new Rectangle (0, 1, 80, 23), win.Frame);
        Assert.Equal (new Rectangle (0, 20, 29, 1), label.Frame);

        var expected = @"
 Menu                                                                           
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│This should be the last line.                                                 │
└──────────────────────────────────────────────────────────────────────────────┘
 F1 Help                                                                        
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }


    // TODO: This is a Dim test. Move to Dim tests.

    [Fact]
    [AutoInitShutdown]
    public void Dim_Subtract_Operator_With_Text ()
    {
        Toplevel top = new ();

        var view = new View
        {
            Text = "View with long text",
            X = 0,
            Y = 0,
            Width = 20,
            Height = 1
        };
        var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
        var count = 20;

        // Label is AutoSize == true
        List<Label> listLabels = new ();

        for (var i = 0; i < count; i++)
        {
            field.Text = $"Label {i}";
            var label = new Label { Text = field.Text, X = 0, Y = i + 1 /*, Width = 10*/ };
            view.Add (label);
            Assert.Equal ($"Label {i}", label.Text);
            Assert.Equal ($"Absolute({i + 1})", label.Y.ToString ());
            listLabels.Add (label);

            if (i == 0)
            {
                Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
                view.Height += 1;
                Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
            }
            else
            {
                Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
                view.Height += 1;
                Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
            }
        }

        field.KeyDown += (s, k) =>
        {
            if (k.KeyCode == KeyCode.Enter)
            {
                ((FakeDriver)Application.Driver).SetBufferSize (22, count + 4);
                Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], _output);
                Assert.Equal (new Rectangle (0, 0, 22, count + 4), pos);

                if (count > 0)
                {
                    Assert.Equal ($"Label {count - 1}", listLabels [count - 1].Text);
                    view.Remove (listLabels [count - 1]);
                    listLabels [count - 1].Dispose ();
                    listLabels.RemoveAt (count - 1);
                    Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
                    view.Height -= 1;
                    count--;

                    if (listLabels.Count > 0)
                    {
                        field.Text = listLabels [count - 1].Text;
                    }
                    else
                    {
                        field.Text = string.Empty;
                    }
                }

                Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
            }
        };

        Application.Iteration += (s, a) =>
        {
            while (count > -1)
            {
                field.NewKeyDownEvent (Key.Enter);

                if (count == 0)
                {
                    field.NewKeyDownEvent (Key.Enter);

                    break;
                }
            }

            Application.RequestStop ();
        };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);

        Assert.Equal (0, count);
        Assert.Equal (count, listLabels.Count);
    }

    // TODO: This is a Label test. Move to Label tests.

    [Fact]
    [SetupFakeDriver]
    public void Label_Height_Zero_Stays_Zero ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);
        var text = "Label";
        var label = new Label
        {
            Text = text,
        };
        label.Width = Dim.Fill () - text.Length;
        label.Height = 0;

        var win = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        win.BeginInit ();
        win.EndInit ();
        win.LayoutSubviews ();
        win.Draw ();

        Assert.Equal (5, text.Length);
        Assert.Equal (new (0, 0, 3, 0), label.Frame);
        //Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Single (label.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), win.Frame);

        var expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);
        label.Width = Dim.Fill () - text.Length;
        win.LayoutSubviews ();
        win.Clear ();
        win.Draw ();

        Assert.Equal (Rectangle.Empty, label.Frame);
        //        Assert.Equal (new (5, 1), label.TextFormatter.Size);

        //Exception exception = Record.Exception (
        //                                        () => Assert.Equal (
        //                                                            new List<string> { string.Empty },
        //                                                            label.TextFormatter.GetLines ()
        //                                                           )
        //                                       );
        //Assert.Null (exception);

        expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Dim_Add_Operator_With_Text ()
    {
        Toplevel top = new ();

        var view = new View
        {
            Text = "View with long text",
            X = 0,
            Y = 0,
            Width = 20,
            Height = 1
        };
        var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
        var count = 0;

        // Label is AutoSize == true
        List<Label> listLabels = new ();

        field.KeyDown += (s, k) =>
        {
            if (k.KeyCode == KeyCode.Enter)
            {
                ((FakeDriver)Application.Driver).SetBufferSize (22, count + 4);
                Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], _output);
                Assert.Equal (new Rectangle (0, 0, 22, count + 4), pos);

                if (count < 20)
                {
                    field.Text = $"Label {count}";

                    // Label is AutoSize = true
                    var label = new Label { Text = field.Text, X = 0, Y = view.Viewport.Height /*, Width = 10*/ };
                    view.Add (label);
                    Assert.Equal ($"Label {count}", label.Text);
                    Assert.Equal ($"Absolute({count + 1})", label.Y.ToString ());
                    listLabels.Add (label);

                    //if (count == 0) {
                    //	Assert.Equal ($"Absolute({count})", view.Height.ToString ());
                    //	view.Height += 2;
                    //} else {
                    Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
                    view.Height += 1;

                    //}
                    count++;
                }

                Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
            }
        };

        Application.Iteration += (s, a) =>
        {
            while (count < 21)
            {
                field.NewKeyDownEvent (Key.Enter);

                if (count == 20)
                {
                    field.NewKeyDownEvent (Key.Enter);

                    break;
                }
            }

            Application.RequestStop ();
        };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);

        Assert.Equal (20, count);
        Assert.Equal (count, listLabels.Count);
    }



    [Fact]
    [AutoInitShutdown]
    public void Label_IsEmpty_False_Minimum_Height ()
    {
        var text = "Label";

        var label = new Label
        {
            //Width = Dim.Fill () - text.Length,
            Text = text
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Assert.Equal (5, text.Length);
        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Equal (["Label"], label.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), win.Frame);
        Assert.Equal (new (0, 0, 10, 4), Application.Top.Frame);

        var expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //label.Width = Dim.Fill () - text.Length;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Exception exception = Record.Exception (() => Assert.Single (label.TextFormatter.GetLines ()));
        Assert.Null (exception);

        expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_IsEmpty_False_Never_Return_Null_Lines ()
    {
        var text = "Label";

        var label = new Label
        {
            //Width = Dim.Fill () - text.Length,
            //Height = 1,
            Text = text
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Assert.Equal (5, text.Length);
        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Equal (["Label"], label.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), win.Frame);
        Assert.Equal (new (0, 0, 10, 4), Application.Top.Frame);

        var expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //label.Width = Dim.Fill () - text.Length;
        Application.Refresh ();


        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Single (label.TextFormatter.GetLines ());

        expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    public void Label_ResizeView_With_Dim_Absolute ()
    {
        var super = new View ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        var label = new Label ();

        label.Text = "New text";
        super.Add (label);
        super.LayoutSubviews ();

        Rectangle expectedLabelBounds = new (0, 0, 8, 1);
        Assert.Equal (expectedLabelBounds, label.Viewport);
        super.Dispose ();
    }

}
