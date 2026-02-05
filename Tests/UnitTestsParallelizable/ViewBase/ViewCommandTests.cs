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
        var acceptInvoked = false;

        view.Accepting += ViewOnAccepting;

        bool? ret = view.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        return;

        void ViewOnAccepting (object? sender, CommandEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void Accept_Command_Invokes_Accepting_Event ()
    {
        var view = new View ();
        var accepted = false;

        view.Accepting += ViewOnAccepting;

        view.InvokeCommand (Command.Accept);
        Assert.True (accepted);

        return;

        void ViewOnAccepting (object? sender, CommandEventArgs e) => accepted = true;
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
        var acceptedInvoked = false;

        view.Accepting += (sender, e) => { e.Handled = false; };

        view.Accepted += (sender, e) => { acceptedInvoked = true; };

        view.InvokeCommand (Command.Accept);
        Assert.True (acceptedInvoked);
    }

    [Fact]
    public void Accepted_Not_Raised_When_Accepting_Handled ()
    {
        View view = new ();
        var acceptedInvoked = false;

        view.Accepting += (sender, e) => { e.Handled = true; };

        view.Accepted += (sender, e) => { acceptedInvoked = true; };

        view.InvokeCommand (Command.Accept);
        Assert.False (acceptedInvoked);
    }

    [Fact]
    public void Accepted_Event_Cannot_Be_Cancelled ()
    {
        View view = new ();
        var acceptedInvoked = false;

        view.Accepted += (sender, e) =>
                         {
                             acceptedInvoked = true;

                             // Accepted event has Handled property but it doesn't affect flow
                             e.Handled = false;
                         };

        bool? ret = view.InvokeCommand (Command.Accept);
        Assert.False (ret);
        Assert.True (acceptedInvoked);
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
        var activatingInvoked = false;

        view.Activating += ViewOnActivating;

        bool? ret = view.InvokeCommand (Command.Activate);
        Assert.True (ret);
        Assert.True (activatingInvoked);

        return;

        void ViewOnActivating (object? sender, CommandEventArgs e)
        {
            activatingInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void Activate_Command_Invokes_Activating_Event ()
    {
        var view = new View ();
        var activating = false;

        view.Activating += ViewOnActivating;

        view.InvokeCommand (Command.Activate);
        Assert.True (activating);

        return;

        void ViewOnActivating (object? sender, CommandEventArgs e) => activating = true;
    }

    [Fact]
    public void LeftButtonPressed_Invokes_Activate_Command ()
    {
        var view = new ViewEventTester ();
        view.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed, Position = Point.Empty, View = view });

        Assert.Equal (1, view.OnActivatingCount);
    }

    #endregion OnActivating/Activating tests

    #region OnHotKey/HotKey tests

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new View ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    #endregion OnHotKey/HotKey tests

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

        var superViewAcceptingCalled = false;
        superView.Accepting += (_, _) => superViewAcceptingCalled = true;

        subView.InvokeCommand (Command.Accept);

        Assert.False (superViewAcceptingCalled);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void Activate_Command_DoesNotBubbleByDefault ()
    {
        View superView = new ();
        View subView = new ();
        superView.Add (subView);

        var superViewActivatingCalled = false;
        superView.Activating += (_, _) => superViewActivatingCalled = true;

        subView.InvokeCommand (Command.Activate);

        Assert.False (superViewActivatingCalled);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_CanDisableAllPropagation ()
    {
        View superView = new () { CommandsToBubbleUp = [] };
        View subView = new ();
        superView.Add (subView);

        var superViewAcceptingCalled = false;
        superView.Accepting += (_, _) => superViewAcceptingCalled = true;

        subView.InvokeCommand (Command.Accept);

        Assert.False (superViewAcceptingCalled);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_CanBeCustomized ()
    {
        View superView = new () { CommandsToBubbleUp = [Command.Accept, Command.Activate] };
        View subView = new ();
        superView.Add (subView);

        var superViewActivatingCalled = false;
        superView.Activating += (_, _) => superViewActivatingCalled = true;

        subView.InvokeCommand (Command.Activate);

        Assert.True (superViewActivatingCalled);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void TryBubbleToSuperView_StopsWhenHandled ()
    {
        View superView = new () { CommandsToBubbleUp = [Command.Accept] };
        View subView = new ();
        superView.Add (subView);

        var superViewAcceptingCalled = false;
        superView.Accepting += (_, _) => superViewAcceptingCalled = true;

        // SubView handles the command
        subView.Accepting += (_, e) => e.Handled = true;

        subView.InvokeCommand (Command.Accept);

        // Should NOT propagate because subView handled it
        Assert.False (superViewAcceptingCalled);
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

        var grandSuperViewAcceptingCalled = false;
        grandSuperView.Accepting += (_, _) => grandSuperViewAcceptingCalled = true;

        subView.InvokeCommand (Command.Accept);

        // Should propagate all the way up
        Assert.True (grandSuperViewAcceptingCalled);
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

        var grandSuperViewAcceptingCalled = false;
        grandSuperView.Accepting += (_, _) => grandSuperViewAcceptingCalled = true;

        // SuperView handles it, so shouldn't propagate further
        superView.Accepting += (_, e) => e.Handled = true;

        subView.InvokeCommand (Command.Accept);

        Assert.False (grandSuperViewAcceptingCalled);
    }

    #endregion Command Propagation Tests

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
