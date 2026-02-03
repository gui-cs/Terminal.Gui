using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public class ShortcutTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var shortcut = new Shortcut ();

        Assert.NotNull (shortcut);
        Assert.True (shortcut.CanFocus);
        Assert.IsType<DimAuto> (shortcut.Width);
        Assert.IsType<DimAuto> (shortcut.Height);
    }

    [Fact]
    public void Size_Defaults ()
    {
        var shortcut = new Shortcut ();
        shortcut.Layout ();

        Assert.Equal (2, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (2, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (0, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (0, shortcut.HelpView.Viewport.Width);
        Assert.Equal (0, shortcut.HelpView.Viewport.Height);

        Assert.Equal (0, shortcut.KeyView.Viewport.Width);
        Assert.Equal (0, shortcut.KeyView.Viewport.Height);

        //  0123456789
        // "   0  A "
        shortcut = new () { Key = Key.A, HelpText = "0" };
        shortcut.Layout ();
        Assert.Equal (8, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (8, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (0, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (1, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (1, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);

        //  0123456789
        // " C  0  A "
        shortcut = new () { Title = "C", Key = Key.A, HelpText = "0" };
        shortcut.Layout ();
        Assert.Equal (9, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (9, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (1, shortcut.CommandView.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView.Viewport.Height);

        Assert.Equal (1, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (1, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);
    }

    [Theory]
    [InlineData ("", "", KeyCode.Null, 2)]
    [InlineData ("C", "", KeyCode.Null, 3)]
    [InlineData ("", "H", KeyCode.Null, 5)]
    [InlineData ("", "", KeyCode.K, 5)]
    [InlineData ("C", "", KeyCode.K, 6)]
    [InlineData ("C", "H", KeyCode.Null, 6)]
    [InlineData ("", "H", KeyCode.K, 8)]
    [InlineData ("C", "H", KeyCode.K, 9)]
    public void NaturalSize (string command, string help, KeyCode key, int expectedWidth)
    {
        var shortcut = new Shortcut { HelpText = help, Key = key, Title = command };

        shortcut.Layout ();

        // |0123456789
        // | C  H  K |
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new () { HelpText = help, Title = command, Key = key };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new () { HelpText = help, Key = key, Title = command };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new () { Key = key, HelpText = help, Title = command };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);
    }

    [Theory]
    [InlineData (0, 0, 3, 3)]
    [InlineData (1, 0, 3, 3)]
    [InlineData (2, 0, 3, 3)]
    [InlineData (3, 0, 3, 3)]
    [InlineData (4, 0, 3, 3)]
    [InlineData (5, 0, 3, 3)]
    [InlineData (6, 0, 3, 3)]
    [InlineData (7, 0, 3, 4)]
    [InlineData (8, 0, 3, 5)]
    [InlineData (9, 0, 3, 6)]
    [InlineData (10, 0, 4, 7)]
    [InlineData (11, 0, 5, 8)]
    public void Set_Width_Layouts_Correctly (int width, int expectedCmdX, int expectedHelpX, int expectedKeyX)
    {
        var shortcut = new Shortcut { Width = width, Title = "C", Text = "H", Key = Key.K };
        shortcut.Layout ();

        // 01234
        // -C--K 

        // 012345
        // -C--K- 

        // 0123456
        // -C-H-K- 

        // 01234567
        // -C--H-K- 

        // 012345678
        // -C--H--K- 

        // 0123456789
        // -C--H--K- 
        Assert.Equal (expectedCmdX, shortcut.CommandView.Frame.X);
        Assert.Equal (expectedHelpX, shortcut.HelpView.Frame.X);
        Assert.Equal (expectedKeyX, shortcut.KeyView.Frame.X);
    }

    [Fact]
    public void CommandView_Text_And_Title_Track ()
    {
        var shortcut = new Shortcut { Title = "T" };

        Assert.Equal (shortcut.Title, shortcut.CommandView.Text);

        shortcut = new ();

        shortcut.CommandView = new () { Text = "T" };
        Assert.Equal (shortcut.Title, shortcut.CommandView.Text);
    }

    [Fact]
    public void HelpText_And_Text_Are_The_Same ()
    {
        var shortcut = new Shortcut { Text = "H" };

        Assert.Equal (shortcut.Text, shortcut.HelpText);

        shortcut = new () { HelpText = "H" };

        Assert.Equal (shortcut.Text, shortcut.HelpText);
    }

    [Theory]
    [InlineData (KeyCode.Null, "")]
    [InlineData (KeyCode.F1, "F1")]
    public void KeyView_Text_Tracks_Key (KeyCode key, string expected)
    {
        var shortcut = new Shortcut { Key = key };

        Assert.Equal (expected, shortcut.KeyView.Text);
    }

    // Test Key
    [Fact]
    public void Key_Defaults_To_Empty ()
    {
        var shortcut = new Shortcut ();

        Assert.Equal (Key.Empty, shortcut.Key);
    }

    [Fact]
    public void Key_Can_Be_Set ()
    {
        var shortcut = new Shortcut ();

        shortcut.Key = Key.F1;

        Assert.Equal (Key.F1, shortcut.Key);
    }

    [Fact]
    public void Key_Can_Be_Set_To_Empty ()
    {
        var shortcut = new Shortcut ();

        shortcut.Key = Key.Empty;

        Assert.Equal (Key.Empty, shortcut.Key);
    }

    [Fact]
    public void Key_Set_Binds_Key_To_CommandView_Accept ()
    {
        var shortcut = new Shortcut ();

        shortcut.Key = Key.F1;

        // TODO:
    }

    [Fact]
    public void Key_Changing_Removes_Previous_Binding ()
    {
        var shortcut = new Shortcut ();

        shortcut.Key = Key.A;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));

        shortcut.Key = Key.B;
        Assert.False (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.B, out _));
    }

    // Test Key gets bound correctly
    [Fact]
    public void BindKeyToApplication_Defaults_To_HotKey ()
    {
        var shortcut = new Shortcut ();

        Assert.False (shortcut.BindKeyToApplication);
    }

    [Fact]
    public void BindKeyToApplication_Can_Be_Set ()
    {
        IApplication? app = Application.Create ();
        var shortcut = new Shortcut { App = app };

        shortcut.BindKeyToApplication = true;

        Assert.True (shortcut.BindKeyToApplication);
    }

    [Fact]
    public void BindKeyToApplication_Changing_Adjusts_KeyBindings ()
    {
        var shortcut = new Shortcut ();

        shortcut.Key = Key.A;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));

        shortcut.App = Application.Create ();
        shortcut.BindKeyToApplication = true;
        shortcut.BeginInit ();
        shortcut.EndInit ();
        Assert.False (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.True (shortcut.App?.Keyboard.KeyBindings.TryGet (Key.A, out _));

        shortcut.BindKeyToApplication = false;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.False (shortcut.App?.Keyboard.KeyBindings.TryGet (Key.A, out _));
    }

    [Theory]
    [InlineData (Orientation.Horizontal)]
    [InlineData (Orientation.Vertical)]
    public void Orientation_SetsCorrectly (Orientation orientation)
    {
        var shortcut = new Shortcut { Orientation = orientation };

        Assert.Equal (orientation, shortcut.Orientation);
    }

    [Theory]
    [InlineData (AlignmentModes.StartToEnd)]
    [InlineData (AlignmentModes.EndToStart)]
    public void AlignmentModes_SetsCorrectly (AlignmentModes alignmentModes)
    {
        var shortcut = new Shortcut { AlignmentModes = alignmentModes };

        Assert.Equal (alignmentModes, shortcut.AlignmentModes);
    }

    [Fact]
    public void Action_SetsAndGetsCorrectly ()
    {
        var actionInvoked = false;

        var shortcut = new Shortcut { Action = () => { actionInvoked = true; } };

        shortcut.Action.Invoke ();

        Assert.True (actionInvoked);
    }

    [Fact]
    public void SubView_Visibility_Controlled_By_Removal ()
    {
        var shortcut = new Shortcut ();

        Assert.True (shortcut.CommandView.Visible);
        Assert.Contains (shortcut.CommandView, shortcut.SubViews);
        Assert.True (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);

        shortcut.HelpText = "help";
        Assert.True (shortcut.HelpView.Visible);
        Assert.Contains (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);

        shortcut.Key = Key.A;
        Assert.True (shortcut.HelpView.Visible);
        Assert.Contains (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.Contains (shortcut.KeyView, shortcut.SubViews);

        shortcut.HelpView.Visible = false;
        shortcut.ShowHide ();
        Assert.False (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.Contains (shortcut.KeyView, shortcut.SubViews);

        shortcut.KeyView.Visible = false;
        shortcut.ShowHide ();
        Assert.False (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.False (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);
    }

    [Fact]
    public void Focus_CanFocus_Default_Is_True ()
    {
        Shortcut shortcut = new ();
        shortcut.Key = Key.A;
        shortcut.Text = "Help";
        shortcut.Title = "Command";
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);
    }

    [Fact]
    public void Focus_CanFocus_CommandView_Add_Tracks ()
    {
        Shortcut shortcut = new ();
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);

        shortcut.CommandView = new () { CanFocus = true };
        Assert.True (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = true;
        Assert.True (shortcut.CommandView.CanFocus);

        shortcut.CanFocus = false;
        Assert.False (shortcut.CanFocus);
        Assert.True (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = false;
        Assert.False (shortcut.CanFocus);
        Assert.False (shortcut.CommandView.CanFocus);

        shortcut.CommandView.CanFocus = true;
        Assert.False (shortcut.CanFocus);
        Assert.True (shortcut.CommandView.CanFocus);
    }

    [Theory (Skip = "Broke somehow!")]
    [InlineData (true, KeyCode.A, 1, 1)]
    [InlineData (true, KeyCode.C, 1, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1, 1)]
    [InlineData (true, KeyCode.Enter, 1, 1)]
    [InlineData (true, KeyCode.Space, 1, 1)]
    [InlineData (true, KeyCode.F1, 0, 0)]
    [InlineData (false, KeyCode.A, 1, 1)]
    [InlineData (false, KeyCode.C, 1, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1, 1)]
    [InlineData (false, KeyCode.Enter, 0, 0)]
    [InlineData (false, KeyCode.Space, 0, 0)]
    [InlineData (false, KeyCode.F1, 0, 0)]
    public void KeyDown_CheckBox_Raises_Accepted_Selected (bool canFocus, KeyCode key, int expectedAccept, int expectedSelect)
    {
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        var shortcut = new Shortcut { Key = Key.A, Text = "0", CommandView = new CheckBox { Title = "_C" }, CanFocus = canFocus };
        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepted = 0;

        shortcut.Accepting += (s, e) =>
                              {
                                  accepted++;
                                  e.Handled = true;
                              };

        var selected = 0;
        shortcut.Activating += (s, e) => selected++;

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAccept, accepted);
        Assert.Equal (expectedSelect, selected);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a CheckBox with CanFocus=false CAN change state when
    ///     Command.Activate is invoked directly on it.
    ///     CanFocus only controls keyboard focus, not the ability to change state.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_False_Changes_State_On_Direct_Activate ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = false
        };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.False (checkBox.CanFocus);

        // Act - Directly invoke Command.Activate on the CheckBox
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=false SHOULD change state
        // CanFocus only controls keyboard focus, not state changes
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a CheckBox with CanFocus=true DOES change state when
    ///     Command.Activate is invoked directly on it.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_True_Changes_State_On_Direct_Activate ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = true
        };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.True (checkBox.CanFocus);

        // Act - Directly invoke Command.Activate on the CheckBox
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=true SHOULD change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a CheckBox used as a CommandView with CanFocus=false
    ///     CAN change its state when the Shortcut is activated.
    ///     CanFocus only controls keyboard focus, not the ability to change state.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_False_CommandView_Changes_State_On_Shortcut_Activate ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = false
        };

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox
        };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.False (checkBox.CanFocus);

        // Act - Invoke Command.Activate on the Shortcut (simulating user activation)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=false SHOULD change state
        // CanFocus only controls keyboard focus, not state changes
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a CheckBox used as a CommandView with CanFocus=true
    ///     DOES change its state when the Shortcut is activated.
    ///     Focusable CheckBoxes should respond to activation commands.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_True_CommandView_Changes_State_On_Shortcut_Activate ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = true
        };

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox
        };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.True (checkBox.CanFocus);

        // Act - Invoke Command.Activate on the Shortcut (simulating user activation)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=true SHOULD change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a mouse press on a CheckBox CommandView changes its state.
    ///     This tests that the mouse binding is correctly set up on the CheckBox.
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_MousePress_Changes_State ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = false
        };

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox
        };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Verify CheckBox has the expected mouse binding for LeftButtonPressed
        Assert.True (checkBox.MouseBindings.TryGet (MouseFlags.LeftButtonPressed, out MouseBinding binding));
        Assert.Contains (Command.Activate, binding.Commands);

        // Act - Simulate a mouse press by invoking the bound command directly
        // This is what NewMouseEvent would do internally
        checkBox.NewMouseEvent (new ()
        {
            Position = new (0, 0),
            Flags = MouseFlags.LeftButtonPressed
        });

        // Assert - CheckBox should change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that invoking Command.Activate directly on the CheckBox CommandView
    ///     (simulating what a mouse press should do) changes its state.
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_Direct_Activate_Changes_State ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = false
        };

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox
        };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Directly invoke Command.Activate on the CheckBox (what mouse press should trigger)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox should change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that MouseHighlightStates defaults to MouseState.In for Shortcut.
    ///     This ensures Shortcuts highlight when the mouse hovers over them.
    /// </summary>
    [Fact]
    public void MouseHighlightStates_Defaults_To_In ()
    {
        // Arrange & Act
        Shortcut shortcut = new ();

        // Assert
        Assert.Equal (MouseState.In, shortcut.MouseHighlightStates);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when CommandView raises Activating (e.g., from direct click),
    ///     the Shortcut also raises its Activating event.
    /// </summary>
    [Fact]
    public void CommandView_Activating_Forwards_To_Shortcut_Activating ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = false
        };

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox
        };

        var shortcutActivatingRaised = false;
        shortcut.Activating += (_, _) => shortcutActivatingRaised = true;

        // Act - Invoke Command.Activate directly on CheckBox (simulating direct mouse click)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should have been raised
        Assert.True (shortcutActivatingRaised);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when CommandView raises Accepting (e.g., double-click on CheckBox),
    ///     the Shortcut also raises its Accepting event.
    /// </summary>
    [Fact]
    public void CommandView_Accepting_Forwards_To_Shortcut_Accepting ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = true
        };

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox
        };

        var shortcutAcceptingRaised = false;
        shortcut.Accepting += (_, _) => shortcutAcceptingRaised = true;

        // Act - Invoke Command.Accept directly on CheckBox (simulating double-click)
        checkBox.InvokeCommand (Command.Accept);

        // Assert - Shortcut.Accepting should have been raised
        Assert.True (shortcutAcceptingRaised);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that when Shortcut is activated via DispatchCommand (e.g., hotkey),
    ///     the CommandView processes the activation but does NOT cause double-forwarding.
    /// </summary>
    [Fact]
    public void Shortcut_Activate_Does_Not_Double_Forward_To_Shortcut ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = false
        };

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox
        };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        // Act - Invoke Command.Activate on Shortcut (simulating hotkey press)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should be raised exactly once (not twice from forwarding)
        Assert.Equal (1, shortcutActivatingCount);
        // And CheckBox should have changed state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that CommandView with CanFocus=true still allows Shortcut.Activating
    ///     to be raised when the CommandView is clicked directly.
    /// </summary>
    [Fact]
    public void CommandView_CanFocus_True_Click_Raises_Shortcut_Activating ()
    {
        // Arrange
        CheckBox checkBox = new ()
        {
            Title = "_Toggle",
            CanFocus = true
        };

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox
        };

        var shortcutActivatingRaised = false;
        shortcut.Activating += (_, _) => shortcutActivatingRaised = true;

        // Act - Invoke Command.Activate directly on CheckBox (simulating mouse click)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should have been raised
        Assert.True (shortcutActivatingRaised);
        // And CheckBox should have changed state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    // NOTE: Shortcut has unified handling - all three commands invoke DispatchCommand
    [Fact]
    public void Shortcut_AllCommands_InvokeSameDispatchCommand ()
    {
        Shortcut shortcut = new () { Title = "Test", Key = Key.T.WithCtrl };
        var dispatchCount = 0;

        shortcut.Accepting += (_, e) =>
                              {
                                  dispatchCount++;
                                  e.Handled = true;
                              };

        shortcut.InvokeCommand (Command.Activate);
        shortcut.InvokeCommand (Command.Accept);
        shortcut.InvokeCommand (Command.HotKey);

        // All three should invoke the same DispatchCommand method
        Assert.Equal (3, dispatchCount);

        shortcut.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Shortcut_Command_Activate_DispatchesCommand ()
    {
        Shortcut shortcut = new () { Title = "Test", Key = Key.T.WithCtrl };
        var acceptingFired = false;

        shortcut.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true;
                              };

        bool? result = shortcut.InvokeCommand (Command.Activate);

        Assert.True (acceptingFired);
        Assert.True (result);

        shortcut.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Shortcut_Command_Accept_SameAsActivate ()
    {
        Shortcut shortcut = new () { Title = "Test", Key = Key.T.WithCtrl };
        var acceptingFired = false;

        shortcut.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true;
                              };

        bool? result = shortcut.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        shortcut.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Shortcut_Command_HotKey_SameAsActivate ()
    {
        Shortcut shortcut = new () { Title = "Test", Key = Key.T.WithCtrl };
        var acceptingFired = false;

        shortcut.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true;
                              };

        bool? result = shortcut.InvokeCommand (Command.HotKey);

        Assert.True (acceptingFired);
        Assert.True (result);

        shortcut.Dispose ();
    }
}
