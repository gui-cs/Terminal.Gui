using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public partial class ShortcutTests
{
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
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

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
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.True (checkBox.CanFocus);

        // Act - Directly invoke Command.Activate on the CheckBox
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=true SHOULD change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that direct InvokeCommand(Activate) on Shortcut does NOT forward to CommandView.
    ///     OnActivating only forwards to CommandView when the activation comes from:
    ///     1. IsFromCommandView - the command originated from CommandView
    ///     2. IsBindingFromShortcut - the command came via a keyboard/mouse binding
    ///     Direct InvokeCommand has neither, so CommandView is not activated.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_False_Direct_InvokeCommand_Does_Not_Change_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.False (checkBox.CanFocus);

        // Act - Invoke Command.Activate directly on the Shortcut (no binding)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - CheckBox does NOT change state because direct InvokeCommand
        // doesn't go through OnActivating's forwarding logic
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that direct InvokeCommand(Activate) on Shortcut does NOT forward to CommandView,
    ///     even when CommandView.CanFocus=true.
    ///     OnActivating only forwards to CommandView when the activation comes via proper bindings.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_True_Direct_InvokeCommand_Does_Not_Change_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.True (checkBox.CanFocus);

        // Act - Invoke Command.Activate directly on the Shortcut (no binding)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - CheckBox does NOT change state because direct InvokeCommand
        // doesn't go through OnActivating's forwarding logic
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a mouse click on a CheckBox CommandView changes its state.
    ///     CheckBox binds LeftButtonClicked to Command.Activate (overriding the base
    ///     View's LeftButtonReleased binding to prevent double activation on double-click).
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_MouseRelease_Changes_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        // BUGBUG: This test tests nothing.
        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Verify CheckBox has the expected mouse binding for LeftButtonClicked
        // (CheckBox overrides the default LeftButtonReleased with LeftButtonClicked to prevent double activation)
        Assert.True (checkBox.MouseBindings.TryGet (MouseFlags.LeftButtonClicked, out MouseBinding binding));
        Assert.Contains (Command.Activate, binding.Commands);

        // Act - Simulate a mouse click by invoking the bound command directly
        checkBox.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });

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
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Directly invoke Command.Activate on the CheckBox (what mouse press should trigger)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox should change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    [Fact]
    public void CommandView_Command_Activate_Bubbles_To_Shortcut ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        // Act - Invoke Command.HotKey directly on CheckBox
        testCommandView.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, commandViewActivatingRaised);
    }

    [Fact]
    public void CommandView_Command_Accept_Forwards_To_Accepting ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var shortcutAcceptingRaised = 0;
        shortcut.Accepting += (_, _) => { shortcutAcceptingRaised++; };

        var commandViewAcceptingRaised = 0;
        testCommandView.Accepting += (_, _) => { commandViewAcceptingRaised++; };

        // Act - Invoke Command.HotKey directly on CheckBox
        testCommandView.InvokeCommand (Command.Accept);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutAcceptingRaised);
        Assert.Equal (1, commandViewAcceptingRaised);
        Assert.Equal (0, shortcutActivatingRaised);
        Assert.Equal (0, commandViewActivatingRaised);
    }

    [Fact]
    public void Command_Activate_Direct_InvokeCommand_Raises_Activating_Once ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        shortcut.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should be raised exactly once
        Assert.Equal (1, shortcutActivatingCount);
    }

    [Fact]
    public void CommandView_Command_HotKey_Ignored_Raised_By_Shortcut ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var shortcutHandlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => { shortcutHandlingHotKeyFired++; };

        var commandViewHandlingHotKeyFired = 0;
        testCommandView.HandlingHotKey += (_, _) => { commandViewHandlingHotKeyFired++; };

        // Act - Invoke Command.HotKey directly on CommandView
        testCommandView.InvokeCommand (Command.HotKey);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (0, shortcutHandlingHotKeyFired);
        Assert.Equal (1, commandViewHandlingHotKeyFired);
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, commandViewActivatingRaised);
    }

    [Fact]
    public void Command_Activate_Raises_Activating_Only ()
    {
        Shortcut shortcut = new ();
        var activatingFired = 0;

        shortcut.Activating += (_, _) => activatingFired++;

        var acceptingFired = 0;
        shortcut.Accepting += (_, _) => acceptingFired++;

        var handlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => handlingHotKeyFired++;

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (1, activatingFired);
        Assert.Equal (0, acceptingFired);
        Assert.Equal (0, handlingHotKeyFired);

        shortcut.Dispose ();
    }

    [Fact]
    public void Command_Accept_Raises_Accepting_Only ()
    {
        using Shortcut shortcut = new ();
        shortcut.Title = "Test";
        shortcut.Key = Key.T.WithCtrl;
        var activatingFired = false;

        shortcut.Activating += (_, _) => { activatingFired = true; };

        var acceptingFired = false;
        shortcut.Accepting += (_, _) => { acceptingFired = true; };

        var handlingHotKeyFired = false;
        shortcut.HandlingHotKey += (_, _) => { handlingHotKeyFired = true; };

        shortcut.InvokeCommand (Command.Accept);

        Assert.False (activatingFired);
        Assert.True (acceptingFired);
        Assert.False (handlingHotKeyFired);
    }

    [Fact]
    public void Command_Accept_Direct_InvokeCommand_Raises_Accepting_Once ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutAcceptingCount = 0;
        shortcut.Accepting += (_, _) => shortcutAcceptingCount++;

        shortcut.InvokeCommand (Command.Accept);

        Assert.Equal (1, shortcutAcceptingCount);
    }

    [Fact]
    public void Command_HotKey_Raises_HandlingHotKey_Then_Activating ()
    {
        using Shortcut shortcut = new ();
        var activatingFired = 0;

        shortcut.Activating += (_, _) => { activatingFired++; };

        var acceptingFired = 0;
        shortcut.Accepting += (_, _) => { acceptingFired++; };

        var handlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => { handlingHotKeyFired++; };

        shortcut.InvokeCommand (Command.HotKey);

        Assert.Equal (1, handlingHotKeyFired);
        Assert.Equal (1, activatingFired);
        Assert.Equal (0, acceptingFired);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that Accept does NOT invoke Activate. These are separate command paths per the design:
    ///     Accept = confirm/execute, Activate = change state.
    /// </summary>
    [Fact]
    public void Accept_Does_Not_Invoke_Activate ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var shortcutAcceptingCount = 0;
        shortcut.Accepting += (_, _) => shortcutAcceptingCount++;

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Invoke Accept directly on the Shortcut
        shortcut.InvokeCommand (Command.Accept);

        // Assert - Accepting fires, Activating does NOT fire, CheckBox state unchanged
        Assert.Equal (1, shortcutAcceptingCount);
        Assert.Equal (0, shortcutActivatingCount);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that when CommandView's Activate bubbles up to Shortcut, the Shortcut
    ///     returns false (not handled) so the originating CommandView can complete its own
    ///     activation (e.g., CheckBox toggles state in OnActivated).
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_Activate_Bubbles_Up_And_CheckBox_Toggles ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var shortcutActivatedCount = 0;
        shortcut.Activated += (_, _) => shortcutActivatedCount++;

        var checkBoxActivatingCount = 0;
        checkBox.Activating += (_, _) => checkBoxActivatingCount++;

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Invoke Activate directly on the CheckBox (simulates what happens when
        // CheckBox processes a command internally, e.g., from mouse click or HotKey)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Both Activating events fire once, and CheckBox toggles state
        Assert.Equal (1, checkBoxActivatingCount);
        Assert.Equal (1, shortcutActivatingCount);
        Assert.Equal (1, shortcutActivatedCount);
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that Action is invoked from both OnActivated and OnAccepted paths,
    ///     as documented in shortcut.md.
    /// </summary>
    [Fact]
    public void Action_Invoked_From_Both_Activate_And_Accept ()
    {
        // Arrange
        var actionCount = 0;

        Shortcut shortcut = new () { Key = Key.F5, Title = "Test", Action = () => actionCount++ };

        // Act 1 - Activate
        shortcut.InvokeCommand (Command.Activate);
        Assert.Equal (1, actionCount);

        // Act 2 - Accept
        shortcut.InvokeCommand (Command.Accept);
        Assert.Equal (2, actionCount);
    }
}
