namespace ViewBaseTests.Commands;

public class ViewCommandTests
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

        view.Accepting += (sender, e) => { e.Handled = false; };

        view.Accepted += (sender, e) => { acceptedInvokedCount++; };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (1, acceptedInvokedCount);
    }

    [Fact]
    public void Accepted_Not_Raised_When_Accepting_Handled ()
    {
        View view = new ();
        var acceptedInvokedCount = 0;

        view.Accepting += (sender, e) => { e.Handled = true; };

        view.Accepted += (sender, e) => { acceptedInvokedCount++; };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (0, acceptedInvokedCount);
    }

    [Fact]
    public void Accepted_Event_Cannot_Be_Cancelled ()
    {
        View view = new ();
        var acceptedInvokedCount = 0;

        view.Accepted += (sender, e) =>
                         {
                             acceptedInvokedCount++;

                             // Accepted event has Handled property but it doesn't affect flow
                             e.Handled = false;
                         };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (1, acceptedInvokedCount);
    }

    [Fact]
    public void OnAccepted_Called_When_Accepting_Not_Handled ()
    {
        OnAcceptedTestView view = new ();

        view.Accepting += (sender, e) => { e.Handled = false; };

        view.InvokeCommand (Command.Accept);
        Assert.Equal (1, view.OnAcceptedCallCount);
    }

    [Fact]
    public void OnAccepted_Not_Called_When_Accepting_Handled ()
    {
        OnAcceptedTestView view = new ();

        view.Accepting += (sender, e) => { e.Handled = true; };

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

        bool? ret = view.InvokeCommand (Command.Activate);
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

    // Claude - Sonnet 4.5
    [Fact]
    public void TryBubbleToSuperView_StopsWhenHandled ()
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
    public void TryBubbleToSuperView_WorksInDeepHierarchy ()
    {
        View grandSuperView = new () { CommandsToBubbleUp = [Command.Accept] };
        View superView = new () { CommandsToBubbleUp = [Command.Accept] };
        View subView = new ();

        grandSuperView.Add (superView);
        superView.Add (subView);

        var grandSuperViewAcceptingCalledCount = 0;
        grandSuperView.Accepting += (_, _) => grandSuperViewAcceptingCalledCount++;

        subView.InvokeCommand (Command.Accept);

        // Should propagate all the way up
        Assert.Equal (1, grandSuperViewAcceptingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void TryBubbleToSuperView_StopsAtIntermediateHandler ()
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
    public void InvokeCommands_Returns_True_If_No_Command_Handled ()
    {
        var view = new ViewEventTester ();

        bool? result = view.InvokeCommands ([Command.Activate, Command.Accept], null);

        Assert.True (result);
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
        Assert.Equal (0, defaultAcceptViewAcceptingCount);
        Assert.Equal (0, defaultAcceptViewAcceptedCount);
    }

    [Fact]
    public void DefaultAcceptView_Peer_Accept_DoesNotForward_To_DefaultAcceptView ()
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
        Assert.Equal (0, defaultAcceptViewAcceptingCount);
        Assert.Equal (0, defaultAcceptViewAcceptedCount);
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
    public void NonIAcceptTarget_Redirects_To_DefaultAcceptView ()
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

        Assert.Equal (1, middleAcceptingCount); // 1 because of DefaultAcceptView
        Assert.Equal (0, middleAcceptedCount); // 0 because middleDefaultView is an IAcceptTarget.IsDefault caused the command to be handled
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleDefaultView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleDefaultView is an IAcceptTarget and should handle Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because middleDefaultView is an IAcceptTarget.IsDefault caused the command to be handled
        Assert.Equal (0, rootIsDefaultViewAcceptingCount); // 0 because root is not an IAcceptTarget
        Assert.Equal (0, rootIsDefaultViewAcceptedCount); // 0 because middle is not an IAcceptTarget
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

        Assert.Equal (1, middleAcceptingCount); // 1 because of DefaultAcceptView
        Assert.Equal (0, middleAcceptedCount); // 0 because middleDefaultView is an IAcceptTarget.IsDefault caused the command to be handled
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleDefaultView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleDefaultView is an IAcceptTarget and should handle Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because middleDefaultView is an IAcceptTarget.IsDefault caused the command to be handled
        Assert.Equal (0, rootIsDefaultViewAcceptingCount); // 0 because root is not an IAcceptTarget
        Assert.Equal (0, rootIsDefaultViewAcceptedCount); // 0 because middle is not an IAcceptTarget
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

        Assert.Equal (1, middleAcceptingCount); // 1 because of DefaultAcceptView
        Assert.Equal (0, middleAcceptedCount); // 0 because middleDefaultView is an IAcceptTarget.IsDefault caused the command to be handled
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleDefaultView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleDefaultView is an IAcceptTarget and should handle Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because middleDefaultView is an IAcceptTarget.IsDefault caused the command to be handled
        Assert.Equal (0, rootIsDefaultViewAcceptingCount); // 0 because root is not an IAcceptTarget
        Assert.Equal (0, rootIsDefaultViewAcceptedCount); // 0 because middle is not an IAcceptTarget
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

    #region BubbleDown Tests

    // Claude - Opus 4.6
    /// <summary>
    ///     Exposes the protected <see cref="View.BubbleDown"/> method for testing.
    /// </summary>
    private class BubbleDownTestView : View
    {
        public bool? TestBubbleDown (View target, ICommandContext? ctx) => BubbleDown (target, ctx);
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
        Assert.True (receivedCtx!.IsBubblingDown);
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

        // BubbleDown from root to leaf — should not bubble to middle or root
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (root), null);
        root.TestBubbleDown (leaf, ctx);

        Assert.Equal (0, middleActivatingCount);
        Assert.Equal (0, rootActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void TryBubbleToSuperView_SkipsWhenIsBubblingDown ()
    {
        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Activate];

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Invoke Activate on subView with IsBubblingDown = true
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (subView), null) { IsBubblingDown = true };
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

        // First: BubbleDown — should NOT bubble
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
        int defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        int superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        int superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        // Act: Invoke Accept directly on the superView (not on a subview)
        superView.InvokeCommand (Command.Accept);

        // Assert: The DefaultAcceptView's Accepting should have fired
        Assert.Equal (1, defaultAcceptViewAcceptingCount);

        // Assert: The superView's Accepted should have fired after the DefaultAcceptView handled Accept
        Assert.Equal (1, superViewAcceptedCount);

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
}
