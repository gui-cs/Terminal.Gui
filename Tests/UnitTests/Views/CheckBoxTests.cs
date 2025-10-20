using System.ComponentModel;
using UnitTests;
using Xunit.Abstractions;

// ReSharper disable AccessToModifiedClosure

namespace Terminal.Gui.ViewsTests;

public class CheckBoxTests (ITestOutputHelper output)
{
    private static readonly Size _size25x1 = new (25, 1);



    [Fact]
    public void Commands_Select ()
    {
        Application.Navigation = new ();
        Application.Top = new ();
        View otherView = new () { CanFocus = true };
        var ckb = new CheckBox ();
        Application.Top.Add (ckb, otherView);
        Application.Top.SetFocus ();
        Assert.True (ckb.HasFocus);

        var checkedStateChangingCount = 0;
        ckb.CheckedStateChanging += (s, e) => checkedStateChangingCount++;

        var selectCount = 0;
        ckb.Selecting += (s, e) => selectCount++;

        var acceptCount = 0;
        ckb.Accepting += (s, e) => acceptCount++;

        Assert.Equal (CheckState.UnChecked, ckb.CheckedState);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);
        Assert.Equal (Key.Empty, ckb.HotKey);

        // Test while focused
        ckb.Text = "_Test";
        Assert.Equal (Key.T, ckb.HotKey);
        ckb.NewKeyDownEvent (Key.T);
        Assert.Equal (CheckState.Checked, ckb.CheckedState);
        Assert.Equal (1, checkedStateChangingCount);
        Assert.Equal (1, selectCount);
        Assert.Equal (0, acceptCount);

        ckb.Text = "T_est";
        Assert.Equal (Key.E, ckb.HotKey);
        ckb.NewKeyDownEvent (Key.E.WithAlt);
        Assert.Equal (2, checkedStateChangingCount);
        Assert.Equal (2, selectCount);
        Assert.Equal (0, acceptCount);

        ckb.NewKeyDownEvent (Key.Space);
        Assert.Equal (3, checkedStateChangingCount);
        Assert.Equal (3, selectCount);
        Assert.Equal (0, acceptCount);

        ckb.NewKeyDownEvent (Key.Enter);
        Assert.Equal (3, checkedStateChangingCount);
        Assert.Equal (3, selectCount);
        Assert.Equal (1, acceptCount);

        Application.Top.Dispose ();
        Application.ResetState ();
    }



    #region Mouse Tests





    #endregion Mouse Tests

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
        AutoInitShutdownAttribute.FakeResize (new Size (30, 5));

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
        var top = new Toplevel ();
        top.Add (win);

        RunState rs = Application.Begin (top);
        AutoInitShutdownAttribute.FakeResize(new Size(30, 6));

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
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        AutoInitShutdownAttribute.FakeResize(new Size(30, 5));

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
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        AutoInitShutdownAttribute.FakeResize(new Size(30, 5));

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

    [Fact]
    public void HotKey_Command_Does_Not_Fire_Accept ()
    {
        var cb = new CheckBox ();
        var accepted = false;

        cb.Accepting += CheckBoxOnAccept;
        cb.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void CheckBoxOnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Theory]
    [InlineData (CheckState.Checked)]
    [InlineData (CheckState.UnChecked)]
    [InlineData (CheckState.None)]
    public void Selected_Handle_Event_Does_Not_Prevent_Change (CheckState initialState)
    {
        var ckb = new CheckBox { AllowCheckStateNone = true };
        var checkedInvoked = false;

        ckb.CheckedState = initialState;

        ckb.Selecting += OnSelecting;

        Assert.Equal (initialState, ckb.CheckedState);
        bool? ret = ckb.InvokeCommand (Command.Select);
        Assert.True (ret);
        Assert.True (checkedInvoked);
        Assert.NotEqual (initialState, ckb.CheckedState);

        return;

        void OnSelecting (object sender, CommandEventArgs e)
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
