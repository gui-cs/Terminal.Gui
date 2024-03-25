using System.ComponentModel;
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
        var superView = new View () { CanFocus = true, Height = 1, Width = 15};
        var focusedView = new View () { CanFocus = true, Width = 1, Height = 1 };
        var label = new Label () { X = 2, Title = "_x" };
        var nextSubview = new View () { CanFocus = true, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, label, nextSubview);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (focusedView.HasFocus);
        Assert.False (label.HasFocus);
        Assert.False (nextSubview.HasFocus);

        label.OnMouseEvent (new MouseEvent () { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked });
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
        var label = new Label { Y = Pos.Center (), Text = "Say Hello 你", AutoSize = true };
        label.X = Pos.AnchorEnd () - Pos.Function (() => label.TextFormatter.Text.GetColumns ());

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);

        Assert.True (label.AutoSize);

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

        Assert.True (label.AutoSize);
        label.Text = "Say Hello 你 changed";
        Assert.True (label.AutoSize);
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

        Assert.True (label.AutoSize);

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

        Assert.True (label.AutoSize);
        label.Text = "Say Hello 你 changed";
        Assert.True (label.AutoSize);
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
        var label = new Label { X = Pos.Center (), Y = Pos.Center (), AutoSize = true };

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);

        Assert.True (label.AutoSize);

        label.Text = "Say Hello 你";

        Assert.True (label.AutoSize);

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
        Assert.True (label.AutoSize);
        Assert.False (label.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 0, 0), label.Frame);
        Assert.Equal (KeyCode.Null, label.HotKey);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_Draw_Fill_Remaining_AutoSize_True ()
    {
        var label = new Label { Text = "This label needs to be cleared before rewritten." };

        var tf1 = new TextFormatter { Direction = TextDirection.LeftRight_TopBottom };
        tf1.Text = "This TextFormatter (tf1) without fill will not be cleared on rewritten.";
        Size tf1Size = tf1.Size;

        var tf2 = new TextFormatter { Direction = TextDirection.LeftRight_TopBottom, FillRemaining = true };
        tf2.Text = "This TextFormatter (tf2) with fill will be cleared on rewritten.";
        Size tf2Size = tf2.Size;

        var top = new Toplevel ();
        top.Add (label);
        Application.Begin (top);

        Assert.True (label.AutoSize);

        tf1.Draw (
                  new Rectangle (new Point (0, 1), tf1Size),
                  label.GetNormalColor (),
                  label.ColorScheme.HotNormal
                 );

        tf2.Draw (new Rectangle (new Point (0, 2), tf2Size), label.GetNormalColor (), label.ColorScheme.HotNormal);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This label needs to be cleared before rewritten.                       
This TextFormatter (tf1) without fill will not be cleared on rewritten.
This TextFormatter (tf2) with fill will be cleared on rewritten.       
",
                                                      _output
                                                     );

        label.Text = "This label is rewritten.";
        label.Draw ();

        tf1.Text = "This TextFormatter (tf1) is rewritten.";

        tf1.Draw (
                  new Rectangle (new Point (0, 1), tf1Size),
                  label.GetNormalColor (),
                  label.ColorScheme.HotNormal
                 );

        tf2.Text = "This TextFormatter (tf2) is rewritten.";
        tf2.Draw (new Rectangle (new Point (0, 2), tf2Size), label.GetNormalColor (), label.ColorScheme.HotNormal);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This label is rewritten.                                               
This TextFormatter (tf1) is rewritten.will not be cleared on rewritten.
This TextFormatter (tf2) is rewritten.                                 
",
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

        Assert.True (label.AutoSize);
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
        Assert.Equal (new Rectangle (0, 0, 12, 1), label.Bounds);

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
        var label = new Label { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你", AutoSize = true };
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
        Assert.Equal (new Rectangle (0, 0, 12, 1), label.Bounds);

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
    public void Full_Border ()
    {
        var label = new Label { Text = "Test", /*Width = 6, Height = 3, */BorderStyle = LineStyle.Single };
        var top = new Toplevel ();
        top.Add (label);
        Application.Begin (top);

        Assert.Equal (new (0, 0, 6, 3), label.Frame);
        Assert.Equal (new (0, 0, 4, 1), label.Bounds);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌┤Te├┐
│Test│
└────┘",
                                                      _output
                                                     );
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
        Assert.Equal (new (0, 0, 4, 1), label.Bounds);
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
        Assert.Equal (new (0, 0, 4, 1), label.Bounds);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
│Test│
└────┘",
                                                      _output
                                                     );
    }

}
