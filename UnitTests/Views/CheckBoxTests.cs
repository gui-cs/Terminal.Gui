using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class CheckBoxTests (ITestOutputHelper output)
{
    private static readonly Size _size25x1 = new (25, 1);

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
    public void CheckBox_AbsoluteSize_Text (string text, int width, int height, int expectedWidth, int expectedHeight)
    {
        var checkBox = new CheckBox
        {
            X = 0,
            Y = 0,
            Width = width,
            Height = height,
            Text = text
        };

        Assert.Equal (new (expectedWidth, expectedHeight), checkBox.Frame.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), checkBox.Viewport.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), checkBox.TextFormatter.ConstrainToSize);

        checkBox.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (1, 0, 1, 0)]
    [InlineData (0, 1, 0, 1)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (10, 1, 10, 1)]
    [InlineData (10, 3, 10, 3)]
    public void CheckBox_AbsoluteSize_DefaultText (int width, int height, int expectedWidth, int expectedHeight)
    {
        var checkBox = new CheckBox
        {
            X = 0,
            Y = 0,
            Width = width,
            Height = height
        };

        Assert.Equal (new (expectedWidth, expectedHeight), checkBox.Frame.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), checkBox.Viewport.Size);
        Assert.Equal (new (expectedWidth, expectedHeight), checkBox.TextFormatter.ConstrainToSize);

        checkBox.Dispose ();
    }

    // Test that Title and Text are the same
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var view = new CheckBox ();
        view.Title = "Hello";
        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);

        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{CM.Glyphs.CheckStateUnChecked} Hello", view.TextFormatter.Text);
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new CheckBox ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{CM.Glyphs.CheckStateUnChecked} Hello", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);
    }

    [Fact]
    [SetupFakeDriver]
    public void AllowNoneChecked_Get_Set ()
    {
        var checkBox = new CheckBox { Text = "Check this out 你" };

        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
        Assert.True (checkBox.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);

        checkBox.AllowCheckStateNone = true;
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.Equal (CheckState.None, checkBox.CheckedState);
        checkBox.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @$"
{CM.Glyphs.CheckStateNone} Check this out 你",
                                                      output
                                                     );
        Assert.True (checkBox.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        Assert.True (checkBox.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (CheckState.None, checkBox.CheckedState);

        checkBox.AllowCheckStateNone = false;
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var ckb = new CheckBox ();
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        Assert.Equal (CheckState.UnChecked, ckb.CheckedState);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal (string.Empty, ckb.Text);
        Assert.Equal ($"{CM.Glyphs.CheckStateUnChecked} ", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new (0, 0, 2, 1), ckb.Frame);

        ckb = new () { Text = "Test", CheckedState = CheckState.Checked };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        Assert.Equal (CheckState.Checked, ckb.CheckedState);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{CM.Glyphs.CheckStateChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new (0, 0, 6, 1), ckb.Frame);

        ckb = new () { Text = "Test", X = 1, Y = 2 };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        Assert.Equal (CheckState.UnChecked, ckb.CheckedState);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{CM.Glyphs.CheckStateUnChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new (1, 2, 6, 1), ckb.Frame);

        ckb = new () { Text = "Test", X = 3, Y = 4, CheckedState = CheckState.Checked };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        Assert.Equal (CheckState.Checked, ckb.CheckedState);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{CM.Glyphs.CheckStateChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new (3, 4, 6, 1), ckb.Frame);
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        var toggled = false;
        var ckb = new CheckBox ();
        ckb.CheckedStateChanging += (s, e) => toggled = true;

        Assert.Equal (CheckState.UnChecked, ckb.CheckedState);
        Assert.False (toggled);
        Assert.Equal (Key.Empty, ckb.HotKey);

        ckb.Text = "_Test";
        Assert.Equal (Key.T, ckb.HotKey);
        Assert.True (ckb.NewKeyDownEvent (Key.T));
        Assert.Equal (CheckState.Checked, ckb.CheckedState);
        Assert.True (toggled);

        ckb.Text = "T_est";
        toggled = false;
        Assert.Equal (Key.E, ckb.HotKey);
        Assert.True (ckb.NewKeyDownEvent (Key.E.WithAlt));
        Assert.True (toggled);
        Assert.Equal (CheckState.UnChecked, ckb.CheckedState);

        toggled = false;
        Assert.Equal (Key.E, ckb.HotKey);
        Assert.True (ckb.NewKeyDownEvent (Key.E));
        Assert.True (toggled);
        Assert.Equal (CheckState.Checked, ckb.CheckedState);

        toggled = false;
        Assert.True (ckb.NewKeyDownEvent (Key.Space));
        Assert.True (toggled);
        Assert.Equal (CheckState.UnChecked, ckb.CheckedState);

        toggled = false;
        Assert.True (ckb.NewKeyDownEvent (Key.Space));
        Assert.True (toggled);
        Assert.Equal (CheckState.Checked, ckb.CheckedState);

        toggled = false;
        Assert.False (ckb.NewKeyDownEvent (Key.Enter));
        Assert.False (toggled);
        Assert.Equal (CheckState.Checked, ckb.CheckedState);
    }

    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var ckb = new CheckBox ();
        var acceptInvoked = false;

        ckb.Accept += ViewOnAccept;

        bool? ret = ckb.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        return;

        void ViewOnAccept (object sender, HandledEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void TextAlignment_Centered ()
    {
        var checkBox = new CheckBox
        {
            X = 1,
            Y = Pos.Center (),
            Text = "Check this out 你",
            TextAlignment = Alignment.Center,
            Width = 25
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill (), Title = "Test Demo 你" };
        win.Add (checkBox);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 5);

        Assert.Equal (Alignment.Center, checkBox.TextAlignment);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.ConstrainToSize);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│    {CM.Glyphs.CheckStateUnChecked} Check this out 你     │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.CheckedState = CheckState.Checked;
        Application.Refresh ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│    {CM.Glyphs.CheckStateChecked} Check this out 你     │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TextAlignment_Justified ()
    {
        var checkBox1 = new CheckBox
        {
            X = 1,
            Y = Pos.Center (),
            Text = "Check first out 你",
            TextAlignment = Alignment.Fill,
            Width = 25
        };

        var checkBox2 = new CheckBox
        {
            X = 1,
            Y = Pos.Bottom (checkBox1),
            Text = "Check second out 你",
            TextAlignment = Alignment.Fill,
            Width = 25
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill (), Title = "Test Demo 你" };
        win.Add (checkBox1, checkBox2);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 6);

        Assert.Equal (Alignment.Fill, checkBox1.TextAlignment);
        Assert.Equal (new (1, 1, 25, 1), checkBox1.Frame);
        Assert.Equal (Alignment.Fill, checkBox2.TextAlignment);
        Assert.Equal (new (1, 2, 25, 1), checkBox2.Frame);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.CheckStateUnChecked}   Check  first  out  你  │
│ {CM.Glyphs.CheckStateUnChecked}  Check  second  out  你  │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 6), pos);

        checkBox1.CheckedState = CheckState.Checked;
        Assert.Equal (new (1, 1, 25, 1), checkBox1.Frame);
        Assert.Equal (_size25x1, checkBox1.TextFormatter.ConstrainToSize);
        checkBox2.CheckedState = CheckState.Checked;
        Assert.Equal (new (1, 2, 25, 1), checkBox2.Frame);
        Assert.Equal (_size25x1, checkBox2.TextFormatter.ConstrainToSize);
        Application.Refresh ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.CheckStateChecked}   Check  first  out  你  │
│ {CM.Glyphs.CheckStateChecked}  Check  second  out  你  │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 6), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TextAlignment_Left ()
    {
        var checkBox = new CheckBox
        {
            X = 1,
            Y = Pos.Center (),
            Text = "Check this out 你",
            Width = 25
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill (), Title = "Test Demo 你" };
        win.Add (checkBox);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 5);

        Assert.Equal (Alignment.Start, checkBox.TextAlignment);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.ConstrainToSize);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.CheckStateUnChecked} Check this out 你        │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.CheckedState = CheckState.Checked;
        Application.Refresh ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.CheckStateChecked} Check this out 你        │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TextAlignment_Right ()
    {
        var checkBox = new CheckBox
        {
            X = 1,
            Y = Pos.Center (),
            Text = "Check this out 你",
            TextAlignment = Alignment.End,
            Width = 25
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill (), Title = "Test Demo 你" };
        win.Add (checkBox);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 5);

        Assert.Equal (Alignment.End, checkBox.TextAlignment);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.ConstrainToSize);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│       Check this out 你 {CM.Glyphs.CheckStateUnChecked}  │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.CheckedState = CheckState.Checked;
        Application.Refresh ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│       Check this out 你 {CM.Glyphs.CheckStateChecked}  │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);
        top.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Fires_Accept ()
    {
        var cb = new CheckBox ();
        var accepted = false;

        cb.Accept += CheckBoxOnAccept;
        cb.InvokeCommand (Command.HotKey);

        Assert.True (accepted);

        return;

        void CheckBoxOnAccept (object sender, HandledEventArgs e) { accepted = true; }
    }

    [Theory]
    [InlineData (CheckState.Checked)]
    [InlineData (CheckState.UnChecked)]
    [InlineData (CheckState.None)]
    public void Toggled_Cancel_Event_Prevents_Toggle (CheckState initialState)
    {
        var ckb = new CheckBox { AllowCheckStateNone = true };
        var checkedInvoked = false;

        ckb.CheckedStateChanging += CheckBoxToggle;

        ckb.CheckedState = initialState;
        Assert.Equal (initialState, ckb.CheckedState);
        bool? ret = ckb.AdvanceCheckState ();
        Assert.True (ret);
        Assert.True (checkedInvoked);
        Assert.Equal (initialState, ckb.CheckedState);

        return;

        void CheckBoxToggle (object sender, CancelEventArgs e)
        {
            checkedInvoked = true;
            e.Cancel = true;
        }
    }
}
