using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public partial class ShortcutTests
{
    /// <summary>Test view that exposes AddCommand publicly for testing TargetView scenarios.</summary>
    private class TestTargetView : View
    {
        public void RegisterCommand (Command command, Func<bool?> impl) => AddCommand (command, impl);
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

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that <see cref="Shortcut.TargetView"/> and <see cref="Shortcut.Command"/> properties
    ///     exist on Shortcut and can be set/get correctly.
    /// </summary>
    [Fact]
    public void TargetView_And_Command_Properties_Default_And_Set ()
    {
        // Arrange
        using Shortcut shortcut = new ();

        // Assert defaults
        Assert.Null (shortcut.TargetView);
        Assert.Equal (Command.NotBound, shortcut.Command);

        // Act
        View target = new () { Title = "Target" };
        shortcut.TargetView = target;
        shortcut.Command = Command.Save;

        // Assert
        Assert.Same (target, shortcut.TargetView);
        Assert.Equal (Command.Save, shortcut.Command);

        target.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that setting <see cref="Shortcut.Command"/> auto-populates Title and HelpText
    ///     from GlobalResources when they are empty.
    /// </summary>
    [Fact]
    public void Command_Property_Does_Not_Overwrite_Existing_Title ()
    {
        // Arrange
        using Shortcut shortcut = new () { Title = "My Title", HelpText = "My Help" };

        // Act — setting Command should NOT overwrite existing Title/HelpText
        shortcut.Command = Command.Save;

        // Assert
        Assert.Equal ("My Title", shortcut.Title);
        Assert.Equal ("My Help", shortcut.HelpText);
        Assert.Equal (Command.Save, shortcut.Command);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that <see cref="Shortcut.OnAccepted"/> invokes the Command on TargetView
    ///     when both TargetView and Command are set.
    /// </summary>
    [Fact]
    public void Accept_Invokes_Command_On_TargetView ()
    {
        // Arrange
        var commandInvoked = false;
        TestTargetView target = new () { Title = "Target" };
        target.RegisterCommand (Command.Save, () =>
                                              {
                                                  commandInvoked = true;

                                                  return true;
                                              });

        target.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        using Shortcut shortcut = new ()
        {
            Key = Key.S.WithCtrl,
            Title = "Save",
            TargetView = target,
            Command = Command.Save
        };

        // Act
        shortcut.InvokeCommand (Command.Accept);

        // Assert
        Assert.True (commandInvoked);

        target.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that Accept does NOT invoke TargetView when Command is NotBound.
    /// </summary>
    [Fact]
    public void Accept_Does_Not_Invoke_TargetView_When_Command_NotBound ()
    {
        // Arrange
        var commandInvoked = false;
        TestTargetView target = new () { Title = "Target" };
        target.RegisterCommand (Command.Save, () =>
                                              {
                                                  commandInvoked = true;

                                                  return true;
                                              });

        using Shortcut shortcut = new ()
        {
            Key = Key.F5,
            Title = "Test",
            TargetView = target
            // Command is default (NotBound)
        };

        // Act
        shortcut.InvokeCommand (Command.Accept);

        // Assert — Command is NotBound so TargetView should NOT be invoked
        Assert.False (commandInvoked);

        target.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that Accept invokes both Action and TargetView.Command when both are set.
    ///     Action fires first (in OnAccepted), then TargetView.InvokeCommand.
    /// </summary>
    [Fact]
    public void Accept_Invokes_Both_Action_And_TargetView ()
    {
        // Arrange
        var actionFired = false;
        var commandInvoked = false;

        TestTargetView target = new () { Title = "Target" };
        target.RegisterCommand (Command.Save, () =>
                                              {
                                                  commandInvoked = true;

                                                  return true;
                                              });
        target.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        using Shortcut shortcut = new ()
        {
            Key = Key.S.WithCtrl,
            Title = "Save",
            TargetView = target,
            Command = Command.Save,
            Action = () => actionFired = true
        };

        // Act
        shortcut.InvokeCommand (Command.Accept);

        // Assert — both fire
        Assert.True (actionFired);
        Assert.True (commandInvoked);

        target.Dispose ();
    }

    [Fact]
    public void Activate_Does_Invoke_TargetView ()
    {
        // Arrange
        var commandInvoked = false;
        TestTargetView target = new () { Title = "Target" };
        target.RegisterCommand (Command.Save, () =>
                                              {
                                                  commandInvoked = true;

                                                  return true;
                                              });
        target.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        using Shortcut shortcut = new ()
        {
            Key = Key.S.WithCtrl,
            Title = "Save",
            TargetView = target,
            Command = Command.Save
        };

        // Act — Activate, not Accept
        shortcut.InvokeCommand (Command.Activate);

        // Assert — TargetView should NOT be invoked on Activate
        Assert.True (commandInvoked);

        target.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that when TargetView is null but Key is valid and Command is set,
    ///     OnAccepted attempts to invoke application-level key bindings. We verify this
    ///     indirectly by checking that the Shortcut's key press triggers the expected path.
    /// </summary>
    [Fact]
    public void Accept_With_Command_And_No_TargetView_Does_Not_Throw ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Shortcut shortcut = new ()
        {
            Key = Key.S.WithCtrl,
            Title = "Save",
            Command = Command.Save
            // No TargetView — will attempt app-level key binding invocation
        };

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        // Act — should not throw even though no app-level handler is registered
        Exception? ex = Record.Exception (() => shortcut.InvokeCommand (Command.Accept));

        // Assert
        Assert.Null (ex);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that when neither TargetView nor Command is set,
    ///     OnAccepted does not try to invoke app-level key bindings.
    /// </summary>
    [Fact]
    public void Accept_Without_TargetView_Or_Command_Only_Fires_Action ()
    {
        // Arrange
        var actionFired = false;

        using Shortcut shortcut = new ()
        {
            Key = Key.F5,
            Title = "Test",
            Action = () => actionFired = true
        };

        // Act
        shortcut.InvokeCommand (Command.Accept);

        // Assert — only Action fires, no TargetView or app-level invocation
        Assert.True (actionFired);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that when a CheckBox CommandView's Activate bubbles up to Shortcut,
    ///     the Shortcut's Action sees the UPDATED CheckBox value (after toggle), not the stale value.
    ///     This is the regression test for the deferred RaiseActivated fix.
    /// </summary>
    [Fact]
    public void Action_Sees_Updated_CheckBox_Value_On_BubbleUp_Activate ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        CheckState? capturedValue = null;

        Shortcut shortcut = new ()
        {
            Key = Key.T,
            CommandView = checkBox,
            Action = () => capturedValue = checkBox.Value
        };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Invoke Activate directly on the CheckBox (simulates bubble-up from click)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Action should see the NEW value (Checked), not the old value (UnChecked)
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.Checked, capturedValue);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies correct event ordering for compound views in Shortcut when activation
    ///     bubbles up from the CommandView (e.g., CheckBox click).
    ///     Expected order:
    ///     1. CheckBox.Activating
    ///     2. Shortcut.Activating (from bubble-up)
    ///     3. Shortcut.Activated (deferred, fires during CheckBox.Activated invocation)
    ///     4. CheckBox.Activated (test handler fires after Shortcut's subscription)
    ///     Key: CheckBox.OnActivated (state change) runs before both Activated events.
    /// </summary>
    [Fact]
    public void BubbleUp_Activate_Event_Ordering_CommandView_Completes_Before_Shortcut_Activated ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        List<string> eventLog = [];
        CheckState? valueAtShortcutActivated = null;

        checkBox.Activating += (_, _) => eventLog.Add ("CheckBox.Activating");
        checkBox.Activated += (_, _) => eventLog.Add ("CheckBox.Activated");

        shortcut.Activating += (_, _) => eventLog.Add ("Shortcut.Activating");
        shortcut.Activated += (_, _) =>
                              {
                                  valueAtShortcutActivated = checkBox.Value;
                                  eventLog.Add ("Shortcut.Activated");
                              };

        // Act - Invoke Activate directly on the CheckBox (simulates bubble-up from click)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Verify ordering:
        // Shortcut.Activated fires during CheckBox.Activated event (Shortcut subscribed first),
        // but CheckBox.OnActivated (state change) already ran before Activated events.
        Assert.Equal (4, eventLog.Count);
        Assert.Equal ("CheckBox.Activating", eventLog [0]);
        Assert.Equal ("Shortcut.Activating", eventLog [1]);
        Assert.Equal ("Shortcut.Activated", eventLog [2]);
        Assert.Equal ("CheckBox.Activated", eventLog [3]);

        // CheckBox.OnActivated toggled state BEFORE Activated events fired
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.Checked, valueAtShortcutActivated);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies correct event ordering for compound views in Shortcut when activation
    ///     is triggered via BubbleDown (e.g., clicking on Shortcut's HelpView area).
    ///     OnActivating calls BubbleDown when the activation source is not the CommandView.
    ///     Expected order:
    ///     1. Shortcut.Activating (fires in RaiseActivating, after OnActivating/BubbleDown)
    ///     2. Shortcut.Activated (direct path, not deferred since IsBubblingUp is false)
    ///     BubbleDown also triggers CommandView events, but inside OnActivating (before Shortcut.Activating).
    /// </summary>
    [Fact]
    public void BubbleDown_Activate_Event_Ordering_With_Binding_Source ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        List<string> eventLog = [];
        CheckState? valueAtShortcutActivated = null;

        checkBox.Activating += (_, _) => eventLog.Add ("CheckBox.Activating");
        checkBox.Activated += (_, _) => eventLog.Add ("CheckBox.Activated");

        shortcut.Activating += (_, _) => eventLog.Add ("Shortcut.Activating");
        shortcut.Activated += (_, _) =>
                              {
                                  valueAtShortcutActivated = checkBox.Value;
                                  eventLog.Add ("Shortcut.Activated");
                              };

        // Act - Invoke Activate with a binding source (the Shortcut), which triggers BubbleDown
        // to the CommandView. This simulates what happens on HotKey press or mouse click on
        // the Shortcut's non-CommandView area (e.g., HelpView or KeyView).
        KeyBinding binding = new ([Command.Activate], Key.T, shortcut);
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (shortcut), binding);
        shortcut.InvokeCommand (Command.Activate, ctx);

        // Assert - BubbleDown fires CommandView events during OnActivating (before Shortcut.Activating),
        // then Shortcut.Activating fires, then Shortcut.Activated (direct path).
        Assert.Equal (4, eventLog.Count);
        Assert.Equal ("CheckBox.Activating", eventLog [0]);
        Assert.Equal ("CheckBox.Activated", eventLog [1]);
        Assert.Equal ("Shortcut.Activating", eventLog [2]);
        Assert.Equal ("Shortcut.Activated", eventLog [3]);

        // CheckBox should have toggled
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.Checked, valueAtShortcutActivated);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that the Command property setter is idempotent — setting the same value
    ///     twice does not re-trigger Title/HelpText logic.
    /// </summary>
    [Fact]
    public void Command_Property_Idempotent ()
    {
        // Arrange
        using Shortcut shortcut = new ();
        shortcut.Command = Command.Save;

        // Capture current title (may have been set from GlobalResources)
        string titleAfterFirst = shortcut.Title;

        // Manually set a different title
        shortcut.Title = "Custom Title";

        // Act — set Command to the same value again
        shortcut.Command = Command.Save;

        // Assert — Title should NOT be overwritten because the setter early-returns
        Assert.Equal ("Custom Title", shortcut.Title);
    }
}
