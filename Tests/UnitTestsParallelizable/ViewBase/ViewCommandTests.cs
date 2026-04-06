using Microsoft.Extensions.Logging;
using UnitTests.Parallelizable;
using Terminal.Gui.Tracing;
using UnitTests;

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
    public void Button_Implements_IAcceptTarget ()
    {
        Button button = new ();

        Assert.IsAssignableFrom<IAcceptTarget> (button);
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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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

        // Do not set this unless debugging. It is a static that is process wide.
        //Trace.EnabledCategories = TraceCategory.Command;

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

        // Do not set this unless debugging. It is a static that is process wide.
        //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
    ///     <see cref="ICommandContext.Values"/> accumulates values as the command propagates.
    ///     The initial value from the source is the first entry; subsequent composites append.
    /// </summary>
    [Fact]
    public void Values_Chain_Accumulates_From_Source_Through_Composites ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

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

}
