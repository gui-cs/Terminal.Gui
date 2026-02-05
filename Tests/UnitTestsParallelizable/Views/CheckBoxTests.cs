namespace ViewsTests;

public class CheckBoxTests
{
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

        void ViewOnAccept (object? sender, CommandEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void AllowCheckStateNone_Get_Set ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var checkBox = new CheckBox { Text = "Check this out 你" };
        (runnable as View)?.Add (checkBox);
        app.Begin (runnable);

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Select with keyboard
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.Equal (CheckState.Checked, checkBox.Value);

        // Select with mouse
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (Point.Empty));
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        checkBox.AllowCheckStateNone = true;
        Assert.True (checkBox.NewKeyDownEvent (Key.Space));
        Assert.Equal (CheckState.None, checkBox.Value);

        checkBox.AllowCheckStateNone = false;
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
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
        var checkBox = new CheckBox { X = 0, Y = 0, Width = width, Height = height };

        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.TextFormatter.ConstrainToSize);

        checkBox.Dispose ();
    }

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

        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.TextFormatter.ConstrainToSize);

        checkBox.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void CheckBox_Command_Accept_ConfirmsStateWithoutToggle ()
    {
        CheckBox checkBox = new () { Text = "Test", Value = CheckState.Checked };
        CheckState initialState = checkBox.Value;
        var acceptingFired = false;

        checkBox.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true; // Signal that the Accept was processed
                              };

        bool? result = checkBox.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.Equal (initialState, checkBox.Value); // State unchanged
        Assert.True (result);

        checkBox.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void CheckBox_Command_Activate_TogglesState ()
    {
        CheckBox checkBox = new () { Text = "Test" };
        CheckState initialState = checkBox.Value;
        var activatingFired = false;

        checkBox.Activating += (_, _) => activatingFired = true;

        bool? result = checkBox.InvokeCommand (Command.Activate);

        Assert.True (activatingFired);
        Assert.NotEqual (initialState, checkBox.Value);
        Assert.True (result);

        checkBox.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void CheckBox_Command_HotKey_InvokesActivate ()
    {
        CheckBox checkBox = new () { Text = "_Test" };
        CheckState initialState = checkBox.Value;
        var activatingFired = false;

        checkBox.Activating += (_, _) => activatingFired = true;

        bool? result = checkBox.InvokeCommand (Command.HotKey);

        // HotKey invokes Activate (toggles state + SetFocus)
        Assert.True (activatingFired);
        Assert.NotEqual (initialState, checkBox.Value);
        Assert.True (result);

        checkBox.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void CheckBox_Enter_ConfirmsWithoutToggle ()
    {
        CheckBox checkBox = new () { Text = "Test", Value = CheckState.Checked };
        CheckState initialState = checkBox.Value;
        var acceptingFired = false;

        checkBox.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true;
                              };

        // Enter should confirm without toggling via Accept command
        bool? result = checkBox.NewKeyDownEvent (Key.Enter);

        Assert.True (acceptingFired);
        Assert.Equal (initialState, checkBox.Value);
        Assert.True (result);

        checkBox.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void CheckBox_Space_TogglesState ()
    {
        CheckBox checkBox = new () { Text = "Test" };
        CheckState initialState = checkBox.Value;

        // Space should trigger state toggle via Activate command
        bool? result = checkBox.NewKeyDownEvent (Key.Space);

        Assert.NotEqual (initialState, checkBox.Value);
        Assert.True (result);

        checkBox.Dispose ();
    }

    [Fact]
    public void Commands_Select ()
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        View otherView = new () { CanFocus = true };
        var ckb = new CheckBox ();
        runnable.Add (ckb, otherView);
        app.Begin (runnable);
        ckb.SetFocus ();
        Assert.True (ckb.HasFocus);

        var checkedStateChangingCount = 0;
        ckb.ValueChanging += (s, e) => checkedStateChangingCount++;

        var selectCount = 0;
        ckb.Activating += (s, e) => selectCount++;

        var acceptCount = 0;
        ckb.Accepting += (s, e) => acceptCount++;

        Assert.Equal (CheckState.UnChecked, ckb.Value);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);
        Assert.Equal (Key.Empty, ckb.HotKey);

        // Test while focused
        ckb.Text = "_Test";
        Assert.Equal (Key.T, ckb.HotKey);
        ckb.NewKeyDownEvent (Key.T);
        Assert.Equal (CheckState.Checked, ckb.Value);
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
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var ckb = new CheckBox ();
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.UnChecked, ckb.Value);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal (string.Empty, ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} ", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 2, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", Value = CheckState.Checked };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.Checked, ckb.Value);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 6, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", X = 1, Y = 2 };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.UnChecked, ckb.Value);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (1, 2, 6, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", X = 3, Y = 4, Value = CheckState.Checked };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.Checked, ckb.Value);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (3, 4, 6, 1), ckb.Frame);
    }

    [Fact]
    public void LeftButtonReleased_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var checkBox = new CheckBox { Text = "Check this out 你" };
        (runnable as View)?.Add (checkBox);
        app.Begin (runnable);

        var checkedStateChangingCount = 0;
        checkBox.ValueChanging += (s, e) => checkedStateChangingCount++;

        var activatingCount = 0;
        checkBox.Activating += (s, e) => activatingCount++;

        var acceptingCount = 0;
        checkBox.Accepting += (s, e) => acceptingCount++;

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = Point.Empty });
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = Point.Empty });
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (1, checkedStateChangingCount);
        Assert.Equal (1, activatingCount);
        Assert.Equal (0, acceptingCount);

        checkBox.AllowCheckStateNone = true;

        // Delay 500ms to prevent double click detection
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = Point.Empty, Timestamp = time.Now.AddMilliseconds (500) });
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (1, checkedStateChangingCount);
        Assert.Equal (1, activatingCount);
        Assert.Equal (0, acceptingCount);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = Point.Empty });
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (2, checkedStateChangingCount);
        Assert.Equal (2, activatingCount);
        Assert.Equal (0, acceptingCount);

        // Delay 500ms to prevent double click detection
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = Point.Empty, Timestamp = time.Now.AddMilliseconds (500) });
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (2, checkedStateChangingCount);
        Assert.Equal (2, activatingCount);
        Assert.Equal (0, acceptingCount);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = Point.Empty });
        Assert.Equal (CheckState.None, checkBox.Value);
        Assert.Equal (3, checkedStateChangingCount);
        Assert.Equal (3, activatingCount);
        Assert.Equal (0, acceptingCount);
    }

    [Fact]
    public void Mouse_DoubleClick_Accepts ()
    {
        var checkBox = new CheckBox { Text = "_Checkbox" };
        Assert.True (checkBox.CanFocus);

        var checkedStateChangingCount = 0;
        checkBox.ValueChanging += (s, e) => checkedStateChangingCount++;

        var selectCount = 0;
        checkBox.Activating += (s, e) => selectCount++;

        var acceptCount = 0;

        checkBox.Accepting += (s, e) =>
                              {
                                  acceptCount++;
                                  e.Handled = true;
                              };

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);

        checkBox.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonDoubleClicked });

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (1, acceptCount);
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
}
