namespace ViewBaseTests.Commands;

public class ViewCommandTests
{
    #region OnAccept/Accept tests

    [Fact]
    public void Accept_Command_Raised_With_HasFocus_False ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        Assert.False (view.InvokeCommand (Command.Accept));

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

        bool? ret = view.InvokeCommand (Command.Accept);
        Assert.False (ret);
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

        protected override void OnAccepted (CommandEventArgs args)
        {
            OnAcceptedCallCount++;
            base.OnAccepted (args);
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
    public void InvokeCommands_Returns_False_If_No_Command_Handled ()
    {
        var view = new ViewEventTester ();

        bool? result = view.InvokeCommands ([Command.Activate, Command.Accept], null);

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
    public void DefaultAcceptView_FindsIsDefaultButton ()
    {
        View superView = new ();
        Button defaultButton = new () { IsDefault = true, Id = "defaultButton" };
        superView.Add (defaultButton);

        Assert.Equal (defaultButton, superView.DefaultAcceptView);
    }

    [Fact]
    public void DefaultAcceptView_ExplicitSetting_OverridesIsDefaultButton ()
    {
        View superView = new ();
        Button defaultButton = new () { IsDefault = true, Id = "defaultButton" };
        View customAcceptView = new () { Id = "customAcceptView" };
        superView.Add (defaultButton);

        superView.DefaultAcceptView = customAcceptView;

        Assert.Equal (customAcceptView, superView.DefaultAcceptView);
    }

    [Fact]
    public void Accept_Bubbles_To_DefaultAcceptView ()
    {
        View superView = new ();
        View subView = new ();
        Button defaultButton = new () { IsDefault = true, Id = "defaultButton" };

        superView.Add (subView);
        superView.Add (defaultButton);

        var defaultButtonAcceptingCount = 0;
        defaultButton.Accepting += (_, _) => defaultButtonAcceptingCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (1, defaultButtonAcceptingCount);
    }

    [Fact]
    public void Accept_DoesNotBubble_To_DefaultAcceptView_WhenHandled ()
    {
        View superView = new ();
        View subView = new ();
        Button defaultButton = new () { IsDefault = true, Id = "defaultButton" };

        superView.Add (subView);
        superView.Add (defaultButton);

        subView.Accepting += (_, e) => e.Handled = true;

        var defaultButtonAcceptingCount = 0;
        defaultButton.Accepting += (_, _) => defaultButtonAcceptingCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, defaultButtonAcceptingCount);
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
