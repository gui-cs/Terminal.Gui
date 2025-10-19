using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class CheckBoxTests (ITestOutputHelper output)
{
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

        ckb = new () { Text = "Test", CheckedState = CheckState.Checked };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.Checked, ckb.CheckedState);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new (0, 0, 6, 1), ckb.Frame);

        ckb = new () { Text = "Test", X = 1, Y = 2 };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.UnChecked, ckb.CheckedState);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new (1, 2, 6, 1), ckb.Frame);

        ckb = new () { Text = "Test", X = 3, Y = 4, CheckedState = CheckState.Checked };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.Checked, ckb.CheckedState);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new (3, 4, 6, 1), ckb.Frame);
    }

    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var ckb = new CheckBox ();
        var acceptInvoked = false;

        ckb.Accepting += ViewOnAccept;

        bool? ret = ckb.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        return;

        void ViewOnAccept (object sender, CommandEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void AllowCheckStateNone_Get_Set ()
    {
        var checkBox = new CheckBox { Text = "Check this out 你" };

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);

        // Select with keyboard
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        // Select with mouse
        Assert.True (checkBox.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);

        checkBox.AllowCheckStateNone = true;
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.Equal (CheckState.None, checkBox.CheckedState);

        checkBox.AllowCheckStateNone = false;
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
    }

    [Fact]
    public void Mouse_Click_Selects ()
    {
        var checkBox = new CheckBox { Text = "_Checkbox" };
        Assert.True (checkBox.CanFocus);

        var checkedStateChangingCount = 0;
        checkBox.CheckedStateChanging += (s, e) => checkedStateChangingCount++;

        var selectCount = 0;
        checkBox.Selecting += (s, e) => selectCount++;

        var acceptCount = 0;
        checkBox.Accepting += (s, e) => acceptCount++;

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);

        Assert.True (checkBox.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
        Assert.Equal (1, checkedStateChangingCount);
        Assert.Equal (1, selectCount);
        Assert.Equal (0, acceptCount);

        Assert.True (checkBox.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        Assert.Equal (2, checkedStateChangingCount);
        Assert.Equal (2, selectCount);
        Assert.Equal (0, acceptCount);

        checkBox.AllowCheckStateNone = true;
        Assert.True (checkBox.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (CheckState.None, checkBox.CheckedState);
        Assert.Equal (3, checkedStateChangingCount);
        Assert.Equal (3, selectCount);
        Assert.Equal (0, acceptCount);
    }

    [Fact]
    public void Mouse_DoubleClick_Accepts ()
    {
        var checkBox = new CheckBox { Text = "_Checkbox" };
        Assert.True (checkBox.CanFocus);

        var checkedStateChangingCount = 0;
        checkBox.CheckedStateChanging += (s, e) => checkedStateChangingCount++;

        var selectCount = 0;
        checkBox.Selecting += (s, e) => selectCount++;

        var acceptCount = 0;

        checkBox.Accepting += (s, e) =>
                              {
                                  acceptCount++;
                                  e.Handled = true;
                              };

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);

        Assert.True (checkBox.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1DoubleClicked }));

        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (1, acceptCount);
    }
}
