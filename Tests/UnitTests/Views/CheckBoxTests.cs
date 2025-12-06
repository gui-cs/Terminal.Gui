using System.ComponentModel;
using UnitTests;
using Xunit.Abstractions;

// ReSharper disable AccessToModifiedClosure

namespace UnitTests.ViewsTests;

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
        checkBox.Layout ();

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
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} Hello", view.TextFormatter.Text);
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new CheckBox ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} Hello", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var ckb = new CheckBox ();
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.UnChecked, ckb.CheckedState);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal (string.Empty, ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} ", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new (0, 0, 2, 1), ckb.Frame);

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
        var top = new Runnable ();
        top.Add (win);

        Application.Begin (top);
        Application.Driver?.SetScreenSize (30, 5);
        Application.LayoutAndDraw ();

        Assert.Equal (Alignment.Center, checkBox.TextAlignment);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.ConstrainToSize);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│    {Glyphs.CheckStateUnChecked} Check this out 你     │
│                            │
└────────────────────────────┘
";

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.CheckedState = CheckState.Checked;
        Application.LayoutAndDraw ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│    {Glyphs.CheckStateChecked} Check this out 你     │
│                            │
└────────────────────────────┘
";

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        var top = new Runnable ();
        top.Add (win);

        SessionToken rs = Application.Begin (top);
        Application.Driver!.SetScreenSize (30, 6);
        Application.LayoutAndDraw ();
        Assert.Equal (Alignment.Fill, checkBox1.TextAlignment);
        Assert.Equal (new (1, 1, 25, 1), checkBox1.Frame);
        Assert.Equal (Alignment.Fill, checkBox2.TextAlignment);
        Assert.Equal (new (1, 2, 25, 1), checkBox2.Frame);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {Glyphs.CheckStateUnChecked}   Check  first  out  你  │
│ {Glyphs.CheckStateUnChecked}  Check  second  out  你  │
│                            │
└────────────────────────────┘
";

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 6), pos);

        checkBox1.CheckedState = CheckState.Checked;
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (new (1, 1, 25, 1), checkBox1.Frame);
        Assert.Equal (_size25x1, checkBox1.TextFormatter.ConstrainToSize);

        checkBox2.CheckedState = CheckState.Checked;
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (new (1, 2, 25, 1), checkBox2.Frame);
        Assert.Equal (_size25x1, checkBox2.TextFormatter.ConstrainToSize);
        AutoInitShutdownAttribute.RunIteration ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {Glyphs.CheckStateChecked}   Check  first  out  你  │
│ {Glyphs.CheckStateChecked}  Check  second  out  你  │
│                            │
└────────────────────────────┘
";

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        var top = new Runnable ();
        top.Add (win);

        Application.Begin (top);
        Application.Driver!.SetScreenSize (30, 5);
        Application.LayoutAndDraw ();
        Assert.Equal (Alignment.Start, checkBox.TextAlignment);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.ConstrainToSize);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {Glyphs.CheckStateUnChecked} Check this out 你        │
│                            │
└────────────────────────────┘
";

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.CheckedState = CheckState.Checked;
        AutoInitShutdownAttribute.RunIteration ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {Glyphs.CheckStateChecked} Check this out 你        │
│                            │
└────────────────────────────┘
";

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        var top = new Runnable ();
        top.Add (win);

        Application.Begin (top);
        Application.Driver!.SetScreenSize (30, 5);
        Application.LayoutAndDraw ();
        Assert.Equal (Alignment.End, checkBox.TextAlignment);
        Assert.Equal (new (1, 1, 25, 1), checkBox.Frame);
        Assert.Equal (_size25x1, checkBox.TextFormatter.ConstrainToSize);

        var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│       Check this out 你 {Glyphs.CheckStateUnChecked}  │
│                            │
└────────────────────────────┘
";

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        checkBox.CheckedState = CheckState.Checked;
        AutoInitShutdownAttribute.RunIteration ();

        expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│       Check this out 你 {Glyphs.CheckStateChecked}  │
│                            │
└────────────────────────────┘
";

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);
        top.Dispose ();
    }


    [Theory]
    [InlineData (CheckState.Checked)]
    [InlineData (CheckState.UnChecked)]
    [InlineData (CheckState.None)]
    public void Activated_Handle_Event_Prevents_Change (CheckState initialState)
    {
        var ckb = new CheckBox { AllowCheckStateNone = true };
        var checkedInvoked = false;

        ckb.CheckedState = initialState;

        ckb.Activating += OnActivating;

        Assert.Equal (initialState, ckb.CheckedState);
        bool? ret = ckb.InvokeCommand (Command.Activate);
        Assert.True (ret);
        Assert.True (checkedInvoked);
        Assert.Equal (initialState, ckb.CheckedState);

        return;

        void OnActivating (object sender, CommandEventArgs e)
        {
            checkedInvoked = true;
            e.Handled = true;
        }
    }

    [Theory]
    [InlineData (CheckState.Checked)]
    [InlineData (CheckState.UnChecked)]
    [InlineData (CheckState.None)]
    public void CheckedStateChanging_Cancel_Event_Prevents_Change (CheckState initialState)
    {
        var ckb = new CheckBox { AllowCheckStateNone = true };
        var checkedInvoked = false;

        ckb.CheckedState = initialState;

        ckb.CheckedStateChanging += OnCheckedStateChanging;

        Assert.Equal (initialState, ckb.CheckedState);

        // AdvanceCheckState returns false if the state was changed, true if it was cancelled, null if it was not changed
        bool? ret = ckb.AdvanceCheckState ();
        Assert.True (ret);
        Assert.True (checkedInvoked);
        Assert.Equal (initialState, ckb.CheckedState);

        return;

        void OnCheckedStateChanging (object sender, ResultEventArgs<CheckState> e)
        {
            checkedInvoked = true;
            e.Handled = true;
        }
    }
}
