namespace Terminal.Gui.ViewTests;
public class ViewCommandTests
{
    #region OnAccept/Accept tests

    [Fact]
    public void Accept_Command_Raises_NoFocus ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        Assert.False (view.InvokeCommand (Command.Accept)); // there's no superview, so it should return true?

        Assert.Equal (1, view.OnAcceptedCount);

        Assert.Equal (1, view.AcceptedCount);

        Assert.False (view.HasFocus);
    }

    [Fact]
    public void Accept_Command_Handle_OnAccept_NoEvent ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        view.HandleOnAccepted = true;
        Assert.True (view.InvokeCommand (Command.Accept));

        Assert.Equal (1, view.OnAcceptedCount);

        Assert.Equal (0, view.AcceptedCount);
    }

    [Fact]
    public void Accept_Handle_Event_OnAccept_Returns_True ()
    {
        var view = new View ();
        var acceptInvoked = false;

        view.Accepting += ViewOnAccept;

        bool? ret = view.InvokeCommand (Command.Accept);
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
    public void Accept_Command_Invokes_Accept_Event ()
    {
        var view = new View ();
        var accepted = false;

        view.Accepting += ViewOnAccept;

        view.InvokeCommand (Command.Accept);
        Assert.True (accepted);

        return;

        void ViewOnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }

    // Accept on subview should bubble up to parent
    [Fact]
    public void Accept_Command_Bubbles_Up_To_SuperView ()
    {
        var view = new ViewEventTester { Id = "view" };
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

    [Fact]
    public void MouseClick_Does_Not_Invoke_Accept_Command ()
    {
        var view = new ViewEventTester ();
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked, Position = Point.Empty, View = view });

        Assert.Equal (0, view.OnAcceptedCount);
    }


    #endregion OnAccept/Accept tests

    #region OnSelect/Select tests

    [Theory]
    [CombinatorialData]
    public void Select_Command_Raises_SetsFocus (bool canFocus)
    {
        var view = new ViewEventTester
        {
            CanFocus = canFocus
        };

        Assert.Equal (canFocus, view.CanFocus);
        Assert.False (view.HasFocus);

        view.InvokeCommand (Command.Select);

        Assert.Equal (1, view.OnSelectingCount);

        Assert.Equal (1, view.SelectingCount);

        Assert.Equal (canFocus, view.HasFocus);
    }

    [Fact]
    public void Select_Command_Handle_OnSelecting_NoEvent ()
    {
        var view = new ViewEventTester ();
        Assert.False (view.HasFocus);

        view.HandleOnSelecting = true;
        Assert.True (view.InvokeCommand (Command.Select));

        Assert.Equal (1, view.OnSelectingCount);

        Assert.Equal (0, view.SelectingCount);
    }

    [Fact]
    public void Select_Handle_Event_OnSelecting_Returns_True ()
    {
        var view = new View ();
        var selectingInvoked = false;

        view.Selecting += ViewOnSelect;

        bool? ret = view.InvokeCommand (Command.Select);
        Assert.True (ret);
        Assert.True (selectingInvoked);

        return;

        void ViewOnSelect (object sender, CommandEventArgs e)
        {
            selectingInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void Select_Command_Invokes_Selecting_Event ()
    {
        var view = new View ();
        var selecting = false;

        view.Selecting += ViewOnSelecting;

        view.InvokeCommand (Command.Select);
        Assert.True (selecting);

        return;

        void ViewOnSelecting (object sender, CommandEventArgs e) { selecting = true; }
    }

    [Fact]
    public void MouseClick_Invokes_Select_Command ()
    {
        var view = new ViewEventTester ();
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked, Position = Point.Empty, View = view });

        Assert.Equal (1, view.OnSelectingCount);
    }

    #endregion OnSelect/Select tests

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

    public class ViewEventTester : View
    {
        public ViewEventTester ()
        {
            Id = "viewEventTester";
            CanFocus = true;

            Accepting += (s, a) =>
                         {
                             a.Handled = HandleAccepted;
                             AcceptedCount++;
                         };

            HandlingHotKey += (s, a) =>
                              {
                                  a.Handled = HandleHandlingHotKey;
                                  HandlingHotKeyCount++;
                              };

            Selecting += (s, a) =>
                         {
                             a.Handled = HandleSelecting;
                             SelectingCount++;
                         };

            CommandNotBound += (s, a) =>
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

        public int OnSelectingCount { get; set; }
        public int SelectingCount { get; set; }
        public bool HandleOnSelecting { get; set; }
        public bool HandleSelecting { get; set; }


        /// <inheritdoc/>
        protected override bool OnSelecting (CommandEventArgs args)
        {
            OnSelectingCount++;

            return HandleOnSelecting;
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
