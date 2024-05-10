using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class CheckBoxTests
{
    private readonly ITestOutputHelper _output;
    private static readonly Size _size25x1 = new (25, 1);
    public CheckBoxTests (ITestOutputHelper output) { _output = output; }


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
    [InlineData ("0_12你", 10, 3, 10, 3)]
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

        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.TextFormatter.Size);

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
            Height = height,
        };

        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.TextFormatter.Size);

        checkBox.Dispose ();
    }

    // Test that Title and Text are the same
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var view = new CheckBox ();
        view.Title = "Hello";
        Assert.Equal ("Hello", view.Title);
        Assert.Equal ($"Hello", view.TitleTextFormatter.Text);

        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{CM.Glyphs.UnChecked} Hello", view.TextFormatter.Text);
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new CheckBox ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{CM.Glyphs.UnChecked} Hello", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ($"Hello", view.TitleTextFormatter.Text);
    }

    [Fact]
    [AutoInitShutdown]
    public void AllowNullChecked_Get_Set ()
    {
        var checkBox = new CheckBox { Text = "Check this out 你" };
        Toplevel top = new ();
        top.Add (checkBox);
        Application.Begin (top);

        Assert.False (checkBox.Checked);
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.True (checkBox.Checked);
        Assert.True (checkBox.NewMouseEvent (new MouseEvent { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked }));
        Assert.False (checkBox.Checked);

        checkBox.AllowNullChecked = true;
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.Null (checkBox.Checked);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @$"
{CM.Glyphs.NullChecked} Check this out 你",
                                                      _output
                                                     );
        Assert.True (checkBox.NewMouseEvent (new MouseEvent { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked }));
        Assert.True (checkBox.Checked);
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.False (checkBox.Checked);
        Assert.True (checkBox.NewMouseEvent (new MouseEvent { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked }));
        Assert.Null (checkBox.Checked);

        checkBox.AllowNullChecked = false;
        Assert.False (checkBox.Checked);
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var ckb = new CheckBox ();
        Assert.True (ckb.Width is Dim.DimAuto);
        Assert.Equal (Dim.Sized (1), ckb.Height);
        Assert.False (ckb.Checked);
        Assert.False (ckb.AllowNullChecked);
        Assert.Equal (string.Empty, ckb.Text);
        Assert.Equal ($"{CM.Glyphs.UnChecked} ", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 2, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", Checked = true };
        Assert.True (ckb.Width is Dim.DimAuto);
        Assert.Equal (Dim.Sized (1), ckb.Height);
        Assert.True (ckb.Checked);
        Assert.False (ckb.AllowNullChecked);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{CM.Glyphs.Checked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 6, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", X = 1, Y = 2 };
        Assert.True (ckb.Width is Dim.DimAuto);
        Assert.Equal (Dim.Sized (1), ckb.Height);
        Assert.False (ckb.Checked);
        Assert.False (ckb.AllowNullChecked);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{CM.Glyphs.UnChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (1, 2, 6, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", X = 3, Y = 4, Checked = true };
        Assert.True (ckb.Width is Dim.DimAuto);
        Assert.Equal (Dim.Sized (1), ckb.Height);
        Assert.True (ckb.Checked);
        Assert.False (ckb.AllowNullChecked);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{CM.Glyphs.Checked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (3, 4, 6, 1), ckb.Frame);
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        var toggled = false;
        var ckb = new CheckBox ();
        ckb.Toggled += (s, e) => toggled = true;

        Assert.False (ckb.Checked);
        Assert.False (toggled);
        Assert.Equal (Key.Empty, ckb.HotKey);

        ckb.Text = "_Test";
        Assert.Equal (Key.T, ckb.HotKey);
        Assert.True (ckb.NewKeyDownEvent (Key.T));
        Assert.True (ckb.Checked);
        Assert.True (toggled);

        ckb.Text = "T_est";
        toggled = false;
        Assert.Equal (Key.E, ckb.HotKey);
        Assert.True (ckb.NewKeyDownEvent (Key.E.WithAlt));
        Assert.True (toggled);
        Assert.False (ckb.Checked);

        toggled = false;
        Assert.Equal (Key.E, ckb.HotKey);
        Assert.True (ckb.NewKeyDownEvent (Key.E));
        Assert.True (toggled);
        Assert.True (ckb.Checked);

        toggled = false;
        Assert.True (ckb.NewKeyDownEvent (Key.Space));
        Assert.True (toggled);
        Assert.False (ckb.Checked);

        toggled = false;
        Assert.True (ckb.NewKeyDownEvent (Key.Space));
        Assert.True (toggled);
        Assert.True (ckb.Checked);

        toggled = false;
        Assert.False (ckb.NewKeyDownEvent (Key.Enter));
        Assert.False (toggled);
        Assert.True (ckb.Checked);
    }

    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var ckb = new CheckBox ();
        var acceptInvoked = false;

        ckb.Accept += ViewOnAccept;

        var ret = ckb.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        return;
        void ViewOnAccept (object sender, CancelEventArgs e)
        {
            acceptInvoked = true;
            e.Cancel = true;
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void TextJustification_Centered ()
    {
        var checkBox = new CheckBox
        {
            X = 1,
            Y = Pos.Center (),
            Text = "Check this out 你",
            Justification = Justification.Centered,
            Width = 25
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill (), Title = "Test Demo 你" };
        win.Add (checkBox);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.Equal (Justification.Centered, checkBox.Justification);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.Size);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│    {CM.Glyphs.UnChecked} Check this out 你     │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.Checked = true;
        Application.Refresh ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│    {CM.Glyphs.Checked} Check this out 你     │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 30, 5), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void TextJustification_Justified ()
    {
        var checkBox1 = new CheckBox
        {
            X = 1,
            Y = Pos.Center (),
            Text = "Check first out 你",
            Justification = Justification.Justified,
            Width = 25
        };

        var checkBox2 = new CheckBox
        {
            X = 1,
            Y = Pos.Bottom (checkBox1),
            Text = "Check second out 你",
            Justification = Justification.Justified,
            Width = 25
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill (), Title = "Test Demo 你" };
        win.Add (checkBox1, checkBox2);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 6);

        Assert.Equal (Justification.Justified, checkBox1.Justification);
        Assert.Equal (new (1, 1, 25, 1), checkBox1.Frame);
        Assert.Equal (Justification.Justified, checkBox2.Justification);
        Assert.Equal (new (1, 2, 25, 1), checkBox2.Frame);
 
        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.UnChecked}   Check  first  out  你  │
│ {CM.Glyphs.UnChecked}  Check  second  out  你  │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 30, 6), pos);

        checkBox1.Checked = true;
        Assert.Equal (new (1, 1, 25, 1), checkBox1.Frame);
        Assert.Equal (_size25x1, checkBox1.TextFormatter.Size);
        checkBox2.Checked = true;
        Assert.Equal (new (1, 2, 25, 1), checkBox2.Frame);
        Assert.Equal (_size25x1, checkBox2.TextFormatter.Size);
        Application.Refresh ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.Checked}   Check  first  out  你  │
│ {CM.Glyphs.Checked}  Check  second  out  你  │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 30, 6), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void TextJustification_Left ()
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
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.Equal (Justification.Left, checkBox.Justification);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.Size);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.UnChecked} Check this out 你        │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.Checked = true;
        Application.Refresh ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.Checked} Check this out 你        │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 30, 5), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void TextJustification_Right ()
    {
        var checkBox = new CheckBox
        {
            X = 1,
            Y = Pos.Center (),
            Text = "Check this out 你",
            Justification = Justification.Right,
            Width = 25
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill (), Title = "Test Demo 你" };
        win.Add (checkBox);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        Assert.Equal (Justification.Right, checkBox.Justification);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.Size);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│       Check this out 你 {CM.Glyphs.UnChecked}  │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.Checked = true;
        Application.Refresh ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│       Check this out 你 {CM.Glyphs.Checked}  │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 30, 5), pos);
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
        void CheckBoxOnAccept (object sender, CancelEventArgs e) { accepted = true; }
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    [InlineData (null)]
    public void Toggled_Cancel_Event_Prevents_Toggle (bool? initialState)
    {
        var ckb = new CheckBox () { AllowNullChecked = true };
        var checkedInvoked = false;

        ckb.Toggled += CheckBoxToggled;

        ckb.Checked = initialState;
        Assert.Equal(initialState, ckb.Checked);
        var ret = ckb.OnToggled ();
        Assert.True (ret);
        Assert.True (checkedInvoked);
        Assert.Equal (initialState, ckb.Checked);

        return;
        void CheckBoxToggled (object sender, CancelEventArgs e)
        {
            checkedInvoked = true;
            e.Cancel = true;
        }
    }
}
