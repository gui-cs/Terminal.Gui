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
            e.Cancel = true;
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

    // See https://github.com/gui-cs/Terminal.Gui/issues/3913
    [Fact]
    public void Button_IsDefault_Raises_Accepted_Correctly ()
    {
        int A_AcceptedCount = 0;
        bool A_CancelAccepting = false;

        int B_AcceptedCount = 0;
        bool B_CancelAccepting = false;

        var w = new Window ()
        {
            BorderStyle = LineStyle.None,
            Width = 10,
            Height = 10
        };

        var btnA = new Button ()
        {
            Width = 3,
            IsDefault = true
        };
        btnA.Accepting += (s, e) =>
                          {
                              A_AcceptedCount++;
                              e.Cancel = A_CancelAccepting;
                          };

        var btnB = new Button ()
        {
            Width = 3,
            X = Pos.Right (btnA)
        };

        btnB.Accepting += (s, e) =>
                          {
                              B_AcceptedCount++;
                              e.Cancel = B_CancelAccepting;
                          };
        w.Add (btnA, btnB);

        w.LayoutSubviews ();

        Application.Top = w;
        Application.TopLevels.Push(w);
        Assert.Same (Application.Top, w);

        // Click button 2
        var btn2Frame = btnB.FrameToScreen ();

        Application.RaiseMouseEvent (
                         new MouseEventArgs ()
                         {
                             ScreenPosition = btn2Frame.Location,
                             Flags = MouseFlags.Button1Clicked
                         });

        // Button A should have been accepted because B didn't cancel and A IsDefault
        Assert.Equal (1, A_AcceptedCount);
        Assert.Equal (1, B_AcceptedCount);

        B_CancelAccepting = true;
        Application.RaiseMouseEvent (
                                     new MouseEventArgs ()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        // Button A (IsDefault) should NOT have been accepted because B canceled
        Assert.Equal (1, A_AcceptedCount);
        Assert.Equal (2, B_AcceptedCount);

        Application.ResetState (true);
    }

    // See: https://github.com/gui-cs/Terminal.Gui/issues/3905
    [Fact]
    public void Button_CanFocus_False_Raises_Accepted_Correctly ()
    {
        int wAcceptedCount = 0;
        bool wCancelAccepting = false;
        var w = new Window ()
        {
            Title = "Window",
            BorderStyle = LineStyle.None,
            Width = 10,
            Height = 10
        };

        w.Accepting += (s, e) =>
                       {
                           wAcceptedCount++;
                           e.Cancel = wCancelAccepting;
                       };

        int btnAcceptedCount = 0;
        bool btnCancelAccepting = false;
        var btn = new Button ()
        {
            Title = "Button",
            Width = 3,
            IsDefault = true,
        };
        btn.CanFocus = true;

        btn.Accepting += (s, e) =>
                         {
                             btnAcceptedCount++;
                             e.Cancel = btnCancelAccepting;
                         };

        w.Add (btn);


        Application.Top = w;
        Application.TopLevels.Push (w);
        Assert.Same (Application.Top, w);

        w.LayoutSubviews ();

        // Click button just like a driver would
        var btnFrame = btn.FrameToScreen ();
        Application.RaiseMouseEvent (
                                     new MouseEventArgs ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Pressed
                                     });

        Application.RaiseMouseEvent (
                                     new MouseEventArgs ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Released
                                     });

        Application.RaiseMouseEvent (
                                     new MouseEventArgs ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        Assert.Equal (1, btnAcceptedCount);
        Assert.Equal (2, wAcceptedCount);

        Application.ResetState (true);
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
        var SelectingInvoked = false;

        view.Selecting += ViewOnSelect;

        bool? ret = view.InvokeCommand (Command.Select);
        Assert.True (ret);
        Assert.True (SelectingInvoked);

        return;

        void ViewOnSelect (object sender, CommandEventArgs e)
        {
            SelectingInvoked = true;
            e.Cancel = true;
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

    public class ViewEventTester : View
    {
        public ViewEventTester ()
        {
            CanFocus = true;

            Accepting += (s, a) =>
                         {
                             a.Cancel = HandleAccepted;
                             AcceptedCount++;
                         };

            HandlingHotKey += (s, a) =>
                              {
                                  a.Cancel = HandleHandlingHotKey;
                                  HandlingHotKeyCount++;
                              };

            Selecting += (s, a) =>
                         {
                             a.Cancel = HandleSelecting;
                             SelectingCount++;
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

        /// <inheritdoc/>
        protected override bool OnSelecting (CommandEventArgs args)
        {
            OnSelectingCount++;

            return HandleOnSelecting;
        }

        public bool HandleSelecting { get; set; }
    }
}
