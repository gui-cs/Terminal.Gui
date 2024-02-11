using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class LabelTests
{
    private readonly ITestOutputHelper _output;
    public LabelTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Stays_True_AnchorEnd ()
    {
        var label = new Label { Y = Pos.Center (), Text = "Say Hello 你", AutoSize = true };
        label.X = Pos.AnchorEnd () - Pos.Function (() => label.TextFormatter.Text.GetColumns ());

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        Application.Top.Add (win);

        Assert.True (label.AutoSize);

        Application.Begin (Application.Top);
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
        Application.Top.Add (win);

        Assert.True (label.AutoSize);

        Application.Begin (Application.Top);
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
        Application.Top.Add (win);

        Assert.True (label.AutoSize);

        label.Text = "Say Hello 你";

        Assert.True (label.AutoSize);

        Application.Begin (Application.Top);
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
    [AutoInitShutdown]
    public void Constructors_Defaults ()
    {
        var label = new Label ();
        Assert.Equal (string.Empty, label.Text);
        Application.Top.Add (label);
        RunState rs = Application.Begin (Application.Top);

        Assert.Equal (TextAlignment.Left, label.TextAlignment);
        Assert.True (label.AutoSize);
        Assert.False (label.CanFocus);
        Assert.Equal (new Rect (0, 0, 0, 0), label.Frame);
        Assert.Equal (KeyCode.Null, label.HotKey);
        var expected = @"";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);

        label = new Label { Text = "Test" };
        Assert.True (label.AutoSize);
        Assert.Equal ("Test", label.Text);
        Application.Top.Add (label);
        rs = Application.Begin (Application.Top);

        Assert.Equal ("Test", label.TextFormatter.Text);
        Assert.Equal (new Rect (0, 0, 4, 1), label.Frame);

        expected = @"
Test
";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);

        label = new Label { X = 3, Y = 4, Text = "Test" };
        Assert.Equal ("Test", label.Text);
        Application.Top.Add (label);
        rs = Application.Begin (Application.Top);

        Assert.Equal ("Test", label.TextFormatter.Text);
        Assert.Equal (new Rect (3, 4, 4, 1), label.Frame);

        expected = @"
   Test
";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_Draw_Fill_Remaining_AutoSize_True ()
    {
        var label = new Label { Text = "This label needs to be cleared before rewritten." };

        var tf1 = new TextFormatter { Direction = TextDirection.LeftRight_TopBottom };
        tf1.Text = "This TextFormatter (tf1) without fill will not be cleared on rewritten.";
        Size tf1Size = tf1.Size;

        var tf2 = new TextFormatter { Direction = TextDirection.LeftRight_TopBottom };
        tf2.Text = "This TextFormatter (tf2) with fill will be cleared on rewritten.";
        Size tf2Size = tf2.Size;

        Application.Top.Add (label);
        Application.Begin (Application.Top);

        Assert.True (label.AutoSize);

        tf1.Draw (
                  new Rect (new Point (0, 1), tf1Size),
                  label.GetNormalColor (),
                  label.ColorScheme.HotNormal,
                  default (Rect),
                  false
                 );

        tf2.Draw (new Rect (new Point (0, 2), tf2Size), label.GetNormalColor (), label.ColorScheme.HotNormal);

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
                  new Rect (new Point (0, 1), tf1Size),
                  label.GetNormalColor (),
                  label.ColorScheme.HotNormal,
                  default (Rect),
                  false
                 );

        tf2.Text = "This TextFormatter (tf2) is rewritten.";
        tf2.Draw (new Rect (new Point (0, 2), tf2Size), label.GetNormalColor (), label.ColorScheme.HotNormal);

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
        Application.Top.Add (label);
        Application.Begin (Application.Top);

        Assert.True (label.AutoSize);
        Assert.Equal (new Rect (0, 0, 16, 1), label.Frame);

        var expected = @"
Demo Simple Rune
";

        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 16, 1), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_Draw_Vertical_Simple_Runes ()
    {
        var label = new Label { TextDirection = TextDirection.TopBottom_LeftRight, Text = "Demo Simple Rune" };
        Application.Top.Add (label);
        Application.Begin (Application.Top);

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

        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 1, 16), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_Draw_Vertical_Wide_Runes ()
    {
        var label = new Label { TextDirection = TextDirection.TopBottom_LeftRight, Text = "デモエムポンズ" };
        Application.Top.Add (label);
        Application.Begin (Application.Top);

        var expected = @"
デ
モ
エ
ム
ポ
ン
ズ
";

        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 2, 7), pos);
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
        Application.Top.Add (win);

        Assert.False (label.IsInitialized);

        Application.Begin (Application.Top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.True (label.IsInitialized);
        Assert.Equal ("Say Hello 你", label.Text);
        Assert.Equal ("Say Hello 你", label.TextFormatter.Text);
        Assert.Equal (new Rect (0, 0, 12, 1), label.Bounds);

        var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";
        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 30, 5), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Update_Parameterless_Only_On_Or_After_Initialize ()
    {
        var label = new Label { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你", AutoSize = true };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        Application.Top.Add (win);

        Assert.False (label.IsInitialized);

        Application.Begin (Application.Top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.True (label.IsInitialized);
        Assert.Equal ("Say Hello 你", label.Text);
        Assert.Equal ("Say Hello 你", label.TextFormatter.Text);
        Assert.Equal (new Rect (0, 0, 12, 1), label.Bounds);

        var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 30, 5), pos);
    }
}
