using Terminal.Gui.Tests;
using Terminal.Gui.Tracing;
using Xunit.Abstractions;

namespace ViewBaseTests.Commands;

public class ViewCommandTests (ITestOutputHelper output)
{
    #region OnAccept/Accept tests

    [Fact]
    public void Accept_Command_Raised_With_HasFocus_False ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        view.InvokeCommand (Command.Accept);

        Assert.Equal (1, view.OnAcceptedCount);

        Assert.Equal (1, view.AcceptedCount);

        Assert.False (view.HasFocus);
    }

    [Fact]
    public void Accept_Handle_Event_OnAccepting_Returns_True ()
    {
        var view = new View ();
        var acceptInvokedCount = 0;

        view.Accepting += ViewOnAccepting;

        bool? ret = view.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.Equal (1, acceptInvokedCount);

        return;

        void ViewOnAccepting (object? sender, CommandEventArgs e)
        {
            acceptInvokedCount++;
            e.Handled = true;
        }
    }

    [Fact]
    public void Accept_Command_Invokes_Accepting_Event ()
    {
        var view = new View ();
        var acceptedCount = 0;

        view.Accepting += ViewOnAccepting;

        view.InvokeCommand (Command.Accept);
        Assert.Equal (1, acceptedCount);

        return;

        void ViewOnAccepting (object? sender, CommandEventArgs e) => acceptedCount++;
    }

    // Accept on subview should bubble up to parent
    [Fact]
    public void Accept_Command_Bubbles_Up_To_SuperView ()
    {
        var view = new ViewEventTester { Id = "view" };
        view.CommandsToBubbleUp = [Command.Accept];
        var subview = new ViewEventTester { Id = "subview" };
        view.Add (subview);

        subview.InvokeCommand (Command.Accept);
        Assert.Equal (1, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        subview.HandleOnAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (2, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        subview.HandleOnAccepted = false;
        subview.HandleAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (3, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        // Add a super view to test deeper hierarchy
        var superView = new ViewEventTester { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Accept];
        superView.Add (view);

        subview.InvokeCommand (Command.Accept);
        Assert.Equal (4, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);
        Assert.Equal (0, superView.OnAcceptedCount);

        subview.HandleAccepted = false;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (5, subview.OnAcceptedCount);
        Assert.Equal (2, view.OnAcceptedCount);
        Assert.Equal (1, superView.OnAcceptedCount);

        view.HandleAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (6, subview.OnAcceptedCount);
        Assert.Equal (3, view.OnAcceptedCount);
        Assert.Equal (1, superView.OnAcceptedCount);
    }

    #endregion OnAccept/Accept tests

    #region Accepted tests

    [Fact]
    public void Accepted_Raised_When_Accepting_Not_Handled ()
    {
        View view = new ();
        var acceptedInvokedCount = 0;

        view.Accepting += (_, e) => { e.Handled = false; };

        view.Accepted += (_, _) => { acceptedInvokedCount++; };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (1, acceptedInvokedCount);
    }

    [Fact]
    public void Accepted_Not_Raised_When_Accepting_Handled ()
    {
        View view = new ();
        var acceptedInvokedCount = 0;

        view.Accepting += (_, e) => { e.Handled = true; };

        view.Accepted += (_, _) => { acceptedInvokedCount++; };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (0, acceptedInvokedCount);
    }

    [Fact]
    public void Accepted_Event_Cannot_Be_Cancelled ()
    {
        View view = new ();
        var acceptedInvokedCount = 0;

        view.Accepted += (_, e) =>
                         {
                             acceptedInvokedCount++;

                             // Accepted event has Handled property, but it doesn't affect flow
                             e.Handled = false;
                         };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (1, acceptedInvokedCount);
    }

    [Fact]
    public void OnAccepted_Called_When_Accepting_Not_Handled ()
    {
        OnAcceptedTestView view = new ();

        view.Accepting += (_, e) => { e.Handled = false; };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (1, view.OnAcceptedCallCount);
    }

    [Fact]
    public void OnAccepted_Not_Called_When_Accepting_Handled ()
    {
        OnAcceptedTestView view = new ();

        view.Accepting += (_, e) => { e.Handled = true; };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (0, view.OnAcceptedCallCount);
    }

    private class OnAcceptedTestView : View
    {
        public int OnAcceptedCallCount { get; private set; }

        protected override void OnAccepted (ICommandContext? ctx)
        {
            OnAcceptedCallCount++;
            base.OnAccepted (ctx);
        }
    }

    #endregion Accepted tests

    #region OnActivating/Activating tests

    [Theory]
    [CombinatorialData]
    public void Activate_Command_Raises_SetsFocus (bool canFocus)
    {
        var view = new ViewEventTester { CanFocus = canFocus };

        Assert.Equal (canFocus, view.CanFocus);
        Assert.False (view.HasFocus);

        view.InvokeCommand (Command.Activate);

        Assert.Equal (1, view.OnActivatingCount);

        Assert.Equal (1, view.ActivatingCount);

        Assert.Equal (canFocus, view.HasFocus);
    }

    [Fact]
    public void Activate_Command_Handle_OnActivating_NoEvent ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        view.HandleOnActivating = true;
        Assert.True (view.InvokeCommand (Command.Activate));

        Assert.Equal (1, view.OnActivatingCount);

        Assert.Equal (0, view.ActivatingCount);
    }

    [Fact]
    public void Activate_Command_Handle_Event_OnActivating_Returns_True ()
    {
        var view = new View ();
        var activatingInvokedCount = 0;

        view.Activating += ViewOnActivating;

        bool? ret = view.InvokeCommand (Command.Activate);
        Assert.True (ret);
        Assert.Equal (1, activatingInvokedCount);

        return;

        void ViewOnActivating (object? sender, CommandEventArgs e)
        {
            activatingInvokedCount++;
            e.Handled = true;
        }
    }

    [Fact]
    public void Activate_Command_Invokes_Activating_Event ()
    {
        var view = new View ();
        var activatingCount = 0;

        view.Activating += ViewOnActivating;

        view.InvokeCommand (Command.Activate);
        Assert.Equal (1, activatingCount);

        return;

        void ViewOnActivating (object? sender, CommandEventArgs e) => activatingCount++;
    }

    [Fact]
    public void LeftButtonReleased_Invokes_Activate_Command ()
    {
        var view = new ViewEventTester ();
        view.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased, Position = Point.Empty, View = view });

        Assert.Equal (1, view.OnActivatingCount);
    }

    #endregion OnActivating/Activating tests

    #region HotKey tests

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new View ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Activates ()
    {
        var view = new View ();

        view.CanFocus = true;
        view.InvokeCommand (Command.HotKey);

        var activatingInvoked = 0;
        view.Activating += ViewOnActivating;

        view.InvokeCommand (Command.Activate);
        Assert.Equal (1, activatingInvoked);

        return;

        void ViewOnActivating (object? sender, CommandEventArgs e)
        {
            activatingInvoked++;
            e.Handled = true;
        }
    }

    #endregion HotKey tests

    #region InvokeCommand Tests

    [Fact]
    public void InvokeCommand_NotBound_Invokes_CommandNotBound ()
    {
        ViewEventTester view = new ();

        view.InvokeCommand (Command.NotBound);

        Assert.False (view.HasFocus);
        Assert.Equal (1, view.OnCommandNotBoundCount);
        Assert.Equal (1, view.CommandNotBoundCount);
    }

    [Fact]
    public void InvokeCommand_Command_Not_Bound_Invokes_CommandNotBound ()
    {
        ViewEventTester view = new ();

        view.InvokeCommand (Command.New);

        Assert.False (view.HasFocus);
        Assert.Equal (1, view.OnCommandNotBoundCount);
        Assert.Equal (1, view.CommandNotBoundCount);
    }

    [Fact]
    public void InvokeCommand_Command_Bound_Does_Not_Invoke_CommandNotBound ()
    {
        ViewEventTester view = new ();

        view.InvokeCommand (Command.Accept);

        Assert.False (view.HasFocus);
        Assert.Equal (0, view.OnCommandNotBoundCount);
        Assert.Equal (0, view.CommandNotBoundCount);
    }

    #endregion

    #region Command Propagation Tests

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_DefaultIsEmpty ()
    {
        View view = new ();
        Assert.Equal ([], view.CommandsToBubbleUp);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void Accept_Command_DoesNotBubbleByDefault ()
    {
        View superView = new ();
        View subView = new ();
        superView.Add (subView);

        var superViewAcceptingCalledCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCalledCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, superViewAcceptingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void Activate_Command_DoesNotBubbleByDefault ()
    {
        View superView = new ();
        View subView = new ();
        superView.Add (subView);

        var superViewActivatingCalledCount = 0;
        superView.Activating += (_, _) => superViewActivatingCalledCount++;

        subView.InvokeCommand (Command.Activate);

        Assert.Equal (0, superViewActivatingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_CanDisableAllPropagation ()
    {
        View superView = new () { CommandsToBubbleUp = [] };
        View subView = new ();
        superView.Add (subView);

        var superViewAcceptingCalledCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCalledCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, superViewAcceptingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_CanBeCustomized ()
    {
        View superView = new () { CommandsToBubbleUp = [Command.Accept, Command.Activate] };
        View subView = new ();
        superView.Add (subView);

        var superViewActivatingCalledCount = 0;
        superView.Activating += (_, _) => superViewActivatingCalledCount++;

        subView.InvokeCommand (Command.Activate);

        Assert.Equal (1, superViewActivatingCalledCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Activate_BubblingUp_Fires_Activated_On_SuperView ()
    {
        View superView = new () { CommandsToBubbleUp = [Command.Activate] };
        View subView = new ();
        superView.Add (subView);

        var activatedCount = 0;
        superView.Activated += (_, _) => activatedCount++;

        subView.InvokeCommand (Command.Activate);

        Assert.Equal (1, activatedCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Activate_BubblingUp_Fires_Activated_In_Deep_Hierarchy ()
    {
        View grandSuperView = new () { CommandsToBubbleUp = [Command.Activate] };
        View superView = new () { CommandsToBubbleUp = [Command.Activate] };
        View subView = new ();
        grandSuperView.Add (superView);
        superView.Add (subView);

        var grandActivatedCount = 0;
        grandSuperView.Activated += (_, _) => grandActivatedCount++;

        subView.InvokeCommand (Command.Activate);

        Assert.Equal (1, grandActivatedCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void ConsumeDispatch_Blocks_Further_Bubbling ()
    {
        // OptionSelector uses ConsumeDispatch=true — activation should NOT
        // propagate from its inner CheckBox to OptionSelector's SuperView
        View superView = new () { CommandsToBubbleUp = [Command.Activate] };
        OptionSelector selector = new () { Labels = ["Option1", "Option2"] };
        superView.Add (selector);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Activate an inner CheckBox with a binding (required for dispatch to occur)
        CheckBox innerCb = selector.SubViews.OfType<CheckBox> ().First ();
        KeyBinding binding = new ([Command.Activate], Key.Space, innerCb);
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (innerCb), binding);
        innerCb.InvokeCommand (Command.Activate, ctx);

        // Consume-dispatch blocks propagation to SuperView
        Assert.Equal (0, superViewActivatingCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_StopsWhenHandled ()
    {
        View superView = new () { CommandsToBubbleUp = [Command.Accept] };
        View subView = new ();
        superView.Add (subView);

        var superViewAcceptingCalledCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCalledCount++;

        // SubView handles the command
        subView.Accepting += (_, e) => e.Handled = true;

        subView.InvokeCommand (Command.Accept);

        // Should NOT propagate because subView handled it
        Assert.Equal (0, superViewAcceptingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_WorksInDeepHierarchy ()
    {
        View grandSuperView = new () { CommandsToBubbleUp = [Command.Accept] };
        View superView = new () { CommandsToBubbleUp = [Command.Accept] };
        View subView = new ();

        grandSuperView.Add (superView);
        superView.Add (subView);

        var grandSuperViewAcceptingCalledCount = 0;
        grandSuperView.Accepting += (_, _) => grandSuperViewAcceptingCalledCount++;

        var grandSuperViewAcceptedCalledCount = 0;
        grandSuperView.Accepted += (_, _) => grandSuperViewAcceptedCalledCount++;

        subView.InvokeCommand (Command.Accept);

        // Should propagate all the way up
        Assert.Equal (1, grandSuperViewAcceptingCalledCount);
        Assert.Equal (1, grandSuperViewAcceptedCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_StopsAtIntermediateHandler ()
    {
        View grandSuperView = new () { CommandsToBubbleUp = [Command.Accept] };
        View superView = new () { CommandsToBubbleUp = [Command.Accept] };
        View subView = new ();

        grandSuperView.Add (superView);
        superView.Add (subView);

        var grandSuperViewAcceptingCalledCount = 0;
        grandSuperView.Accepting += (_, _) => grandSuperViewAcceptingCalledCount++;

        // SuperView handles it, so shouldn't propagate further
        superView.Accepting += (_, e) => e.Handled = true;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, grandSuperViewAcceptingCalledCount);
    }

    #endregion Command Propagation Tests

    #region GetSupportedCommands Tests

    [Fact]
    public void GetSupportedCommands_Returns_DefaultCommands ()
    {
        View view = new ();

        IEnumerable<Command> commands = view.GetSupportedCommands ();

        Assert.Contains (Command.Activate, commands);
        Assert.Contains (Command.Accept, commands);
        Assert.Contains (Command.HotKey, commands);
        Assert.Contains (Command.NotBound, commands);
    }

    [Fact]
    public void GetSupportedCommands_DoesNotContain_Unsupported_Commands ()
    {
        View view = new ();

        IEnumerable<Command> commands = view.GetSupportedCommands ();

        // Command.New is not bound by default on View
        Assert.DoesNotContain (Command.New, commands);
    }

    #endregion

    #region InvokeCommands Tests

    [Fact]
    public void InvokeCommands_Invokes_Multiple_Commands ()
    {
        var view = new ViewEventTester ();

        view.InvokeCommands ([Command.Activate, Command.Accept], null);

        Assert.Equal (1, view.OnActivatingCount);
        Assert.Equal (1, view.OnAcceptedCount);
    }

    [Fact]
    public void InvokeCommands_Returns_True_If_Any_Command_Handled ()
    {
        var view = new ViewEventTester ();
        view.HandleOnActivating = true;

        bool? result = view.InvokeCommands ([Command.Activate, Command.Accept], null);

        Assert.True (result);
    }

    [Fact]
    public void InvokeCommands_Returns_False_If_No_Command_Handled ()
    {
        var view = new ViewEventTester ();

        bool? result = view.InvokeCommands ([Command.Activate, Command.Accept], null);

        // Neither Activate nor Accept is genuinely handled by a plain view with no dispatch
        // target and no bubble config — both return false, allowing key propagation to continue.
        Assert.False (result);
    }

    [Fact]
    public void InvokeCommands_EmptyArray_Returns_Null ()
    {
        var view = new View ();

        bool? result = view.InvokeCommands ([], null);

        Assert.Null (result);
    }

    #endregion

    #region InvokeCommand With Binding Tests

    [Fact]
    public void InvokeCommand_WithKeyBinding_PassesBindingInContext ()
    {
        var view = new View ();
        KeyBinding keyBinding = new ([Command.Accept]) { Key = Key.Enter };
        ICommandContext? receivedContext = null;

        view.Accepting += (_, e) => receivedContext = e.Context;

        view.InvokeCommand (Command.Accept, keyBinding);

        Assert.NotNull (receivedContext);
        Assert.Equal (Command.Accept, receivedContext!.Command);

        if (receivedContext.Binding is KeyBinding kb)
        {
            Assert.Equal (Key.Enter, kb.Key);
        }
        else
        {
            Assert.Fail ("Binding should be KeyBinding");
        }
    }

    [Fact]
    public void InvokeCommand_WithMouseBinding_PassesBindingInContext ()
    {
        var view = new View ();
        MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonClicked);
        ICommandContext? receivedContext = null;

        view.Activating += (_, e) => receivedContext = e.Context;

        view.InvokeCommand (Command.Activate, mouseBinding);

        Assert.NotNull (receivedContext);

        if (receivedContext!.Binding is MouseBinding mb)
        {
            Assert.Equal (MouseFlags.LeftButtonClicked, mb.MouseEvent?.Flags);
        }
        else
        {
            Assert.Fail ("Binding should be MouseBinding");
        }
    }

    [Fact]
    public void InvokeCommand_WithNullBinding_ContextHasNullBinding ()
    {
        var view = new View ();
        ICommandContext? receivedContext = null;

        view.Accepting += (_, e) => receivedContext = e.Context;

        view.InvokeCommand (Command.Accept, (ICommandBinding?)null);

        Assert.NotNull (receivedContext);
        Assert.Null (receivedContext!.Binding);
    }

    [Fact]
    public void InvokeCommand_WithContext_PassesContextToHandler ()
    {
        var view = new View ();
        View sourceView = new () { Id = "source" };
        KeyBinding keyBinding = new ([Command.Accept]) { Key = Key.F1 };
        CommandContext ctx = new () { Command = Command.Accept, Source = new WeakReference<View> (sourceView), Binding = keyBinding };

        ICommandContext? receivedContext = null;
        view.Accepting += (_, e) => receivedContext = e.Context;

        view.InvokeCommand (Command.Accept, ctx);

        Assert.NotNull (receivedContext);
        Assert.Equal (Command.Accept, receivedContext!.Command);

        View? source = null;
        receivedContext.Source?.TryGetTarget (out source);
        Assert.Equal ("source", source?.Id);
    }

    #endregion

    #region Activated Tests

    [Fact]
    public void Activated_Raised_When_Activating_Not_Handled ()
    {
        View view = new ();
        var activatedInvokedCount = 0;

        view.Activating += (_, e) => { e.Handled = false; };
        view.Activated += (_, _) => { activatedInvokedCount++; };

        view.InvokeCommand (Command.Activate);

        Assert.Equal (1, activatedInvokedCount);
    }

    [Fact]
    public void Activated_Not_Raised_When_Activating_Handled ()
    {
        View view = new ();
        var activatedInvokedCount = 0;

        view.Activating += (_, e) => { e.Handled = true; };
        view.Activated += (_, _) => { activatedInvokedCount++; };

        view.InvokeCommand (Command.Activate);

        Assert.Equal (0, activatedInvokedCount);
    }

    [Fact]
    public void Activated_Event_Receives_Context ()
    {
        View view = new ();
        ICommandContext? receivedContext = null;

        view.Activated += (_, e) => { receivedContext = e.Value; };

        view.InvokeCommand (Command.Activate);

        Assert.NotNull (receivedContext);
        Assert.Equal (Command.Activate, receivedContext!.Command);
    }

    private class OnActivatedTestView : View
    {
        public int OnActivatedCallCount { get; private set; }

        protected override void OnActivated (ICommandContext? ctx)
        {
            OnActivatedCallCount++;
            base.OnActivated (ctx);
        }
    }

    [Fact]
    public void OnActivated_Called_When_Activating_Not_Handled ()
    {
        OnActivatedTestView view = new ();

        view.Activating += (_, e) => { e.Handled = false; };

        view.InvokeCommand (Command.Activate);

        Assert.Equal (1, view.OnActivatedCallCount);
    }

    [Fact]
    public void OnActivated_Not_Called_When_Activating_Handled ()
    {
        OnActivatedTestView view = new ();

        view.Activating += (_, e) => { e.Handled = true; };

        view.InvokeCommand (Command.Activate);

        Assert.Equal (0, view.OnActivatedCallCount);
    }

    #endregion

    #region HotKeyCommand Tests

    [Fact]
    public void HotKeyCommand_Raised_When_HandlingHotKey_Not_Handled ()
    {
        View view = new ();
        var hotKeyCommandInvokedCount = 0;

        view.HandlingHotKey += (_, e) => { e.Handled = false; };
        view.HotKeyCommand += (_, _) => { hotKeyCommandInvokedCount++; };

        view.InvokeCommand (Command.HotKey);

        Assert.Equal (1, hotKeyCommandInvokedCount);
    }

    [Fact]
    public void HotKeyCommand_Not_Raised_When_HandlingHotKey_Handled ()
    {
        View view = new ();
        var hotKeyCommandInvokedCount = 0;

        view.HandlingHotKey += (_, e) => { e.Handled = true; };
        view.HotKeyCommand += (_, _) => { hotKeyCommandInvokedCount++; };

        view.InvokeCommand (Command.HotKey);

        Assert.Equal (0, hotKeyCommandInvokedCount);
    }

    [Fact]
    public void HotKeyCommand_Event_Receives_Context ()
    {
        View view = new ();
        ICommandContext? receivedContext = null;

        view.HotKeyCommand += (_, e) => { receivedContext = e.Value; };

        view.InvokeCommand (Command.HotKey);

        Assert.NotNull (receivedContext);
        Assert.Equal (Command.HotKey, receivedContext!.Command);
    }

    private class OnHotKeyCommandTestView : View
    {
        public int OnHotKeyCommandCallCount { get; private set; }

        protected override void OnHotKeyCommand (ICommandContext? ctx)
        {
            OnHotKeyCommandCallCount++;
            base.OnHotKeyCommand (ctx);
        }
    }

    [Fact]
    public void OnHotKeyCommand_Called_When_HandlingHotKey_Not_Handled ()
    {
        OnHotKeyCommandTestView view = new ();

        view.HandlingHotKey += (_, e) => { e.Handled = false; };

        view.InvokeCommand (Command.HotKey);

        Assert.Equal (1, view.OnHotKeyCommandCallCount);
    }

    [Fact]
    public void OnHotKeyCommand_Not_Called_When_HandlingHotKey_Handled ()
    {
        OnHotKeyCommandTestView view = new ();

        view.HandlingHotKey += (_, e) => { e.Handled = true; };

        view.InvokeCommand (Command.HotKey);

        Assert.Equal (0, view.OnHotKeyCommandCallCount);
    }

    #endregion

    #region DefaultAcceptView Tests

    [Fact]
    public void DefaultAcceptView_Default_IsNull_WhenNoIsDefaultButton ()
    {
        View view = new ();

        Assert.Null (view.DefaultAcceptView);
    }

    [Fact]
    public void DefaultAcceptView_CanBeSet ()
    {
        View view = new ();
        View acceptView = new () { Id = "acceptView" };

        view.DefaultAcceptView = acceptView;

        Assert.Equal (acceptView, view.DefaultAcceptView);
    }

    [Fact]
    public void DefaultAcceptView_FindsIsDefaultTarget ()
    {
        View superView = new ();
        AcceptTargetTestView defaultButton = new () { IsDefault = true, Id = "defaultButton" };
        superView.Add (defaultButton);

        Assert.Equal (defaultButton, superView.DefaultAcceptView);
    }

    [Fact]
    public void DefaultAcceptView_ExplicitSetting_OverridesAcceptTargetTestView ()
    {
        View superView = new ();
        AcceptTargetTestView defaultButton = new () { IsDefault = true, Id = "defaultButton" };
        View customAcceptView = new () { Id = "customAcceptView" };
        superView.Add (defaultButton);

        superView.DefaultAcceptView = customAcceptView;

        Assert.Equal (customAcceptView, superView.DefaultAcceptView);
    }

    [Fact]
    public void DefaultAcceptView_Peer_Accept_Bubbles_To_DefaultAcceptView ()
    {
        View superView = new () { CanFocus = true };
        AcceptTargetTestView nonDefaultAcceptView = new () { IsDefault = false, Id = "nonDefaultAcceptView", CanFocus = true };
        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };

        superView.Add (nonDefaultAcceptView);
        superView.Add (defaultAcceptView);
        superView.CommandsToBubbleUp = [Command.Accept];
        superView.DefaultAcceptView = defaultAcceptView;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;
        var defaultAcceptViewAcceptedCount = 0;
        defaultAcceptView.Accepted += (_, _) => defaultAcceptViewAcceptedCount++;

        var subViewAcceptingCount = 0;
        nonDefaultAcceptView.Accepting += (_, _) => subViewAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        var superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        nonDefaultAcceptView.InvokeCommand (Command.Accept);

        Assert.Equal (1, superViewAcceptingCount);
        Assert.Equal (1, superViewAcceptedCount);
        Assert.Equal (1, subViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptedCount);
    }

    [Fact]
    public void DefaultAcceptView_Non_IAcceptTarget_Peer_Accept_Bubbles_To_DefaultAcceptView ()
    {
        View superView = new () { CanFocus = true };
        View subView = new () { Id = "subView", CanFocus = true };
        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };

        superView.Add (subView);
        superView.Add (defaultAcceptView);
        superView.CommandsToBubbleUp = [Command.Accept];
        superView.DefaultAcceptView = defaultAcceptView;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        var defaultAcceptViewAcceptedCount = 0;
        defaultAcceptView.Accepted += (_, _) => defaultAcceptViewAcceptedCount++;

        var subViewAcceptingCount = 0;
        subView.Accepting += (_, _) => subViewAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        var superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (1, superViewAcceptingCount);
        Assert.Equal (1, superViewAcceptedCount);
        Assert.Equal (1, subViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptedCount);
    }

    [Fact]
    public void DefaultAcceptView_Peer_IAcceptTarget_NonDefault_Accept_Bubbles_To_DefaultAcceptView ()
    {
        View superView = new () { CanFocus = true };
        AcceptTargetTestView subView = new () { IsDefault = false, Id = "subView", CanFocus = true };
        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };

        superView.Add (subView);
        superView.Add (defaultAcceptView);
        superView.CommandsToBubbleUp = [Command.Accept];
        superView.DefaultAcceptView = defaultAcceptView;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;
        var defaultAcceptViewAcceptedCount = 0;
        defaultAcceptView.Accepted += (_, _) => defaultAcceptViewAcceptedCount++;

        var subViewAcceptingCount = 0;
        subView.Accepting += (_, _) => subViewAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (1, superViewAcceptingCount);
        Assert.Equal (1, subViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptedCount);
    }

    [Fact]
    public void DefaultAcceptView_Accept_DoesNotBubble_To_DefaultAcceptView_WhenHandled ()
    {
        View superView = new () { CanFocus = true };
        View subView = new () { CanFocus = true };
        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };

        superView.Add (subView);
        superView.Add (defaultAcceptView);

        subView.Accepting += (_, e) => e.Handled = true;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, defaultAcceptViewAcceptingCount);
    }

    #endregion

    #region IAcceptTarget Tests

    // CoPilot - ChatGPT o1
    /// <summary>
    ///     Test view that implements <see cref="IAcceptTarget"/> to verify accept target behavior.
    /// </summary>
    private class AcceptTargetTestView : View, IAcceptTarget
    {
        public AcceptTargetTestView () => CanFocus = true;

        public bool IsDefault { get; set; }
    }

    // CoPilot - ChatGPT o1
    [Fact]
    public void NonIAcceptTarget_Without_CommandsToBubbleUp_DoesNotRedirect_To_DefaultAcceptView ()
    {
        View superView = new () { CanFocus = true };
        View nonAcceptTarget = new () { Id = "nonAcceptTarget", CanFocus = true };
        View defaultAcceptView = new () { Id = "defaultAcceptView", CanFocus = true };

        superView.Add (nonAcceptTarget);
        superView.Add (defaultAcceptView);
        superView.DefaultAcceptView = defaultAcceptView;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        // Non-IAcceptTarget should redirect to DefaultAcceptView
        nonAcceptTarget.InvokeCommand (Command.Accept);

        Assert.Equal (0, defaultAcceptViewAcceptingCount);
        Assert.Equal (0, superViewAcceptingCount);
    }

    // CoPilot - ChatGPT o1
    [Fact]
    public void Button_Implements_IAcceptTarget ()
    {
        Button button = new ();

        Assert.IsAssignableFrom<IAcceptTarget> (button);
    }

    // CoPilot - ChatGPT o1
    [Fact]
    public void Button_BubblesUp_To_SuperView ()
    {
        View superView = new () { CanFocus = true };
        superView.CommandsToBubbleUp = [Command.Accept];

        Button button = new () { Text = "OK" };
        superView.Add (button);

        var superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        // Button (IAcceptTarget) should bubble up to SuperView
        button.InvokeCommand (Command.Accept);

        Assert.Equal (1, superViewAcceptedCount);
    }

    [Fact]
    public void IAcceptTarget_In_Deep_Hierarchy_BubblesUp ()
    {
        View root = new () { Id = "root", CanFocus = true };
        root.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView rootIsDefaultView = new () { IsDefault = true, Id = "rootIsDefaultView", CanFocus = true };

        View middle = new () { Id = "middle", CanFocus = true };
        middle.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView middleDefaultView = new () { IsDefault = true, Id = "middleDefaultView" };

        root.Add (middle);
        middle.Add (middleDefaultView);

        root.Add (rootIsDefaultView);
        root.DefaultAcceptView = rootIsDefaultView;

        var rootAcceptingCount = 0;
        root.Accepting += (_, _) => rootAcceptingCount++;

        var rootAcceptedCount = 0;
        root.Accepted += (_, _) => rootAcceptedCount++;

        var middleAcceptingCount = 0;
        middle.Accepting += (_, _) => middleAcceptingCount++;

        var middleAcceptedCount = 0;
        middle.Accepted += (_, _) => middleAcceptedCount++;

        var rootIsDefaultViewAcceptingCount = 0;
        rootIsDefaultView.Accepting += (_, _) => rootIsDefaultViewAcceptingCount++;

        var rootIsDefaultViewAcceptedCount = 0;
        rootIsDefaultView.Accepted += (_, _) => rootIsDefaultViewAcceptedCount++;

        var middleDefaultViewAcceptingCount = 0;
        middleDefaultView.Accepting += (_, _) => middleDefaultViewAcceptingCount++;

        var middleDefaultViewAcceptedCount = 0;
        middleDefaultView.Accepted += (_, _) => middleDefaultViewAcceptedCount++;

        middleDefaultView.InvokeCommand (Command.Accept);

        Assert.Equal (1, middleAcceptingCount); // 1 because Accept bubbled up from middleDefaultView
        Assert.Equal (0, middleAcceptedCount); // 0 because Accept bubbled up to root, so middle's DefaultAcceptHandler returned early
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleDefaultView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleDefaultView is an IAcceptTarget and should handle Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because Accept bubbled up, so middleDefaultView's DefaultAcceptHandler returned early
        Assert.Equal (1, rootIsDefaultViewAcceptingCount); // 1 because root's DefaultAcceptView is invoked via DispatchDown
        Assert.Equal (1, rootIsDefaultViewAcceptedCount); // 1 because root's DefaultAcceptView receives Accepted after DispatchDown
    }

    [Fact]
    public void IAcceptTarget_In_Deep_Hierarchy_BubblesUp2 ()
    {
        View root = new () { Id = "root", CanFocus = true };
        root.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView rootIsDefaultView = new () { IsDefault = true, Id = "rootIsDefaultView", CanFocus = true };

        View middle = new () { Id = "middle", CanFocus = true };
        middle.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView middleView = new () { IsDefault = false, Id = "middleView" };

        root.Add (middle);
        middle.Add (middleView);

        root.Add (rootIsDefaultView);
        root.DefaultAcceptView = rootIsDefaultView;

        var rootAcceptingCount = 0;
        root.Accepting += (_, _) => rootAcceptingCount++;

        var rootAcceptedCount = 0;
        root.Accepted += (_, _) => rootAcceptedCount++;

        var middleAcceptingCount = 0;
        middle.Accepting += (_, _) => middleAcceptingCount++;

        var middleAcceptedCount = 0;
        middle.Accepted += (_, _) => middleAcceptedCount++;

        var rootIsDefaultViewAcceptingCount = 0;
        rootIsDefaultView.Accepting += (_, _) => rootIsDefaultViewAcceptingCount++;

        var rootIsDefaultViewAcceptedCount = 0;
        rootIsDefaultView.Accepted += (_, _) => rootIsDefaultViewAcceptedCount++;

        var middleDefaultViewAcceptingCount = 0;
        middleView.Accepting += (_, _) => middleDefaultViewAcceptingCount++;

        var middleDefaultViewAcceptedCount = 0;
        middleView.Accepted += (_, _) => middleDefaultViewAcceptedCount++;

        middleView.InvokeCommand (Command.Accept);

        Assert.Equal (1, middleAcceptingCount); // 1 because Accept bubbled up from middleView
        Assert.Equal (0, middleAcceptedCount); // 0 because Accept bubbled up to root, so middle's DefaultAcceptHandler returned early
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleView (non-default IAcceptTarget) fires Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because Accept bubbled up, so middleView's DefaultAcceptHandler returned early
        Assert.Equal (1, rootIsDefaultViewAcceptingCount); // 1 because root's DefaultAcceptView is invoked via DispatchDown
        Assert.Equal (1, rootIsDefaultViewAcceptedCount); // 1 because root's DefaultAcceptView receives Accepted after DispatchDown
    }

    [Fact]
    public void IAcceptTarget_In_Deep_Hierarchy_BubblesUp3 ()
    {
        View root = new () { Id = "root", CanFocus = true };
        root.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView rootIsDefaultView = new () { IsDefault = true, Id = "rootIsDefaultView", CanFocus = true };

        View middle = new () { Id = "middle", CanFocus = true };
        middle.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        View middleView = new () { Id = "middleView" };

        root.Add (middle);
        middle.Add (middleView);

        root.Add (rootIsDefaultView);
        root.DefaultAcceptView = rootIsDefaultView;

        var rootAcceptingCount = 0;
        root.Accepting += (_, _) => rootAcceptingCount++;

        var rootAcceptedCount = 0;
        root.Accepted += (_, _) => rootAcceptedCount++;

        var middleAcceptingCount = 0;
        middle.Accepting += (_, _) => middleAcceptingCount++;

        var middleAcceptedCount = 0;
        middle.Accepted += (_, _) => middleAcceptedCount++;

        var rootIsDefaultViewAcceptingCount = 0;
        rootIsDefaultView.Accepting += (_, _) => rootIsDefaultViewAcceptingCount++;

        var rootIsDefaultViewAcceptedCount = 0;
        rootIsDefaultView.Accepted += (_, _) => rootIsDefaultViewAcceptedCount++;

        var middleDefaultViewAcceptingCount = 0;
        middleView.Accepting += (_, _) => middleDefaultViewAcceptingCount++;

        var middleDefaultViewAcceptedCount = 0;
        middleView.Accepted += (_, _) => middleDefaultViewAcceptedCount++;

        middleView.InvokeCommand (Command.Accept);

        Assert.Equal (1, middleAcceptingCount); // 1 because Accept bubbled up from middleView
        Assert.Equal (0, middleAcceptedCount); // 0 because Accept bubbled up to root, so middle's DefaultAcceptHandler returned early
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleView (plain View) fires Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because Accept bubbled up, so middleView's DefaultAcceptHandler returned early
        Assert.Equal (1, rootIsDefaultViewAcceptingCount); // 1 because root's DefaultAcceptView is invoked via DispatchDown
        Assert.Equal (1, rootIsDefaultViewAcceptedCount); // 1 because root's DefaultAcceptView receives Accepted after DispatchDown
    }

    // CoPilot - ChatGPT o1
    [Fact]
    public void IAcceptTarget_Handled_Does_Not_BubbleUp ()
    {
        View superView = new () { CanFocus = true };
        AcceptTargetTestView acceptTarget = new () { Id = "acceptTarget" };

        superView.Add (acceptTarget);

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        // Handle the Accepting event on the acceptTarget
        acceptTarget.Accepting += (_, e) => e.Handled = true;

        acceptTarget.InvokeCommand (Command.Accept);

        // Should not bubble up because it was handled
        Assert.Equal (0, superViewAcceptingCount);
    }

    // CoPilot - ChatGPT o1
    [Fact]
    public void NonIAcceptTarget_Handled_Does_Not_Redirect ()
    {
        View superView = new () { CanFocus = true };
        View nonAcceptTarget = new () { Id = "nonAcceptTarget", CanFocus = true };
        View defaultAcceptView = new () { Id = "defaultAcceptView", CanFocus = true };

        superView.Add (nonAcceptTarget);
        superView.Add (defaultAcceptView);
        superView.DefaultAcceptView = defaultAcceptView;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        // Handle the Accepting event on the nonAcceptTarget
        nonAcceptTarget.Accepting += (_, e) => e.Handled = true;

        nonAcceptTarget.InvokeCommand (Command.Accept);

        // Should not redirect because it was handled
        Assert.Equal (0, defaultAcceptViewAcceptingCount);
    }

    #endregion

    #region DispatchDown Tests

    // Claude - Opus 4.6
    /// <summary>
    ///     Exposes the protected <see cref="View.DispatchDown"/> method for testing.
    /// </summary>
    private class BubbleDownTestView : View
    {
        public bool? TestBubbleDown (View target, ICommandContext? ctx) => DispatchDown (target, ctx);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_InvokesCommandOnTarget ()
    {
        BubbleDownTestView superView = new ();
        ViewEventTester target = new ();
        superView.Add (target);

        CommandContext ctx = new (Command.Activate, new WeakReference<View> (superView), null);

        superView.TestBubbleDown (target, ctx);

        Assert.Equal (1, target.OnActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_SetsIsBubblingDown_True ()
    {
        BubbleDownTestView superView = new ();
        View target = new ();
        superView.Add (target);

        ICommandContext? receivedCtx = null;
        target.Activating += (_, e) => receivedCtx = e.Context;

        CommandContext ctx = new (Command.Activate, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, ctx);

        Assert.NotNull (receivedCtx);
        Assert.Equal (CommandRouting.DispatchingDown, receivedCtx!.Routing);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_PreservesBinding ()
    {
        BubbleDownTestView superView = new ();
        View target = new ();
        superView.Add (target);

        ICommandContext? receivedCtx = null;
        target.Activating += (_, e) => receivedCtx = e.Context;

        KeyBinding originalBinding = new ([Command.Activate]) { Key = Key.Space };
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (superView), originalBinding);
        superView.TestBubbleDown (target, ctx);

        Assert.NotNull (receivedCtx);
        Assert.NotNull (receivedCtx!.Binding);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_PreservesSource ()
    {
        BubbleDownTestView superView = new () { Id = "superView" };
        View target = new () { Id = "target" };
        superView.Add (target);

        ICommandContext? receivedCtx = null;
        target.Activating += (_, e) => receivedCtx = e.Context;

        WeakReference<View> originalSource = new (superView);
        CommandContext ctx = new (Command.Activate, originalSource, null);
        superView.TestBubbleDown (target, ctx);

        Assert.NotNull (receivedCtx);
        View? source = null;
        Assert.True (receivedCtx!.Source?.TryGetTarget (out source));
        Assert.Same (superView, source);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_PreservesCommand ()
    {
        BubbleDownTestView superView = new ();
        ViewEventTester target = new ();
        superView.Add (target);

        CommandContext ctx = new (Command.Accept, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, ctx);

        Assert.Equal (1, target.OnAcceptedCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_UsesNotBound_WhenCtxIsNull ()
    {
        BubbleDownTestView superView = new ();
        ViewEventTester target = new ();
        superView.Add (target);

        superView.TestBubbleDown (target, null);

        // NotBound command should fire CommandNotBound
        Assert.Equal (1, target.OnCommandNotBoundCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_Target_DoesNotBubbleUp ()
    {
        BubbleDownTestView superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Activate];

        View target = new () { Id = "target" };
        superView.Add (target);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        CommandContext ctx = new (Command.Activate, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, ctx);

        // The target's Activate must NOT bubble back up to superView
        Assert.Equal (0, superViewActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_Target_DoesNotBubbleUp_Accept ()
    {
        BubbleDownTestView superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Accept];

        Button defaultButton = new () { IsDefault = true, Id = "defaultButton" };
        View target = new () { Id = "target" };
        superView.Add (target);
        superView.Add (defaultButton);

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        var defaultButtonAcceptingCount = 0;
        defaultButton.Accepting += (_, _) => defaultButtonAcceptingCount++;

        CommandContext ctx = new (Command.Accept, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, ctx);

        // Neither superView Accepting nor DefaultAcceptView should fire
        Assert.Equal (0, superViewAcceptingCount);
        Assert.Equal (0, defaultButtonAcceptingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_Target_DoesNotBubbleUp_DeepHierarchy ()
    {
        BubbleDownTestView root = new () { Id = "root" };
        root.CommandsToBubbleUp = [Command.Activate];

        View middle = new () { Id = "middle" };
        middle.CommandsToBubbleUp = [Command.Activate];
        root.Add (middle);

        View leaf = new () { Id = "leaf" };
        middle.Add (leaf);

        var rootActivatingCount = 0;
        root.Activating += (_, _) => rootActivatingCount++;

        var middleActivatingCount = 0;
        middle.Activating += (_, _) => middleActivatingCount++;

        // DispatchDown from root to leaf — should not bubble to middle or root
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (root), null);
        root.TestBubbleDown (leaf, ctx);

        Assert.Equal (0, middleActivatingCount);
        Assert.Equal (0, rootActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void CommandsToBubbleUp_SkipsWhenIsBubblingDown ()
    {
        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Activate];

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Invoke Activate on subView with Routing = DispatchingDown
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (subView), null) { Routing = CommandRouting.DispatchingDown };
        subView.InvokeCommand (Command.Activate, ctx);

        // SuperView should NOT receive the event
        Assert.Equal (0, superViewActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_Then_NormalInvoke_BubblesNormally ()
    {
        BubbleDownTestView superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Activate];

        View target = new () { Id = "target" };
        superView.Add (target);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // First: DispatchDown — should NOT bubble
        CommandContext downCtx = new (Command.Activate, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, downCtx);
        Assert.Equal (0, superViewActivatingCount);

        // Second: Normal invoke — SHOULD bubble
        target.InvokeCommand (Command.Activate);
        Assert.Equal (1, superViewActivatingCount);
    }

    /// <summary>
    ///     Regression test: When <see cref="Command.Accept"/> is invoked directly on a view that has a
    ///     <see cref="View.DefaultAcceptView"/>, the DefaultAcceptView's <see cref="View.Accepting"/> should fire,
    ///     and the original view's <see cref="View.Accepted"/> should also fire afterward.
    ///     This replicates the failure in DialogTests.GenericString_Command_Accept_BubblesUp where
    ///     <c>dialog.InvokeCommand(Command.Accept)</c> did not cause the default button's Accepting to fire
    ///     (okAcceptingFired was 0 instead of 1) and the dialog's Accepted event never fired
    ///     (dialogAcceptedFired was 0 instead of 1).
    /// </summary>
    [Fact]
    public void Accept_Direct_On_View_With_DefaultAcceptView_Fires_DefaultAcceptView_Accepting_And_View_Accepted ()
    {
        // Arrange: A superView with CommandsToBubbleUp and a DefaultAcceptView (IAcceptTarget with IsDefault = true)
        View superView = new () { Id = "superView", CanFocus = true };
        superView.CommandsToBubbleUp = [Command.Accept];

        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };
        superView.Add (defaultAcceptView);
        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        var superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        // Act: Invoke Accept directly on the superView (not on a subview)
        superView.InvokeCommand (Command.Accept);

        // Assert: The DefaultAcceptView's Accepting should have fired
        Assert.Equal (1, defaultAcceptViewAcceptingCount);

        // Assert: The superView's Accepting should have fired
        Assert.Equal (1, superViewAcceptingCount);

        // Assert: The superView's Accepted should have fired after the DefaultAcceptView handled Accept
        Assert.Equal (1, superViewAcceptedCount);
    }

    #endregion

    public class ViewEventTester : View
    {
        public ViewEventTester ()
        {
            Id = "viewEventTester";
            CanFocus = true;

            Accepting += (_, a) =>
                         {
                             a.Handled = HandleAccepted;
                             AcceptedCount++;
                         };

            HandlingHotKey += (_, a) =>
                              {
                                  a.Handled = HandleHandlingHotKey;
                                  HandlingHotKeyCount++;
                              };

            Activating += (_, a) =>
                          {
                              a.Handled = HandleActivating;
                              ActivatingCount++;
                          };

            CommandNotBound += (_, a) =>
                               {
                                   a.Handled = HandleCommandNotBound;
                                   CommandNotBoundCount++;
                               };
        }

        public int OnAcceptedCount { get; set; }
        public int AcceptedCount { get; set; }
        public bool HandleOnAccepted { get; set; }

        /// <inheritdoc/>
        protected override bool OnAccepting (CommandEventArgs args)
        {
            OnAcceptedCount++;

            return HandleOnAccepted;
        }

        public bool HandleAccepted { get; set; }

        public int OnHandlingHotKeyCount { get; set; }
        public int HandlingHotKeyCount { get; set; }
        public bool HandleOnHandlingHotKey { get; set; }

        /// <inheritdoc/>
        protected override bool OnHandlingHotKey (CommandEventArgs args)
        {
            OnHandlingHotKeyCount++;

            return HandleOnHandlingHotKey;
        }

        public bool HandleHandlingHotKey { get; set; }

        public int OnActivatingCount { get; set; }
        public int ActivatingCount { get; set; }
        public bool HandleOnActivating { get; set; }
        public bool HandleActivating { get; set; }

        /// <inheritdoc/>
        protected override bool OnActivating (CommandEventArgs args)
        {
            OnActivatingCount++;

            return HandleOnActivating;
        }

        public int OnCommandNotBoundCount { get; set; }
        public int CommandNotBoundCount { get; set; }

        public bool HandleOnCommandNotBound { get; set; }

        public bool HandleCommandNotBound { get; set; }

        protected override bool OnCommandNotBound (CommandEventArgs args)
        {
            OnCommandNotBoundCount++;

            return HandleOnCommandNotBound;
        }
    }

    #region IValue Integration Tests

    // Claude - Opus 4.5
    [Fact]
    public void InvokeCommand_WithIValueSource_PopulatesValue ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            TestValueView view = new () { Id = "valueView", Value = "test value" };
            ICommandContext? capturedContext = null;
            var acceptingCount = 0;

            view.Accepting += (_, args) =>
                              {
                                  acceptingCount++;
                                  capturedContext = args.Context;
                              };

            view.InvokeCommand (Command.Accept);

            Assert.Equal (1, acceptingCount);
            Assert.NotNull (capturedContext);
            Assert.Equal ("test value", capturedContext!.Value);
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void InvokeCommand_WithoutIValueSource_ValueIsNull ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            View view = new () { Id = "plainView" };
            ICommandContext? capturedContext = null;
            var acceptingCount = 0;

            view.Accepting += (_, args) =>
                              {
                                  acceptingCount++;
                                  capturedContext = args.Context;
                              };

            view.InvokeCommand (Command.Accept);

            Assert.Equal (1, acceptingCount);
            Assert.NotNull (capturedContext);
            Assert.Null (capturedContext!.Value);
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void InvokeCommand_WithBinding_PopulatesValueFromIValue ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            TestValueView view = new () { Id = "valueView", Value = 42 };
            ICommandContext? capturedContext = null;
            var acceptingCount = 0;

            view.Accepting += (_, args) =>
                              {
                                  acceptingCount++;
                                  capturedContext = args.Context;
                              };

            KeyBinding binding = new ([Command.Accept]) { Key = Key.Enter };
            view.InvokeCommand (Command.Accept, binding);

            Assert.Equal (1, acceptingCount);
            Assert.NotNull (capturedContext);
            Assert.Equal (42, capturedContext!.Value);
            Assert.NotNull (capturedContext.Binding);
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void InvokeCommand_IValueReturnsNull_ValueIsNull ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            TestValueView view = new () { Id = "nullValueView", Value = null };
            ICommandContext? capturedContext = null;
            var acceptingCount = 0;

            view.Accepting += (_, args) =>
                              {
                                  acceptingCount++;
                                  capturedContext = args.Context;
                              };

            view.InvokeCommand (Command.Accept);

            Assert.Equal (1, acceptingCount);
            Assert.NotNull (capturedContext);
            Assert.Null (capturedContext!.Value);
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void CommandBridge_PreservesValue_OnAccept ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            View owner = new () { Id = "owner" };
            TestValueView remote = new () { Id = "remote", Value = "bridged value" };
            ICommandContext? capturedContext = null;
            var acceptingCount = 0;

            owner.Accepting += (_, args) =>
                               {
                                   acceptingCount++;
                                   capturedContext = args.Context;
                               };

            using CommandBridge bridge = CommandBridge.Connect (owner, remote, Command.Accept);

            // Invoke Accept on remote
            remote.InvokeCommand (Command.Accept);

            Assert.Equal (1, acceptingCount);
            Assert.NotNull (capturedContext);
            Assert.Equal ("bridged value", capturedContext!.Value);
            Assert.Equal (CommandRouting.Bridged, capturedContext.Routing);
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void CommandBridge_PreservesValue_OnActivate ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            View owner = new () { Id = "owner" };
            TestValueView remote = new () { Id = "remote", Value = 123 };
            ICommandContext? capturedContext = null;
            var activatedCount = 0;

            owner.Activated += (_, args) =>
                               {
                                   activatedCount++;
                                   capturedContext = args.Value;
                               };

            using CommandBridge bridge = CommandBridge.Connect (owner, remote, Command.Activate);

            // Invoke Activate on remote
            remote.InvokeCommand (Command.Activate);

            Assert.Equal (1, activatedCount);
            Assert.NotNull (capturedContext);
            Assert.Equal (123, capturedContext!.Value);
            Assert.Equal (CommandRouting.Bridged, capturedContext.Routing);
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void CommandBridge_NullValue_PropagatesCorrectly ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            View owner = new () { Id = "owner" };
            TestValueView remote = new () { Id = "remote", Value = null };
            ICommandContext? capturedContext = null;
            var acceptingCount = 0;

            owner.Accepting += (_, args) =>
                               {
                                   acceptingCount++;
                                   capturedContext = args.Context;
                               };

            using CommandBridge bridge = CommandBridge.Connect (owner, remote, Command.Accept);

            // Invoke Accept on remote
            remote.InvokeCommand (Command.Accept);

            Assert.Equal (1, acceptingCount);
            Assert.NotNull (capturedContext);
            Assert.Null (capturedContext!.Value);
            Assert.Equal (CommandRouting.Bridged, capturedContext.Routing);
        }
    }

    // Test view that implements IValue<object?>
    private class TestValueView : View, IValue<object?>
    {
        public object? Value
        {
            get;
            set
            {
                object? old = field;
                ValueChanging?.Invoke (this, new ValueChangingEventArgs<object?> (old, value));
                field = value;
                ValueChanged?.Invoke (this, new ValueChangedEventArgs<object?> (old, value));
                ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (old, value));
            }
        }

        public event EventHandler<ValueChangingEventArgs<object?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;
    }

    /// <summary>
    ///     A view that implements <see cref="IValue{T}"/> and increments its value in
    ///     <see cref="OnActivated"/>, similar to how <see cref="CheckBox"/> advances its
    ///     <see cref="CheckState"/>. Used to test the command pipeline independently of CheckBox.
    /// </summary>
    private class ToggleView : View, IValue<int>
    {
        /// <summary>Gets the number of times <see cref="OnActivated"/> has been called.</summary>
        public int ActivatedCount { get; private set; }

        public int Value
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                int old = field;
                ValueChanging?.Invoke (this, new ValueChangingEventArgs<int> (old, value));
                field = value;
                ValueChanged?.Invoke (this, new ValueChangedEventArgs<int> (old, value));
                ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (old, value));
            }
        }

        public event EventHandler<ValueChangingEventArgs<int>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<int>>? ValueChanged;

        private event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

        event EventHandler<ValueChangedEventArgs<object?>>? IValue.ValueChangedUntyped
        {
            add => ValueChangedUntyped += value;
            remove => ValueChangedUntyped -= value;
        }

        /// <inheritdoc/>
        protected override void OnActivated (ICommandContext? commandContext)
        {
            base.OnActivated (commandContext);
            ActivatedCount++;
            Value++;
        }
    }

    /// <summary>
    ///     A minimal composite view that relays <see cref="Command.Activate"/> to a dispatch
    ///     target (its first SubView), replicating the containment pattern of
    ///     <see cref="Shortcut"/>/<see cref="MenuItem"/> without depending on those classes.
    /// </summary>
    private class RelayComposite : View
    {
        public RelayComposite () => CommandsToBubbleUp = [Command.Activate];

        protected override View? GetDispatchTarget (ICommandContext? ctx) => SubViews.FirstOrDefault ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Proves the double-activation bug: when a view that mutates state in
    ///     <see cref="View.OnActivated"/> is the dispatch target of a relay composite, and
    ///     activation starts on the target itself (e.g., mouse click), the command bubbles up
    ///     to the composite which dispatches back down. This causes
    ///     <see cref="View.OnActivated"/> to fire twice: once from the inner DispatchDown,
    ///     and again from the originator's own <c>RaiseActivated</c>. The state mutation
    ///     (Value++) therefore happens twice instead of once.
    /// </summary>
    [Fact]
    public void OnActivated_Fires_Once_When_Originator_Is_DispatchTarget ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            // Arrange: RelayComposite contains ToggleView as dispatch target
            ToggleView toggleView = new () { Id = "toggleView" };

            RelayComposite composite = new () { Id = "composite" };
            composite.Add (toggleView);

            var compositeActivatedCount = 0;

            composite.Activated += (_, _) => compositeActivatedCount++;

            Assert.Equal (0, toggleView.Value);
            Assert.Equal (0, toggleView.ActivatedCount);

            // Act: Invoke Activate on the dispatch target itself (simulates a mouse click).
            // The binding is required so TryDispatchToTarget's relay guard passes.
            KeyBinding binding = new ([Command.Activate], Key.Space, composite);
            CommandContext ctx = new (Command.Activate, new WeakReference<View> (toggleView), binding);
            toggleView.InvokeCommand (Command.Activate, ctx);

            // Assert: OnActivated should fire exactly ONCE, so Value should be 1.
            Assert.Equal (1, toggleView.ActivatedCount);
            Assert.Equal (1, toggleView.Value);
            Assert.Equal (1, compositeActivatedCount);
        }
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     When a ToggleView is the dispatch target of a RelayComposite, and a
    ///     <see cref="CommandBridge"/> connects the composite's Activated event to a host view,
    ///     the host should see the post-toggle value and exactly one Activated event.
    ///     Direct invocation (no binding) — the toggle fires once, but the value in the bridged
    ///     context is stale (captured before OnActivated mutated it).
    ///     Replicates <c>Target_CheckBox_CommandView_Activate_Direct_Source_Reaches_Target_And_Value_Is_Correct</c>
    ///     without depending on PopoverMenu, MenuItem, or CheckBox.
    /// </summary>
    [Fact]
    public void Bridge_Receives_Correct_Value_When_Originator_Is_DispatchTarget_Direct ()
    {
        using IDisposable verbose = TestLogging.Verbose (output);

        Trace.EnabledCategories = TraceCategory.Command;

        // Arrange: Host ← Bridge ← Composite → ToggleView (dispatch target)
        ToggleView toggleView = new () { Id = "toggleView" };

        RelayComposite composite = new () { Id = "composite" };
        composite.Add (toggleView);

        View host = new () { Id = "host" };

        // Bridge: composite.Activated → host.InvokeCommand(Activate, Bridged)
        using CommandBridge bridge = CommandBridge.Connect (host, composite, Command.Activate);

        object? capturedValue = null;
        var hostActivatedCount = 0;
        var valueChangeCount = 0;

        toggleView.ValueChanged += (_, _) => valueChangeCount++;

        host.Activated += (_, args) =>
                          {
                              hostActivatedCount++;
                              capturedValue = args.Value?.Value;
                          };

        Assert.Equal (0, toggleView.Value);

        // Act: Direct invocation on the toggle view (no binding).
        toggleView.InvokeCommand (Command.Activate);

        // Assert: The toggle should happen exactly once (no double-fire in the direct path).
        Assert.Equal (1, toggleView.ActivatedCount);
        Assert.Equal (1, toggleView.Value);
        Assert.Equal (1, valueChangeCount);

        // Assert: Host's Activated event should fire exactly once.
        Assert.Equal (1, hostActivatedCount);

        // The value at the host should be the post-toggle value (1).
        // level doesn't re-read the dispatch target's value after the originator's
        // OnActivated has mutated it.
        Assert.Equal (1, capturedValue as int?);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Same as <see cref="Bridge_Receives_Correct_Value_When_Originator_Is_DispatchTarget_Direct"/>
    ///     but with a <see cref="KeyBinding"/> whose source is the composite (simulates key activation
    ///     that bubbles from the toggle view up to the composite).
    ///     The binding enables TryDispatchToTarget's relay guard, causing the composite to
    ///     DispatchDown back to the toggle view — triggering the double-fire bug.
    ///     Replicates <c>Target_CheckBox_CommandView_Activate_With_KeyBinding</c> without depending on
    ///     PopoverMenu, MenuItem, or CheckBox.
    /// </summary>
    [Fact]
    public void Bridge_Receives_Correct_Value_When_Originator_Is_DispatchTarget_WithBinding ()
    {
        using IDisposable verbose = TestLogging.Verbose (output);

        Trace.EnabledCategories = TraceCategory.Command;

        // Arrange: Host ← Bridge ← Composite → ToggleView (dispatch target)
        ToggleView toggleView = new () { Id = "toggleView" };

        RelayComposite composite = new () { Id = "composite" };
        composite.Add (toggleView);

        View host = new () { Id = "host" };

        using CommandBridge bridge = CommandBridge.Connect (host, composite, Command.Activate);

        object? capturedValue = null;
        var hostActivatedCount = 0;
        var valueChangeCount = 0;

        toggleView.ValueChanged += (_, _) => valueChangeCount++;

        host.Activated += (_, args) =>
                          {
                              hostActivatedCount++;
                              capturedValue = args.Value?.Value;
                          };

        Assert.Equal (0, toggleView.Value);

        // Act: Invoke with a binding whose source is the composite.
        KeyBinding binding = new ([Command.Activate], Key.Space, composite);
        toggleView.InvokeCommand (Command.Activate, binding);

        // Assert: OnActivated should fire exactly once, so Value should be 1.
        Assert.Equal (1, toggleView.ActivatedCount);
        Assert.Equal (1, toggleView.Value);
        Assert.Equal (1, valueChangeCount);

        // Assert: Host's Activated event should fire exactly once.
        Assert.Equal (1, hostActivatedCount);

        // The value at the host should be the post-toggle value (1).
        Assert.Equal (1, capturedValue as int?);
    }

    #endregion

    #region Values Chain Tests (Option B)

    /// <summary>
    ///     A ConsumeDispatch composite that implements <see cref="IValue{T}"/> and updates its own
    ///     value in <see cref="OnActivated"/>. Replicates the OptionSelector/FlagSelector pattern
    ///     without depending on those classes.
    /// </summary>
    private class CompositeValueView : View, IValue<int?>
    {
        public CompositeValueView () => CommandsToBubbleUp = [Command.Activate];

        public int? Value
        {
            get;
            set
            {
                int? old = field;
                ValueChanging?.Invoke (this, new ValueChangingEventArgs<int?> (old, value));
                field = value;
                ValueChanged?.Invoke (this, new ValueChangedEventArgs<int?> (old, value));
                _valueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (old, value));
            }
        }

        /// <inheritdoc/>
        protected override bool ConsumeDispatch => true;

        /// <inheritdoc/>
        protected override View? GetDispatchTarget (ICommandContext? ctx) => SubViews.FirstOrDefault ();

        /// <inheritdoc/>
        protected override void OnActivated (ICommandContext? ctx)
        {
            base.OnActivated (ctx);

            // Simulate what OptionSelector.ApplyActivation does: update own value after base fires.
            Value = 42;
        }

        public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;

        private event EventHandler<ValueChangedEventArgs<object?>>? _valueChangedUntyped;

        event EventHandler<ValueChangedEventArgs<object?>>? IValue.ValueChangedUntyped
        {
            add => _valueChangedUntyped += value;
            remove => _valueChangedUntyped -= value;
        }
    }

    // Claude - Sonnet 4.6
    /// <summary>
    ///     When a ConsumeDispatch composite implements <see cref="IValue"/>, <see cref="ICommandContext.Value"/>
    ///     delivered to direct <see cref="View.Activated"/> subscribers must be the composite's
    ///     post-mutation value (appended to <see cref="ICommandContext.Values"/>), not the
    ///     dispatch target's raw value.
    /// </summary>
    [Fact]
    public void Values_ConsumeDispatch_Composite_Appends_Own_Value ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            ToggleView toggleView = new () { Id = "toggleView" };
            CompositeValueView composite = new () { Id = "composite" };
            composite.Add (toggleView);

            object? capturedValue = null;
            IReadOnlyList<object?>? capturedValues = null;

            composite.Activated += (_, args) =>
                                   {
                                       capturedValue = args.Value?.Value;
                                       capturedValues = args.Value?.Values;
                                   };

            // Act: programmatic invocation dispatches to the focused ToggleView.
            composite.InvokeCommand (Command.Activate);

            // Assert: ctx.Value is the composite's int? (42), not CheckState/int from ToggleView.
            Assert.Equal (42, capturedValue as int?);
            Assert.NotNull (capturedValues);

            // The chain should contain the composite's initial value, the dispatch target's
            // value, and the composite's post-mutation value.
            Assert.True (capturedValues!.Count >= 2);
            Assert.Equal (42, capturedValues [^1] as int?);

            composite.Dispose ();
        }
    }

    // Claude - Sonnet 4.6
    /// <summary>
    ///     When a ConsumeDispatch composite implements <see cref="IValue"/>,
    ///     ancestors receiving <see cref="View.Activated"/> via <see cref="View.CommandsToBubbleUp"/>
    ///     must see the composite's post-mutation value in <see cref="ICommandContext.Value"/>.
    /// </summary>
    [Fact]
    public void Values_BubbleActivatedUp_Carries_Composite_Value_To_Ancestor ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            ToggleView toggleView = new () { Id = "toggleView" };
            CompositeValueView composite = new () { Id = "composite" };
            composite.Add (toggleView);

            View ancestor = new () { Id = "ancestor" };
            ancestor.CommandsToBubbleUp = [Command.Activate];
            ancestor.Add (composite);

            object? ancestorCapturedValue = null;

            ancestor.Activated += (_, args) => { ancestorCapturedValue = args.Value?.Value; };

            // Act
            composite.InvokeCommand (Command.Activate);

            // Assert: ancestor sees the composite's value (42), not ToggleView's value.
            Assert.Equal (42, ancestorCapturedValue as int?);

            ancestor.Dispose ();
        }
    }

    // Claude - Sonnet 4.6
    /// <summary>
    ///     <see cref="ICommandContext.Values"/> accumulates values as the command propagates.
    ///     The initial value from the source is the first entry; subsequent composites append.
    /// </summary>
    [Fact]
    public void Values_Chain_Accumulates_From_Source_Through_Composites ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            TestValueView sourceView = new () { Id = "source", Value = "initial" };

            IReadOnlyList<object?>? capturedValues = null;

            sourceView.Activated += (_, args) => { capturedValues = args.Value?.Values; };

            // Act: simple activation on an IValue view
            sourceView.InvokeCommand (Command.Activate);

            // Assert: Values contains the source view's value
            Assert.NotNull (capturedValues);
            Assert.Contains ("initial", capturedValues!);

            sourceView.Dispose ();
        }
    }

    // Claude - Sonnet 4.6
    /// <summary>
    ///     When a <see cref="CommandBridge"/> bridges an Activated event,
    ///     <see cref="ICommandContext.Values"/> is preserved across the bridge,
    ///     including all accumulated values from the originating chain.
    /// </summary>
    [Fact]
    public void Values_Bridge_Preserves_Full_Chain ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            ToggleView toggleView = new () { Id = "toggleView" };
            CompositeValueView composite = new () { Id = "composite" };
            composite.Add (toggleView);

            View host = new () { Id = "host" };
            using CommandBridge bridge = CommandBridge.Connect (host, composite, Command.Activate);

            IReadOnlyList<object?>? hostCapturedValues = null;
            object? hostCapturedValue = null;

            host.Activated += (_, args) =>
                              {
                                  hostCapturedValues = args.Value?.Values;
                                  hostCapturedValue = args.Value?.Value;
                              };

            // Act
            composite.InvokeCommand (Command.Activate);

            // Assert: Bridge carries the full Values chain, including composite's value
            Assert.NotNull (hostCapturedValues);
            Assert.True (hostCapturedValues!.Count >= 2);

            // The last value should be the composite's value (42)
            Assert.Equal (42, hostCapturedValue as int?);

            host.Dispose ();
            composite.Dispose ();
        }
    }

    // Claude - Sonnet 4.6
    /// <summary>
    ///     Non-IValue views do not add to the <see cref="ICommandContext.Values"/> chain.
    ///     Only views implementing <see cref="IValue"/> contribute values.
    /// </summary>
    [Fact]
    public void Values_NonIValue_View_Has_Empty_Values ()
    {
        using (TestLogging.Verbose (output))
        {
            Trace.EnabledCategories = TraceCategory.Command;

            View plainView = new () { Id = "plainView" };

            IReadOnlyList<object?>? capturedValues = null;

            plainView.Activated += (_, args) => { capturedValues = args.Value?.Values; };

            plainView.InvokeCommand (Command.Activate);

            // Assert: No values accumulated for non-IValue view
            Assert.NotNull (capturedValues);
            Assert.Empty (capturedValues!);

            plainView.Dispose ();
        }
    }

    #endregion

    #region Bridge Cancellation Bug (PopoverMenus.cs line 192)

    // Claude - Opus 4.6
    /// <summary>
    ///     Replicates the BUGBUG at PopoverMenus.cs line 192: when a <see cref="CommandBridge"/>
    ///     relays an <c>Activated</c> event to an owner, and the owner (or its ancestor) tries to
    ///     cancel via <c>OnActivating</c>, the originator's state has already changed because the
    ///     bridge fires from the post-event (<c>Activated</c>), not the pre-event (<c>Activating</c>).
    ///     Also verifies that a <c>BridgedCancellation</c> trace warning is emitted.
    ///
    ///     Topology (uses only View base classes):
    ///     <code>
    ///     ancestor (Activating handler cancels for specific source)
    ///       └── owner  ← Bridge ←  container (CommandsToBubbleUp=[Activate])
    ///                                  └── toggleView (IValue, mutates in OnActivated)
    ///     </code>
    ///
    ///     Expected: cancelling at the ancestor's Activating should prevent the state change.
    ///     Actual:   toggleView.Value has already incremented by the time ancestor's Activating fires.
    /// </summary>
    [Fact]
    public void Bridge_Ancestor_Cancel_OnActivating_Does_Not_Prevent_Originator_State_Change ()
    {
        ListBackend traceBackend = new ();
        using IDisposable scope = Trace.PushScope (TraceCategory.Command, traceBackend);

        // Arrange: toggleView inside container, bridged to owner, owner inside ancestor.
        ToggleView toggleView = new () { Id = "toggleView" };

        View container = new () { Id = "container" };
        container.CommandsToBubbleUp = [Command.Activate];
        container.Add (toggleView);

        View owner = new () { Id = "owner" };

        View ancestor = new () { Id = "ancestor" };
        ancestor.CommandsToBubbleUp = [Command.Activate];
        ancestor.Add (owner);

        using CommandBridge bridge = CommandBridge.Connect (owner, container, Command.Activate);

        // Track the toggleView.Value at the moment ancestor's Activating fires.
        int? valueAtAncestorActivating = null;
        var ancestorActivatingFired = false;

        ancestor.Activating += (_, args) =>
                                {
                                    ancestorActivatingFired = true;
                                    valueAtAncestorActivating = toggleView.Value;

                                    // Cancel — this should prevent further processing,
                                    // but cannot undo the toggleView's state change.
                                    args.Handled = true;
                                };

        Assert.Equal (0, toggleView.Value);

        // Act: Activate the toggleView directly (simulates a user click on the inner view).
        toggleView.InvokeCommand (Command.Activate);

        // Assert: The bridge should have caused ancestor.Activating to fire.
        Assert.True (ancestorActivatingFired, "ancestor.Activating should have fired via bridge");

        // BUGBUG: By the time ancestor's Activating handler fires, toggleView.Value
        // has already been incremented. The cancellation is too late.
        Assert.Equal (1, toggleView.Value); // State change already happened
        Assert.Equal (1, valueAtAncestorActivating); // Was already 1 when ancestor saw it

        // Verify the BridgedCancellation trace warning was emitted.
        Assert.Contains (traceBackend.Entries, e => e.Phase == "BridgedCancellation" && e.Message!.Contains ("OnActivated"));
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Contrast test: in the normal (non-bridge) bubble-up path, cancelling at the ancestor's
    ///     <c>OnActivating</c> DOES prevent the originator's state change, because <c>TryBubbleUp</c>
    ///     calls <c>SuperView.InvokeCommand</c> during <c>RaiseActivating</c> (the pre-event phase).
    ///     The originator's <c>OnActivated</c> only fires if <c>RaiseActivating</c> succeeds.
    ///     No <c>BridgedCancellation</c> trace warning should appear.
    ///
    ///     Topology:
    ///     <code>
    ///     ancestor (Activating handler cancels)
    ///       └── toggleView (IValue, mutates in OnActivated)
    ///     </code>
    ///
    ///     This proves the asymmetry: direct containment bubbling supports cancellation;
    ///     bridge-based bubbling does not.
    /// </summary>
    [Fact]
    public void Direct_Ancestor_Cancel_OnActivating_Prevents_Originator_State_Change ()
    {
        ListBackend traceBackend = new ();
        using IDisposable scope = Trace.PushScope (TraceCategory.Command, traceBackend);

        // Arrange: toggleView inside ancestor (direct containment, no bridge).
        ToggleView toggleView = new () { Id = "toggleView" };

        View ancestor = new () { Id = "ancestor" };
        ancestor.CommandsToBubbleUp = [Command.Activate];
        ancestor.Add (toggleView);

        int? valueAtAncestorActivating = null;
        var ancestorActivatingFired = false;

        ancestor.Activating += (_, args) =>
                                {
                                    ancestorActivatingFired = true;
                                    valueAtAncestorActivating = toggleView.Value;

                                    // Cancel — in the direct path, this DOES prevent
                                    // the originator's OnActivated from firing.
                                    args.Handled = true;
                                };

        Assert.Equal (0, toggleView.Value);

        // Act: Activate the toggleView directly.
        toggleView.InvokeCommand (Command.Activate);

        // Assert: ancestor.Activating should have fired via TryBubbleUp.
        Assert.True (ancestorActivatingFired, "ancestor.Activating should have fired via TryBubbleUp");

        // In the direct containment path, cancellation at the ancestor DOES work:
        // toggleView.OnActivated never fires, so Value remains 0.
        Assert.Equal (0, toggleView.Value);
        Assert.Equal (0, valueAtAncestorActivating);

        // No BridgedCancellation warning — this is a direct containment path.
        Assert.DoesNotContain (traceBackend.Entries, e => e.Phase == "BridgedCancellation");
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Accept-side analog of the bridge cancellation bug: when a <see cref="CommandBridge"/>
    ///     relays an <c>Accepted</c> event to an owner, and the owner (or its ancestor) tries to
    ///     cancel via <c>OnAccepting</c>, the originator's state has already changed because the
    ///     bridge fires from the post-event (<c>Accepted</c>).
    ///     Verifies that a <c>BridgedCancellation</c> trace warning is emitted.
    ///
    ///     Topology:
    ///     <code>
    ///     ancestor (Accepting handler cancels)
    ///       └── owner  ← Bridge(Accept) ←  acceptToggleView (mutates in OnAccepted)
    ///     </code>
    /// </summary>
    [Fact]
    public void Bridge_Ancestor_Cancel_OnAccepting_Does_Not_Prevent_Originator_State_Change ()
    {
        ListBackend traceBackend = new ();
        using IDisposable scope = Trace.PushScope (TraceCategory.Command, traceBackend);

        // Arrange: acceptToggleView bridged to owner, owner inside ancestor.
        AcceptToggleView acceptToggleView = new () { Id = "acceptToggleView" };

        View owner = new () { Id = "owner" };

        View ancestor = new () { Id = "ancestor" };
        ancestor.CommandsToBubbleUp = [Command.Accept];
        ancestor.Add (owner);

        using CommandBridge bridge = CommandBridge.Connect (owner, acceptToggleView, Command.Accept);

        int? valueAtAncestorAccepting = null;
        var ancestorAcceptingFired = false;

        ancestor.Accepting += (_, args) =>
                               {
                                   ancestorAcceptingFired = true;
                                   valueAtAncestorAccepting = acceptToggleView.AcceptedCount;

                                   // Cancel — this should prevent further processing,
                                   // but cannot undo the acceptToggleView's state change.
                                   args.Handled = true;
                               };

        Assert.Equal (0, acceptToggleView.AcceptedCount);

        // Act: Accept on the remote view.
        acceptToggleView.InvokeCommand (Command.Accept);

        // Assert: The bridge should have caused ancestor.Accepting to fire.
        Assert.True (ancestorAcceptingFired, "ancestor.Accepting should have fired via bridge");

        // By the time ancestor's Accepting handler fires, acceptToggleView.OnAccepted
        // has already been called. The cancellation is too late.
        Assert.Equal (1, acceptToggleView.AcceptedCount); // State change already happened
        Assert.Equal (1, valueAtAncestorAccepting); // Was already 1 when ancestor saw it

        // Verify the BridgedCancellation trace warning was emitted.
        Assert.Contains (traceBackend.Entries, e => e.Phase == "BridgedCancellation" && e.Message!.Contains ("OnAccepted"));
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Contrast test for Accept: in the normal (non-bridge) bubble-up path, cancelling at the
    ///     ancestor's <c>OnAccepting</c> DOES prevent the originator's state change, because
    ///     <c>TryBubbleUp</c> calls <c>SuperView.InvokeCommand</c> during <c>RaiseAccepting</c>
    ///     (the pre-event phase). No <c>BridgedCancellation</c> trace warning should appear.
    ///
    ///     Topology:
    ///     <code>
    ///     ancestor (Accepting handler cancels)
    ///       └── acceptToggleView (mutates in OnAccepted)
    ///     </code>
    /// </summary>
    [Fact]
    public void Direct_Ancestor_Cancel_OnAccepting_Prevents_Originator_State_Change ()
    {
        ListBackend traceBackend = new ();
        using IDisposable scope = Trace.PushScope (TraceCategory.Command, traceBackend);

        // Arrange: acceptToggleView inside ancestor (direct containment, no bridge).
        AcceptToggleView acceptToggleView = new () { Id = "acceptToggleView" };

        View ancestor = new () { Id = "ancestor" };
        ancestor.CommandsToBubbleUp = [Command.Accept];
        ancestor.Add (acceptToggleView);

        int? valueAtAncestorAccepting = null;
        var ancestorAcceptingFired = false;

        ancestor.Accepting += (_, args) =>
                               {
                                   ancestorAcceptingFired = true;
                                   valueAtAncestorAccepting = acceptToggleView.AcceptedCount;

                                   // Cancel — in the direct path, this DOES prevent
                                   // the originator's OnAccepted from firing.
                                   args.Handled = true;
                               };

        Assert.Equal (0, acceptToggleView.AcceptedCount);

        // Act: Accept on the view directly.
        acceptToggleView.InvokeCommand (Command.Accept);

        // Assert: ancestor.Accepting should have fired via TryBubbleUp.
        Assert.True (ancestorAcceptingFired, "ancestor.Accepting should have fired via TryBubbleUp");

        // In the direct containment path, cancellation at the ancestor DOES work:
        // acceptToggleView.OnAccepted never fires, so AcceptedCount remains 0.
        Assert.Equal (0, acceptToggleView.AcceptedCount);
        Assert.Equal (0, valueAtAncestorAccepting);

        // No BridgedCancellation warning — this is a direct containment path.
        Assert.DoesNotContain (traceBackend.Entries, e => e.Phase == "BridgedCancellation");
    }

    #endregion

    #region Bridge Cancellation Test Helpers

    /// <summary>
    ///     A minimal view that tracks Accept-side state changes.
    ///     Increments <see cref="AcceptedCount"/> in <see cref="OnAccepted"/> to
    ///     provide a trackable state change for bridge cancellation tests.
    /// </summary>
    private class AcceptToggleView : View
    {
        /// <summary>Gets the number of times <see cref="OnAccepted"/> has been called.</summary>
        public int AcceptedCount { get; private set; }

        /// <inheritdoc/>
        protected override void OnAccepted (ICommandContext? commandContext)
        {
            base.OnAccepted (commandContext);
            AcceptedCount++;
        }
    }

    #endregion
}
